using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Repository;
using PlatformStatusTracker.Core.Data;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace PlatformStatusTracker.Core.Model
{
    public class DataUpdateAgent
    {
        private readonly IChangeSetRepository _changeSetRepository;
        private readonly IStatusRawDataRepository _statusRawDataRepository;
        private readonly ILogger _logger;

        public DataUpdateAgent(ILogger<DataUpdateAgent> logger, IChangeSetRepository repository, IStatusRawDataRepository statusRawDataRepository)
        {
            _logger = logger;
            _changeSetRepository = repository;
            _statusRawDataRepository = statusRawDataRepository;
        }

        private async Task UpdateAsync(StatusDataType dataType, string jsonUrl)
        {
            var now = DateTime.UtcNow;

            // Download a JSON from a source site.
            _logger.LogInformation("Download Status: StatusDataType={0}; Url={1}", dataType, jsonUrl);
            var httpClient = new HttpClient();
            var newJson = await httpClient.GetStringAsync(jsonUrl);

            // Save the new JSON to Blog Storage.
            _logger.LogInformation("Upload/Save to Storage: StatusDataType={0}; Url={1}", dataType, jsonUrl);
            await _statusRawDataRepository.InsertAsync(dataType, now, newJson);

            // Fetch a latest changeset from Table
            var latestChangeSet = await _changeSetRepository.GetLatestChangeSetAsync(dataType);

            if (latestChangeSet != null)
            {
                // Fetch the previous JSON from Blob Storage.
                // If the changeset's Date is same date, downloads a previous day's JSON.
                // Otherwise It downloads latest JSON.
                var from = (latestChangeSet.Date.Date == now.Date)
                    ? latestChangeSet.From
                    : latestChangeSet.Date;
                _logger.LogInformation("Fetch data: StatusDataType={0}; Date={1}", dataType, from);
                var prevJson = await _statusRawDataRepository.GetByDateAsync(dataType, from);

                // Create a changeset from two JSONs.
                var curStatuses = PlatformStatuses.Deserialize(dataType, newJson);
                var prevStatuses = PlatformStatuses.Deserialize(dataType, prevJson);
                var changeInfo = PlatformStatusTracking.GetChangeInfoSetFromStatuses(prevStatuses, curStatuses);
                var changeInfoJson = JsonConvert.SerializeObject(changeInfo);

                // Load previous changeset from Table and check modification.
                var prevChangeSet = (await _changeSetRepository.GetChangeSetsRangeAsync(dataType, from, from, 1)).FirstOrDefault();
                if (prevChangeSet != null && JsonConvert.SerializeObject(prevChangeSet.Changes) == changeInfoJson)
                {
                    _logger.LogInformation("Not Modified: StatusDataType={0}; Date={1}; From={2}", dataType, now, from);
                    return;
                }

                // Save new changeset to Table.
                _logger.LogInformation("Insert/Update: StatusDataType={0}; Date={1}; From={2}", dataType, now, from);
                await _changeSetRepository.InsertOrReplaceAsync(dataType, now, changeInfoJson, from);
            }
            else
            {
                // Previous changeset doesn't exist on the table. Create a new row.
                await _changeSetRepository.InsertOrReplaceAsync(dataType, now, "[]", now);
            }
        }

        public async Task UpdateAllAsync()
        {
            //await UpdateChromiumAsync();
            //await UpdateModernIeAsync();
            //await UpdateWebKitJavaScriptCoreAsync();
            //await UpdateWebKitWebCoreAsync();
            //await UpdateMozillaAsync();

            await Task.WhenAll(UpdateChromiumAsync(), UpdateModernIeAsync(), UpdateWebKitJavaScriptCoreAsync(), UpdateWebKitWebCoreAsync(), UpdateMozillaAsync());
        }

        public Task UpdateChromiumAsync()
        {
            return UpdateAsync(StatusDataType.Chromium, "http://www.chromestatus.com/features.json");
        }

        public Task UpdateModernIeAsync()
        {
            return UpdateAsync(StatusDataType.Edge, "https://raw.githubusercontent.com/MicrosoftEdge/Status/production/status.json");
        }

        public Task UpdateWebKitWebCoreAsync()
        {
            return UpdateAsync(StatusDataType.WebKitWebCore, "http://svn.webkit.org/repository/webkit/trunk/Source/WebCore/features.json");
        }

        public Task UpdateWebKitJavaScriptCoreAsync()
        {
            return UpdateAsync(StatusDataType.WebKitJavaScriptCore, "http://svn.webkit.org/repository/webkit/trunk/Source/JavaScriptCore/features.json");
        }

        public Task UpdateMozillaAsync()
        {
            return UpdateAsync(StatusDataType.Mozilla, "https://platform-status.mozilla.org/api/status");
        }
    }
}
