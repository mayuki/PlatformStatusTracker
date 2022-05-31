using PlatformStatusTracker.Core;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Web.ViewModels.Home
{
    public class ChangesViewModel
    {
        public ChangeSetsViewModel ChangeSets { get; init; } = default!;
        public DateTime Date { get; init; }

        public static async Task<ChangesViewModel> CreateAsync(IChangeSetRepository changeSetRepository, DateTime date)
        {
            var edgeChangeSetsTask = GetChangeSetsByBrowser(changeSetRepository, StatusDataType.Edge, date);
            var chromeChangeSetsTask = GetChangeSetsByBrowser(changeSetRepository, StatusDataType.Chromium, date);
            var webkitWebCoreChangeSetsTask = GetChangeSetsByBrowser(changeSetRepository, StatusDataType.WebKitWebCore, date);
            var webkitJavaScriptCoreChangeSetsTask = GetChangeSetsByBrowser(changeSetRepository, StatusDataType.WebKitJavaScriptCore, date);
            var mozillaChangeSetsTask = GetChangeSetsByBrowser(changeSetRepository, StatusDataType.Mozilla, date);

            await Utility.Measure(() => Task.WhenAll(
                edgeChangeSetsTask,
                chromeChangeSetsTask,
                webkitWebCoreChangeSetsTask,
                webkitJavaScriptCoreChangeSetsTask,
                mozillaChangeSetsTask));

            var edgeChangeSets = edgeChangeSetsTask.Result;
            var chromeChangeSets = chromeChangeSetsTask.Result;
            var webkitWebCoreChangeSets = webkitWebCoreChangeSetsTask.Result;
            var webkitJavaScriptCoreChangeSets = webkitJavaScriptCoreChangeSetsTask.Result;
            var mozillaChangeSets = mozillaChangeSetsTask.Result;

            return new ChangesViewModel()
            {
                ChangeSets = new ChangeSetsViewModel()
                {
                    IeChangeSet = edgeChangeSets.Any() ? edgeChangeSets[0] : null,
                    ChromeChangeSet = chromeChangeSets.Any() ? chromeChangeSets[0] : null,
                    WebKitWebCoreChangeSet = webkitWebCoreChangeSets.Any() ? webkitWebCoreChangeSets[0] : null,
                    WebKitJavaScriptCoreChangeSet = webkitJavaScriptCoreChangeSets.Any() ? webkitJavaScriptCoreChangeSets[0] : null,
                    MozillaChangeSet = mozillaChangeSets.Any() ? mozillaChangeSets[0] : null,
                },
                Date = date,
            };
        }

        private static async Task<ChangeSet[]> GetChangeSetsByBrowser(IChangeSetRepository changeSetRepository, StatusDataType type, DateTime date)
        {
            return (await Utility.Measure(() => changeSetRepository.GetChangeSetsRangeAsync(type, date.AddMonths(-6), date, take: 1)))
                                                           .OrderByDescending(x => x.Date)
                                                           .ToArray();
        }
    }
}
