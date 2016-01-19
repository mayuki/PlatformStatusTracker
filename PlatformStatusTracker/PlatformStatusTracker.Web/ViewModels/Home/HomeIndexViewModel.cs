using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Repository;

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

        public static async Task<HomeIndexViewModel> CreateAsync(IStatusDataRepository statusDataRepository)
        {
#if !FALSE
            var ieChangeSetsTask = GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.InternetExplorer);
            var chromeChangeSetsTask = GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.Chromium);
            var webkitWebCoreChangeSetsTask = GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.WebKitWebCore);
            var webkitJavaScriptCoreChangeSetsTask = GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.WebKitJavaScriptCore);
            var mozillaChangeSetsTask = GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.Mozilla);
            var lastUpdatedTask = statusDataRepository.GetLastUpdated();

            await Task.WhenAll(ieChangeSetsTask, chromeChangeSetsTask, webkitWebCoreChangeSetsTask, webkitJavaScriptCoreChangeSetsTask, mozillaChangeSetsTask, lastUpdatedTask);

            var ieChangeSets = ieChangeSetsTask.Result;
            var chromeChangeSets = chromeChangeSetsTask.Result;
            var webkitWebCoreChangeSets = webkitWebCoreChangeSetsTask.Result;
            var webkitJavaScriptCoreChangeSets = webkitJavaScriptCoreChangeSetsTask.Result;
            var mozillaChangeSets = mozillaChangeSetsTask.Result;
            var lastUpdated = lastUpdatedTask.Result;
#else
            var ieChangeSets = await GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.InternetExplorer);
            var chromeChangeSets = await GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.Chromium);
            var webkitWebCoreChangeSets = await GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.WebKitWebCore);
            var webkitJavaScriptCoreChangeSets = await GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.WebKitJavaScriptCore);
            var mozillaChangeSets = await GetChangeSetsByBrowserAsync(statusDataRepository, StatusDataType.Mozilla);
            var lastUpdated = await statusDataRepository.GetLastUpdated();
#endif

            return new HomeIndexViewModel()
                   {
                       IeChangeSetsByDate = ieChangeSets.ToDictionary(k => k.Date, v => v),
                       ChromeChangeSetsByDate = chromeChangeSets.ToDictionary(k => k.Date, v => v),
                       WebKitWebCoreChangeSetsByDate = webkitWebCoreChangeSets.ToDictionary(k => k.Date, v => v),
                       WebKitJavaScriptCoreChangeSetsByDate = webkitJavaScriptCoreChangeSets.ToDictionary(k => k.Date, v => v),
                       MozillaChangeSetsByDate = mozillaChangeSets.ToDictionary(k => k.Date, v => v),
                       Dates = new [] { ieChangeSets, chromeChangeSets, webkitWebCoreChangeSets, webkitJavaScriptCoreChangeSets, mozillaChangeSets }
                            .SelectMany(x => x)
                            .Where(x => x.Changes.Any())
                            .Select(x => x.Date)
                            .Distinct()
                            .ToArray(),
                       LastUpdatedAt = lastUpdated,
                   };
        }

        private static async Task<ChangeSet[]> GetChangeSetsByBrowserAsync(IStatusDataRepository statusDataRepository, StatusDataType type)
        {
            var statusDataSet = (await statusDataRepository.GetPlatformStatusesRangeAsync(type, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow, take: 30))
                                                           .OrderByDescending(x => x.Date)
                                                           .ToArray();

            if (statusDataSet.Length < 2)
            {
                return new ChangeSet[0];
            }

            return ChangeSet.GetChangeSetsFromPlatformStatuses(statusDataSet);
        }
    }
}