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
    public class ChangesViewModel
    {
        public ChangeSetsViewModel ChangeSets { get; private set; }
        public DateTime Date { get; private set; }

        public static async Task<ChangesViewModel> CreateAsync(IStatusDataRepository statusDataRepository, DateTime date)
        {
            var ieChangeSetsTask = GetChangeSetsByBrowser(statusDataRepository, StatusDataType.InternetExplorer, date);
            var chromeChangeSetsTask = GetChangeSetsByBrowser(statusDataRepository, StatusDataType.Chromium, date);
            var webkitWebCoreChangeSetsTask = GetChangeSetsByBrowser(statusDataRepository, StatusDataType.WebKitWebCore, date);
            var webkitJavaScriptCoreChangeSetsTask = GetChangeSetsByBrowser(statusDataRepository, StatusDataType.WebKitJavaScriptCore, date);

            await Task.WhenAll(ieChangeSetsTask, chromeChangeSetsTask, webkitWebCoreChangeSetsTask, webkitJavaScriptCoreChangeSetsTask);

            var ieChangeSets = ieChangeSetsTask.Result;
            var chromeChangeSets = chromeChangeSetsTask.Result;
            var webkitWebCoreChangeSets = webkitWebCoreChangeSetsTask.Result;
            var webkitJavaScriptCoreChangeSets = webkitJavaScriptCoreChangeSetsTask.Result;

            return new ChangesViewModel()
            {
                ChangeSets = new ChangeSetsViewModel()
                             {
                                 IeChangeSet = ieChangeSets.Any() ? ieChangeSets[0] : null,
                                 ChromeChangeSet = chromeChangeSets.Any() ? chromeChangeSets[0] : null,
                                 WebKitWebCoreChangeSet = webkitWebCoreChangeSets.Any() ? webkitWebCoreChangeSets[0] : null,
                                 WebKitJavaScriptCoreChangeSet = webkitJavaScriptCoreChangeSets.Any() ? webkitJavaScriptCoreChangeSets[0] : null,
                             },
                Date = date,
            };
        }

        private static async Task<ChangeSet[]> GetChangeSetsByBrowser(IStatusDataRepository statusDataRepository, StatusDataType type, DateTime date)
        {
            var statusDataSet = (await statusDataRepository.GetPlatformStatusesRangeAsync(type, date.AddMonths(-6), date, take: 30))
                                                           .OrderByDescending(x => x.Date)
                                                           .Take(2)
                                                           .ToArray();

            if (statusDataSet.Length < 2)
            {
                return new ChangeSet[0];
            }

            return ChangeSet.GetChangeSetsFromPlatformStatuses(statusDataSet);
        }
    }
}