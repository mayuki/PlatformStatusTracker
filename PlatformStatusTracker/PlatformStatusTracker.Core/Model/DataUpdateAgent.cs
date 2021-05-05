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
        private readonly IReadOnlyList<(StatusDataType DataType, string Url)> _targets = new[]
        {
            (StatusDataType.Chromium, "http://www.chromestatus.com/features.json"),
            (StatusDataType.Edge, "https://raw.githubusercontent.com/MicrosoftEdge/Status/production/status.json"),
            (StatusDataType.WebKitWebCore, "http://svn.webkit.org/repository/webkit/trunk/Source/WebCore/features.json"),
            (StatusDataType.WebKitJavaScriptCore, "http://svn.webkit.org/repository/webkit/trunk/Source/JavaScriptCore/features.json"),
            (StatusDataType.Mozilla, "https://platform-status.mozilla.org/api/status"),
        };

        public DataUpdateAgent(ILogger<DataUpdateAgent> logger, IChangeSetRepository repository, IStatusRawDataRepository statusRawDataRepository)
        {
            _logger = logger;
            _changeSetRepository = repository;
            _statusRawDataRepository = statusRawDataRepository;
        }

        private async Task UpdateAsync(StatusDataType dataType)
        {
            var now = DateTime.UtcNow;

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

                var maxRetryCount = 7;
                Retry:
                if (!await TryUpdateChangeSetAsync(dataType, from, now))
                {
                    if (maxRetryCount > 0)
                    {
                        maxRetryCount--;
                        from = from.AddDays(-1);
                        goto Retry;
                    }
                    else
                    {
                        throw new InvalidOperationException("Can't create or update a change set. The JSON data may be corrupted or invalid.");
                    }
                }
            }
            else
            {
                // Previous changeset doesn't exist on the table. Create a new row.
                await _changeSetRepository.InsertOrReplaceAsync(dataType, now, "[]", now);
            }
        }

        private async Task FetchAndStoreRawDataAsync(StatusDataType dataType, DateTime date, string jsonUrl)
        {
            _logger.LogInformation("FetchAndStoreRawDataAsync: StatusDataType={0}; Date={1}; Url={2}", dataType, date, jsonUrl);

            // Download a JSON from a source site.
            _logger.LogInformation("Download Status: StatusDataType={0}; Url={1}", dataType, jsonUrl);
            var httpClient = new HttpClient();
            var newJson = await httpClient.GetStringAsync(jsonUrl);

            // Save the new JSON to Blog Storage.
            _logger.LogInformation("Upload/Save to Storage: StatusDataType={0}; Url={1}", dataType, jsonUrl);
            await _statusRawDataRepository.InsertAsync(dataType, date, newJson);
        }

        private async Task<bool> TryUpdateChangeSetAsync(StatusDataType dataType, DateTime from, DateTime to)
        {
            _logger.LogInformation("UpdateChangeSetAsync: StatusDataType={0}; Date={1}; From={2}", dataType, to, from);

            try
            {
                _logger.LogTrace("Fetch data: StatusDataType={0}; Date={1}", dataType, to);
                var newJson = await _statusRawDataRepository.GetByDateAsync(dataType, to);
                var curStatuses = PlatformStatuses.Deserialize(dataType, newJson);

                _logger.LogTrace("Fetch data: StatusDataType={0}; Date={1}", dataType, from);
                var prevJson = await _statusRawDataRepository.GetByDateAsync(dataType, from);
                var prevStatuses = PlatformStatuses.Deserialize(dataType, prevJson);

                // Create a changeset from two JSONs.
                var changeInfo = PlatformStatusTracking.GetChangeInfoSetFromStatuses(prevStatuses, curStatuses);
                var changeInfoJson = JsonConvert.SerializeObject(changeInfo);

                // Load previous changeset from Table and check modification.
                var prevChangeSet = (await _changeSetRepository.GetChangeSetsRangeAsync(dataType, from, from, 1)).FirstOrDefault();
                if (prevChangeSet != null && JsonConvert.SerializeObject(prevChangeSet.Changes) == changeInfoJson)
                {
                    _logger.LogInformation("Not Modified: StatusDataType={0}; Date={1}; From={2}", dataType, to, from);
                    return true;
                }

                // Save new changeset to Table.
                _logger.LogInformation("Insert/Update: StatusDataType={0}; Date={1}; From={2}; Changes={3}", dataType, to, from, changeInfo.Length);
                await _changeSetRepository.InsertOrReplaceAsync(dataType, to, changeInfoJson, from);
                return true;

            }
            catch (Exception e)
            {
                _logger.LogError("Insert/Update: StatusDataType={0}; Date={1}; From={2}; Error={3}", dataType, to, from, e.Message);
                return false;
            }
        }

        public async Task UpdateAllAsync()
        {
            var now = DateTime.UtcNow;

            // Fetch and store JSONs
            foreach (var target in _targets)
            {
                await FetchAndStoreRawDataAsync(target.DataType, now, target.Url);
            }

            // Update
            foreach (var target in _targets)
            {
                await UpdateAsync(target.DataType);
            }
        }

        public async Task UpdateChangeSetByRangeAsync(StatusDataType dataType, DateTime begin, DateTime end)
        {
            if (begin > end) throw new ArgumentException("The end date must be after the begin date.");

            var cur = begin;
            var prev = cur.AddDays(-1);
            while (cur <= end)
            {
                if (await TryUpdateChangeSetAsync(dataType, prev, cur))
                {
                    prev = cur;
                    cur = cur.AddDays(1);
                }
                else
                {
                    cur = cur.AddDays(1);
                }
            }
        }
    }
}
