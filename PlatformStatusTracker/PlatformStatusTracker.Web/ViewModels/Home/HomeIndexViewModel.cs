using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Web.ViewModels.Home
{
    public class HomeIndexViewModel
    {
        public Dictionary<DateTime, ChangeSet> IeChangeSetsByDate { get; private set; }
        public Dictionary<DateTime, ChangeSet> ChromeChangeSetsByDate { get; private set; }
        public Dictionary<DateTime, ChangeSet> WebKitWebCoreChangeSetsByDate { get; private set; }
        public Dictionary<DateTime, ChangeSet> WebKitJavaScriptCoreChangeSetsByDate { get; private set; }
        public Dictionary<DateTime, ChangeSet> MozillaChangeSetsByDate { get; private set; }
        public DateTime[] Dates { get; private set; }
        public DateTime LastUpdatedAt { get; private set; }

        public static async Task<HomeIndexViewModel> CreateAsync(IChangeSetRepository changeSetRepository)
        {
#if !FALSE
            var edgeChangeSetsTask = GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.Edge);
            var chromeChangeSetsTask = GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.Chromium);
            var webkitWebCoreChangeSetsTask = GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.WebKitWebCore);
            var webkitJavaScriptCoreChangeSetsTask = GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.WebKitJavaScriptCore);
            var mozillaChangeSetsTask = GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.Mozilla);

            await Task.WhenAll(edgeChangeSetsTask, chromeChangeSetsTask, webkitWebCoreChangeSetsTask, webkitJavaScriptCoreChangeSetsTask, mozillaChangeSetsTask);

            var edgeChangeSets = edgeChangeSetsTask.Result;
            var chromeChangeSets = chromeChangeSetsTask.Result;
            var webkitWebCoreChangeSets = webkitWebCoreChangeSetsTask.Result;
            var webkitJavaScriptCoreChangeSets = webkitJavaScriptCoreChangeSetsTask.Result;
            var mozillaChangeSets = mozillaChangeSetsTask.Result;
            var lastUpdated = edgeChangeSets.First().Date;
#else
            var ieChangeSets = await GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.InternetExplorer);
            var chromeChangeSets = await GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.Chromium);
            var webkitWebCoreChangeSets = await GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.WebKitWebCore);
            var webkitJavaScriptCoreChangeSets = await GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.WebKitJavaScriptCore);
            var mozillaChangeSets = await GetChangeSetsByBrowserAsync(changeSetRepository, StatusDataType.Mozilla);
            var lastUpdated = await statusDataRepository.GetLastUpdated();
#endif

            return new HomeIndexViewModel()
            {
                IeChangeSetsByDate = edgeChangeSets.ToDictionary(k => k.Date, v => v),
                ChromeChangeSetsByDate = chromeChangeSets.ToDictionary(k => k.Date, v => v),
                WebKitWebCoreChangeSetsByDate = webkitWebCoreChangeSets.ToDictionary(k => k.Date, v => v),
                WebKitJavaScriptCoreChangeSetsByDate = webkitJavaScriptCoreChangeSets.ToDictionary(k => k.Date, v => v),
                MozillaChangeSetsByDate = mozillaChangeSets.ToDictionary(k => k.Date, v => v),
                Dates = new[] { edgeChangeSets, chromeChangeSets, webkitWebCoreChangeSets, webkitJavaScriptCoreChangeSets, mozillaChangeSets }
                            .SelectMany(x => x)
                            .Where(x => x.Changes.Any())
                            .Select(x => x.Date)
                            .Distinct()
                            .ToArray(),
                LastUpdatedAt = lastUpdated,
            };
        }

        private static async Task<ChangeSet[]> GetChangeSetsByBrowserAsync(IChangeSetRepository changeSetRepository, StatusDataType type)
        {
            return (await changeSetRepository.GetChangeSetsRangeAsync(type, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow, take: 30))
                                                           .OrderByDescending(x => x.Date)
                                                           .ToArray();
        }

        public DateTime GetUpdatedAtByDate(DateTime date)
        {
            var dateTime = date;
            foreach (var changeSetsByDate in new [] { IeChangeSetsByDate, ChromeChangeSetsByDate, WebKitJavaScriptCoreChangeSetsByDate, WebKitWebCoreChangeSetsByDate, MozillaChangeSetsByDate })
            {
                if (changeSetsByDate.TryGetValue(dateTime, out var t) && t.UpdatedAt > dateTime)
                {
                    dateTime = t.UpdatedAt;
                }
            }

            return dateTime;
        }
    }
}
