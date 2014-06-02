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

            await Task.WhenAll(ieChangeSetsTask, chromeChangeSetsTask);

            var ieChangeSets = ieChangeSetsTask.Result;
            var chromeChangeSets = chromeChangeSetsTask.Result;

            return new ChangesViewModel()
            {
                ChangeSets = new ChangeSetsViewModel()
                             {
                                 IeChangeSet = ieChangeSets.Any() ? ieChangeSets[0] : null,
                                 ChromeChangeSet = chromeChangeSets.Any() ? chromeChangeSets[0] : null,
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