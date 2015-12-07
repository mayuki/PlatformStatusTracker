using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlatformStatusTracker.Core.Data;

namespace PlatformStatusTracker.Core.Model
{
    public class PlatformStatusTracking
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prevStatuses"></param>
        /// <param name="currentStatuses"></param>
        /// <returns></returns>
        public static ChangeInfo[] GetChangeInfoSetFromStatuses(IPlatformStatus[] prevStatuses, IPlatformStatus[] currentStatuses)
        {
            // WORKAROUND: ie-status.json has a duplicated id.
            var prevIdAndNames = prevStatuses.Select(x => new { x.Id, x.Name }).ToLookup(k => k.Id, v => v);
            var curIdAndNames = currentStatuses.Select(x => new { x.Id, x.Name }).ToLookup(k => k.Id, v => v);
            Func<IPlatformStatus, String> prevIdMapper = (platformStatus) => prevIdAndNames[platformStatus.Id].Count() > 1 ? platformStatus.Id + "_" + platformStatus.Name : platformStatus.Id.ToString();
            Func<IPlatformStatus, String> curIdMapper = (platformStatus) => curIdAndNames[platformStatus.Id].Count() > 1 ? platformStatus.Id + "_" + platformStatus.Name : platformStatus.Id.ToString();

            var prevStatusIds = prevStatuses.Select(prevIdMapper).ToArray();
            var prevStatusesByName = prevStatuses.ToDictionary(prevIdMapper, v => v);
            var currentStatusIds = currentStatuses.Select(curIdMapper).ToArray();
            var currentStatusesByName = currentStatuses.ToDictionary(curIdMapper, v => v);

            // Deleted or Added Id
            var deletedOrAddedIds = currentStatusIds
                                        .Except(prevStatusIds)
                                        .Concat(prevStatusIds.Except(currentStatusIds))
                                        .ToArray();
            var deletedOrAddedChanges = deletedOrAddedIds.Select(x => ChangeInfo.Create(prevStatusesByName.ContainsKey(x) ? prevStatusesByName[x] : null, currentStatusesByName.ContainsKey(x) ? currentStatusesByName[x] : null));

            // Changed Statuses
            var changedStatusNames = Enumerable.Intersect(currentStatusIds, prevStatusIds)
                                              .Where(x => !prevStatusesByName[x].CompareStatus(currentStatusesByName[x]))
                                              .ToArray();

            return changedStatusNames.Select(x => ChangeInfo.Create(prevStatusesByName[x], currentStatusesByName[x]))
                                        .Concat(deletedOrAddedChanges)
                                        .ToArray();
        }

    }
}
