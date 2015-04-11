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
            await Task.WhenAll(UpdateChromiumAsync(), UpdateModernIeAsync());
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
            var data = await httpClient.GetStringAsync("https://raw.githubusercontent.com/InternetExplorer/Status.IE/production/app/static/ie-status.json");

            await _statusDataRepository.InsertAsync(StatusDataType.InternetExplorer, DateTime.UtcNow.Date, data);
        }
    }
}
