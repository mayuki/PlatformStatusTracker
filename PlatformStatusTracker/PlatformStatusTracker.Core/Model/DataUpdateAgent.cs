using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Repository;

namespace PlatformStatusTracker.Core.Model
{
    public class DataUpdateAgent
    {
        private IStatusDataRepository _statusDataRepository;
        public DataUpdateAgent(IStatusDataRepository repository)
        {
            _statusDataRepository = repository;
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

        public async Task UpdateChromiumAsync()
        {
            var httpClient = new HttpClient();
            var data = await httpClient.GetStringAsync("http://www.chromestatus.com/features.json");

            await _statusDataRepository.InsertAsync(StatusDataType.Chromium, DateTime.UtcNow.Date, data);
        }

        public async Task UpdateModernIeAsync()
        {
            var httpClient = new HttpClient();
            var data = await httpClient.GetStringAsync("https://raw.githubusercontent.com/MicrosoftEdge/Status/production/app/static/ie-status.json");

            await _statusDataRepository.InsertAsync(StatusDataType.InternetExplorer, DateTime.UtcNow.Date, data);
        }

        public async Task UpdateWebKitWebCoreAsync()
        {
            var httpClient = new HttpClient();
            var data = await httpClient.GetStringAsync("http://svn.webkit.org/repository/webkit/trunk/Source/WebCore/features.json");

            await _statusDataRepository.InsertAsync(StatusDataType.WebKitWebCore, DateTime.UtcNow.Date, data);
        }

        public async Task UpdateWebKitJavaScriptCoreAsync()
        {
            var httpClient = new HttpClient();
            var data = await httpClient.GetStringAsync("http://svn.webkit.org/repository/webkit/trunk/Source/JavaScriptCore/features.json");

            await _statusDataRepository.InsertAsync(StatusDataType.WebKitJavaScriptCore, DateTime.UtcNow.Date, data);
        }

        public async Task UpdateMozillaAsync()
        {
            var httpClient = new HttpClient();
            var data = await httpClient.GetStringAsync("https://platform-status.mozilla.org/status.json");

            await _statusDataRepository.InsertAsync(StatusDataType.Mozilla, DateTime.UtcNow.Date, data);
        }
    }
}
