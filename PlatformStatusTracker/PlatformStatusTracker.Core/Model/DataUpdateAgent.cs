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
using System.Diagnostics;

namespace PlatformStatusTracker.Core.Model
{
    public class DataUpdateAgent
    {
        private IChangeSetRepository _changeSetRepository;
        private IStatusRawDataRepository _statusRawDataRepository;

        public DataUpdateAgent(IChangeSetRepository repository, IStatusRawDataRepository statusRawDataRepository)
        {
            _changeSetRepository = repository;
            _statusRawDataRepository = statusRawDataRepository;
        }

        private async Task UpdateAsync(StatusDataType dataType, string jsonUrl)
        {
            var now = DateTime.UtcNow;

            // Download a JSON from a source site.
            Debug.WriteLine("Download Status: StatusDataType={0}; Url={1}", dataType, jsonUrl);
            var httpClient = new HttpClient();
            var newJson = await httpClient.GetStringAsync(jsonUrl);

            // Save the new JSON to Blog Storage.
            Debug.WriteLine("Upload/Save to Storage: StatusDataType={0}; Url={1}", dataType, jsonUrl);
            await _statusRawDataRepository.InsertAsync(dataType, now, newJson);

            // Fetch a latest changeset from Table
            var latestChangeSet = await _changeSetRepository.GetLatestChangeSetAsync(dataType);

            // Fetch the previous JSON from Blob Storage.
            // If the changeset's Date is same date, downloads a previous day's JSON.
            // Otherwise It downloads latest JSON.
            var from = (latestChangeSet.Date.Date == now.Date)
                    ? latestChangeSet.From
                    : latestChangeSet.Date;
            Debug.WriteLine("Fetch data: StatusDataType={0}; Date={1}", dataType, from);
            var prevJson = await _statusRawDataRepository.GetByDateAsync(dataType, from);

            // Create a changeset from two JSONs.
            var curStatuses = PlatformStatuses.Deserialize(dataType, newJson);
            var prevStatuses = PlatformStatuses.Deserialize(dataType, prevJson);
            var changeInfo = PlatformStatusTracking.GetChangeInfoSetFromStatuses(prevStatuses, curStatuses);
            var changeInfoJson = JsonConvert.SerializeObject(changeInfo);

            // Save new changeset to Table.
            Debug.WriteLine("Insert/Update : StatusDataType={0}; Date={1}; From={2}", dataType, now, from);
            await _changeSetRepository.InsertAsync(dataType, now, changeInfoJson, from);
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
