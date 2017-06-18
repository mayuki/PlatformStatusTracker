using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PlatformStatusTracker.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Core.Repository
{
    public interface IStatusRawDataRepository
    {
        Task InsertAsync(StatusDataType dataType, DateTime date, string content);
        Task<string> GetByDateAsync(StatusDataType dataType, DateTime date);
    }

    public class StatusRawDataAzureStorageRepository : IStatusRawDataRepository
    {
        private readonly string _connectionString;
        public StatusRawDataAzureStorageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InsertAsync(StatusDataType dataType, DateTime date, string content)
        {
            var container = await GetContainerAsync();
            var typeNameV2 = dataType == StatusDataType.InternetExplorer ? "Edge" : dataType.ToString();
            var data = Encoding.UTF8.GetBytes(content);
            await container.GetBlockBlobReference($"{typeNameV2}/{date.ToString("yyyyMMdd")}.json").UploadFromByteArrayAsync(data, 0, data.Length).ConfigureAwait(false);
        }

        public async Task<string> GetByDateAsync(StatusDataType dataType, DateTime date)
        {
            var container = await GetContainerAsync();
            var typeNameV2 = dataType == StatusDataType.InternetExplorer ? "Edge" : dataType.ToString();
            return await container.GetBlockBlobReference($"{typeNameV2}/{date.ToString("yyyyMMdd")}.json").DownloadTextAsync().ConfigureAwait(false);
        }

        private async Task<CloudBlobContainer> GetContainerAsync()
        {
            var storageAccount = CloudStorageAccount.Parse(_connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("statuses");
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            return container;
        }
    }
}
