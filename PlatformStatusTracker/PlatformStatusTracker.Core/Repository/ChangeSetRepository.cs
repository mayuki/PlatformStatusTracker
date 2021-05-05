using LZ4;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using PlatformStatusTracker.Core.Configuration;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Core.Repository
{
    public interface IChangeSetRepository
    {
        Task<ChangeSet> GetLatestChangeSetAsync(StatusDataType type);
        Task<ChangeSet[]> GetChangeSetsRangeAsync(StatusDataType type, DateTime fromDate, DateTime toDate, int take);
        Task InsertOrReplaceAsync(StatusDataType dataType, DateTime date, string changeSetContent, DateTime from);
    }

    public class ChangeSetAzureStorageRepository : IChangeSetRepository
    {
        private readonly string _connectionString;

        public ChangeSetAzureStorageRepository(IOptions<ConnectionStringOptions> connectionStringOptions)
            : this(connectionStringOptions.Value.AzureStorageConnectionString)
        {
        }

        public ChangeSetAzureStorageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<CloudTable> CreateOrGetTableAsync()
        {
            //var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var storageAccount = CloudStorageAccount.Parse(_connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();

            var table = tableClient.GetTableReference("ChangeSets");
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            return table;
        }

        public async Task<ChangeSet> GetLatestChangeSetAsync(StatusDataType type)
        {
            var table = await CreateOrGetTableAsync();
            var queryFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "02_"); // 01_*

            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "01_" + (int)type)
            );

            return (await table.ExecuteQuerySegmentedAsync(new TableQuery<ChangeSetEntity>().Where(queryFilter).Take(1), null))
                .Select(x => x.ToChangeSet())
                .FirstOrDefault();
        }

        public async Task<ChangeSet[]> GetChangeSetsRangeAsync(StatusDataType type, DateTime fromDate, DateTime toDate, int take = 30)
        {
            if (fromDate > toDate)
                throw new ArgumentException("fromDate parameter must be before toDate.");

            var table = await CreateOrGetTableAsync();
            var queryFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "02_"); // 01_*

            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "01_" + (int)type)
            );

            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, "01_" + ChangeSetEntity.ConvertDateTimeToRowKey(fromDate.Date)) // RowKey <= fromDate.Date (Higher RowKey is older)
            );
            queryFilter = TableQuery.CombineFilters(
                queryFilter,
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, "01_" + ChangeSetEntity.ConvertDateTimeToRowKey(toDate.Date)) // RowKey >= toDate.Date (Higher RowKey is older)
            );

            var entities = await Utility.Measure(() => table.ExecuteQuerySegmentedAsync(new TableQuery<ChangeSetEntity>().Where(queryFilter).Take(take), null)).ConfigureAwait(false);

            return entities.Select(x => x.ToChangeSet()).ToArray();
        }

        public async Task InsertOrReplaceAsync(StatusDataType dataType, DateTime date, string changeSetContent, DateTime from)
        {
            var table = await CreateOrGetTableAsync();
            await table.ExecuteAsync(TableOperation.InsertOrReplace(new ChangeSetEntity(dataType, date, changeSetContent, from))).ConfigureAwait(false);
        }

        public async Task InsertAsync(IEnumerable<ChangeSetEntity> entities)
        {
            var table = await CreateOrGetTableAsync();
            await table.ExecuteBatchAsync(entities.Aggregate(new TableBatchOperation(), (batch, x) => { batch.InsertOrReplace(x); return batch; })).ConfigureAwait(false);
        }

        public class ChangeSetEntity : TableEntity
        {
            public int DataType { get; set; }
            public DateTime Date { get; set; }
            public byte[] Content { get; set; }
            public DateTime From { get; set; }
            public DateTime UpdatedAt { get; set; }

            public ChangeSetEntity() { }
            public ChangeSetEntity(StatusDataType dataType, DateTime date, String changeSetContent, DateTime from)
            {
                PartitionKey = "01_" + ((int)dataType).ToString();
                RowKey = "01_" + (ConvertDateTimeToRowKey(date.Date)); // Date only

                DataType = (int)dataType;
                Date = date.Date;
                Content = LZ4Codec.Wrap(Encoding.UTF8.GetBytes(changeSetContent));
                From = from;
                UpdatedAt = DateTime.UtcNow;
            }

            public string GetContent()
            {
                return Encoding.UTF8.GetString(LZ4Codec.Unwrap(Content));
            }

            public ChangeSet ToChangeSet()
            {
                IChangeInfo[] changes;
                switch ((StatusDataType)DataType)
                {
                    case StatusDataType.Edge: changes = JsonConvert.DeserializeObject<IeChangeInfo[]>(GetContent()); break;
                    case StatusDataType.Chromium: changes = JsonConvert.DeserializeObject<ChromiumChangeInfo[]>(GetContent()); break;
                    case StatusDataType.WebKitJavaScriptCore: changes = JsonConvert.DeserializeObject<WebKitChangeInfo[]>(GetContent()); break;
                    case StatusDataType.WebKitWebCore: changes = JsonConvert.DeserializeObject<WebKitChangeInfo[]>(GetContent()); break;
                    case StatusDataType.Mozilla: changes = JsonConvert.DeserializeObject<MozillaChangeInfo[]>(GetContent()); break;
                    default: throw new NotSupportedException();
                }

                return new ChangeSet()
                {
                    Date = Date,
                    From = From,
                    Changes = changes,
                    UpdatedAt = UpdatedAt,
                };
            }

            /// <summary>
            /// Create RowKey value from DateTime (If the date is newer, RowKey value is lower)
            /// </summary>
            /// <param name="dateTime"></param>
            /// <returns></returns>
            public static String ConvertDateTimeToRowKey(DateTime dateTime)
            {
                // for ascending order
                return String.Format("{0:D19}", DateTime.MaxValue.Ticks - dateTime.Ticks);
            }
        }
    }
}
