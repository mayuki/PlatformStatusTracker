using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
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

            return
                (await table.ExecuteQuerySegmentedAsync(new TableQuery<StatusDataEntity>().Where(queryFilter).Take(take), null))
                    .Select(CreatePlatformStatusesFromEntity)
                    .ToArray();
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

        private PlatformStatuses CreatePlatformStatusesFromEntity(StatusDataEntity entity)
        {
            return new PlatformStatuses(entity.Date,
                                        (entity.DataType == (Int32)StatusDataType.InternetExplorer)
                                            ? (PlatformStatus[])PlatformStatuses.DeserializeForIeStatus(entity.GetContent())
                                        : (entity.DataType == (Int32)StatusDataType.WebKitWebCore || entity.DataType == (Int32)StatusDataType.WebKitJavaScriptCore)
                                            ? (PlatformStatus[])PlatformStatuses.DeserializeForWebKitStatus(entity.GetContent())
                                            : (PlatformStatus[])PlatformStatuses.DeserializeForChromiumStatus(entity.GetContent())
                                        );
        }

        private class StatusDataEntity : TableEntity
        {
            public StatusDataEntity()
            { }

            public StatusDataEntity(StatusDataType dataType, DateTime date, String content)
            {
                PartitionKey = "01_" + ((Int32)dataType).ToString();
                RowKey = "01_" + (ConvertDateTimeToRowKey(date.Date)); // Date only

                DataType = (Int32)dataType;
                Date = date;
                Content = Compress(Encoding.UTF8.GetBytes(content));
            }

            public Byte[] Content { get; set; }
            public DateTime Date { get; set; }
            public Int32 DataType { get; set; }

            public String GetContent()
            {
                return Encoding.UTF8.GetString(Decompress(Content));
            }

            private Byte[] Compress(Byte[] bytes)
            {
                var stream = new MemoryStream();
                var gzipStream = new GZipStream(stream, CompressionLevel.Optimal);
                gzipStream.Write(bytes, 0, bytes.Length);
                gzipStream.Close();

                return stream.ToArray();
            }

            private Byte[] Decompress(Byte[] bytes)
            {
                var gzipStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress);
                var memStream = new MemoryStream();
                while (true)
                {
                    var buffer = new byte[1024];
                    var readLen = gzipStream.Read(buffer, 0, buffer.Length);
                    if (readLen == 0)
                        break;

                    memStream.Write(buffer, 0, readLen);
                }

                return memStream.ToArray();
            }
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
