using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using PlatformStatusTracker.Core.Data;
using PlatformStatusTracker.Core.Enum;

namespace PlatformStatusTracker.Core.Repository
{
    public interface IStatusDataRepository
    {
        Task<PlatformStatuses[]> GetPlatformStatusesRangeAsync(StatusDataType type, DateTime fromDate, DateTime toDate, Int32 take);
        Task InsertAsync(StatusDataType type, DateTime date, String jsonData);
        Task<DateTime> GetLastUpdated();
    }

    public class StatusDataAzureStorageRepository : IStatusDataRepository
    {
        private String _connectionString;

        public StatusDataAzureStorageRepository(String connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PlatformStatuses[]> GetPlatformStatusesRangeAsync(StatusDataType type, DateTime fromDate, DateTime toDate, Int32 take = 30)
        {
            if (fromDate > toDate)
                throw new ArgumentException("fromDate parameter must be before toDate.");

            var table = await CreateOrGetTable();
            var queryFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "02_"); // 01_*

            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "01_" + (Int32)type)
            );

            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, "01_" + ConvertDateTimeToRowKey(fromDate.Date)) // RowKey <= fromDate.Date (Higher RowKey is older)
            );
            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, "01_" + ConvertDateTimeToRowKey(toDate.Date)) // RowKey >= toDate.Date (Higher RowKey is older)
            );

            var entities = await Utility.Measure(() => table.ExecuteQuerySegmentedAsync(new TableQuery<StatusDataEntity>().Where(queryFilter).Take(take), null));

            var tasks = entities.Select(x =>
                    {
                        // return null if JSON data was broken.
                        try
                        {
                            return CreatePlatformStatusesFromEntityAsync(x);
                        }
                        catch (JsonReaderException)
                        {
                            return null;
                        }
                    })
                    .Where(x => x != null)
                    .ToArray();

            return await Task.WhenAll(tasks);
        }

        public async Task InsertAsync(StatusDataType type, DateTime date, String jsonData)
        {
            var table = await CreateOrGetTable();
            await table.ExecuteAsync(TableOperation.InsertOrReplace(new StatusDataEntity(type, date, jsonData)));
        }

        public async Task<DateTime> GetLastUpdated()
        {
            var table = await CreateOrGetTable();
            var queryFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "02_"); // 01_*

            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "01_" + (Int32) StatusDataType.InternetExplorer)
            );

            var entity = (await table.ExecuteQuerySegmentedAsync(new TableQuery<StatusDataEntity>().Where(queryFilter).Take(1), null))
                .FirstOrDefault();

            return entity == null ? new DateTime(0) : entity.Timestamp.DateTime;
        }

        private async Task<CloudTable> CreateOrGetTable()
        {
            //var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var storageAccount = CloudStorageAccount.Parse(_connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();

            var table = tableClient.GetTableReference("StatusData");
            await table.CreateIfNotExistsAsync();

            return table;
        }

        /// <summary>
        /// Create RowKey value from DateTime (If the date is newer, RowKey value is lower)
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static String ConvertDateTimeToRowKey(DateTime dateTime)
        {
            // for ascending order
            return String.Format("{0:D19}", DateTime.MaxValue.Ticks - dateTime.Ticks);
        }

        private Task<PlatformStatuses> CreatePlatformStatusesFromEntityAsync(StatusDataEntity entity)
        {
            // Deserialization is very heavy...
            return Task.Run(() => 
                new PlatformStatuses(entity.Date,
                                        (entity.DataType == (Int32)StatusDataType.InternetExplorer)
                                            ? (IPlatformStatus[])PlatformStatuses.DeserializeForIeStatus(entity.GetContent())
                                        : (entity.DataType == (Int32)StatusDataType.WebKitWebCore || entity.DataType == (Int32)StatusDataType.WebKitJavaScriptCore)
                                            ? (IPlatformStatus[])PlatformStatuses.DeserializeForWebKitStatus(entity.GetContent())
                                        : (entity.DataType == (Int32)StatusDataType.Mozilla)
                                            ? (IPlatformStatus[])PlatformStatuses.DeserializeForMozillaStatus(entity.GetContent())
                                        : (IPlatformStatus[])PlatformStatuses.DeserializeForChromiumStatus(entity.GetContent())
                                        )
            );
        }

        private class StatusDataEntity : TableEntity
        {
            public StatusDataEntity()
            { }

            public StatusDataEntity(StatusDataType dataType, DateTime date, String content)
            {
                this.PartitionKey = "01_" + ((Int32)dataType).ToString();
                this.RowKey = "01_" + (ConvertDateTimeToRowKey(date.Date)); // Date only

                this.DataType = (Int32)dataType;
                this.Date = date;
                this.DataCompression = (Int32)DataCompressionType.Bzip2;

                var splitSize = (1024 * 50);
                var compressedContent = Compress(Encoding.UTF8.GetBytes(content));
                for (var i = 0; i < Math.Ceiling(compressedContent.Length / (double)splitSize); i++)
                {
                    this.SplitCount = i;

                    var tmpContent = new byte[Math.Min(splitSize, compressedContent.Length - (i * splitSize))];
                    Array.Copy(compressedContent, (i * splitSize), tmpContent, 0, tmpContent.Length);

                    switch (i)
                    {
                        case 0:
                            Content = tmpContent;
                            break;
                        case 1:
                            Content1 = tmpContent;
                            break;
                        case 2:
                            Content2 = tmpContent;
                            break;
                        case 3:
                            Content3 = tmpContent;
                            break;
                        case 4:
                            Content4 = tmpContent;
                            break;
                        default:
                            throw new Exception("data size is too large.: " + compressedContent.Length);
                    }
                }
            }

            public Byte[] Content { get; set; }
            public Byte[] Content1 { get; set; }
            public Byte[] Content2 { get; set; }
            public Byte[] Content3 { get; set; }
            public Byte[] Content4 { get; set; }

            public DateTime Date { get; set; }
            public Int32 DataType { get; set; }
            public Int32 DataCompression { get; set; }
            public Int32 SplitCount { get; set; }

            public String GetContent()
            {
                switch (this.SplitCount)
                {
                    case 0:
                        return Encoding.UTF8.GetString(Decompress(Content));
                    case 1:
                        return Encoding.UTF8.GetString(Decompress(Content, Content1));
                    case 2:
                        return Encoding.UTF8.GetString(Decompress(Content, Content1, Content2));
                    case 3:
                        return Encoding.UTF8.GetString(Decompress(Content, Content1, Content2, Content3));
                    case 4:
                        return Encoding.UTF8.GetString(Decompress(Content, Content1, Content2, Content3, Content4));
                    default:
                        throw new Exception("splitted count is too large.: " + this.SplitCount);
                }
            }

            private byte[] Compress(byte[] bytes)
            {
                var stream = new MemoryStream();
                var compressStream = (DataCompression == (Int32) DataCompressionType.Gzip)
                    ? (Stream)new GZipStream(stream, CompressionLevel.Optimal)
                    : (Stream)new BZip2OutputStream(stream, 900*1024);

                compressStream.Write(bytes, 0, bytes.Length);
                compressStream.Close();

                return stream.ToArray();
            }

            private byte[] Decompress(params byte[][] bytesArray)
            {
                var compressedStream = new MemoryStream(bytesArray.Sum(x => x.Length));
                foreach (var bytes in bytesArray)
                {
                    compressedStream.Write(bytes, 0, bytes.Length);
                }
                compressedStream.Position = 0;

                var uncompressStream = (DataCompression == (Int32)DataCompressionType.Gzip)
                    ? (Stream)new GZipStream(compressedStream, CompressionMode.Decompress)
                    : (Stream)new BZip2InputStream(compressedStream);
                var decompressedStream = new MemoryStream(512 * 1024);
                var buffer = new byte[1024 * 64];

                while (true)
                {
                    var readLen = uncompressStream.Read(buffer, 0, buffer.Length);
                    if (readLen == 0)
                        break;

                    decompressedStream.Write(buffer, 0, readLen);
                }

                return decompressedStream.ToArray();
            }
        }

        private enum DataCompressionType
        {
            Gzip = 0,
            Bzip2 = 1,
        }
    }

    public class StatusDataTestRepository : IStatusDataRepository
    {
        public async Task<PlatformStatuses[]> GetPlatformStatusesRangeAsync(StatusDataType type, DateTime fromDate, DateTime toDate, Int32 take)
        {
            return new[]
                   {
                       new PlatformStatuses(DateTime.Parse("2015/1/1"), PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""A"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""10"",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")),
                       new PlatformStatuses(DateTime.Parse("2015/1/2"), PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""A"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""10"",""ieUnprefixed"":""11""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")),
                       new PlatformStatuses(DateTime.Parse("2015/1/3"), PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""A"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""10"",""ieUnprefixed"":""11""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}, {""name"":""B"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Under Consideration"",""iePrefixed"":"""",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")),
                       new PlatformStatuses(DateTime.Parse("2015/1/4"), PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""A"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""10"",""ieUnprefixed"":""11""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}, {""name"":""B"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""In Development"",""iePrefixed"":"""",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")),
                       new PlatformStatuses(DateTime.Parse("2015/1/5"), PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""A"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""10"",""ieUnprefixed"":""11""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}, {""name"":""B"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""99"",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")),
                   };
        }

        public async Task InsertAsync(StatusDataType type, DateTime date, String jsonData)
        {
            // 何もしない
        }

        public async Task<DateTime> GetLastUpdated()
        {
            return DateTime.UtcNow;
        }
    }
}
