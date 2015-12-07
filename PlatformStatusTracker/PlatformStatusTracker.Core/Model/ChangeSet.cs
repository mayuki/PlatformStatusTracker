using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlatformStatusTracker.Core.Data;
using PlatformStatusTracker.Core.Repository;

namespace PlatformStatusTracker.Core.Model
{
    public class ChangeSet
    {
        /// <summary>
        /// Status changed at date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// List of changed statuses.
        /// </summary>
        public ChangeInfo[] Changes { get; set; }

        public static ChangeSet[] GetChangeSetsFromPlatformStatuses(PlatformStatuses[] platformStatuses)
        {
            return Enumerable.Range(0, platformStatuses.Length - 1)
                             .Select(x => new { New = platformStatuses[x], Old = platformStatuses[x + 1] })
                             .Select(x => new ChangeSet() { Date = x.New.Date, Changes = PlatformStatusTracking.GetChangeInfoSetFromStatuses(x.Old.Statuses, x.New.Statuses) })
                             .ToArray();
        }
    }

    public class ChangeInfo
    {
        public IPlatformStatus OldStatus { get; set; }
        public IPlatformStatus NewStatus { get; set; }

        public Boolean IsChanged { get { return OldStatus != null && NewStatus != null; } }
        public Boolean IsAdded { get { return OldStatus == null && NewStatus != null; } }
        public Boolean IsRemoved { get { return OldStatus != null && NewStatus == null; } }

        public ChangeInfo(IPlatformStatus oldStatus, IPlatformStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }

        public static ChangeInfo Create(IPlatformStatus oldStatus, IPlatformStatus newStatus)
        {
            if (oldStatus is ChromiumPlatformStatus || newStatus is ChromiumPlatformStatus)
            {
                return new ChromiumChangeInfo(oldStatus, newStatus);
            }
            if (oldStatus is WebKitPlatformStatus || newStatus is WebKitPlatformStatus)
            {
                return new WebKitChangeInfo(oldStatus, newStatus);
            }
            else
            {
                return new IeChangeInfo(oldStatus, newStatus);
            }
        }
    }
    public class IeChangeInfo : ChangeInfo
    {
        public IeChangeInfo(IPlatformStatus oldStatus, IPlatformStatus newStatus)
            : base(oldStatus, newStatus)
        {}

        public IePlatformStatus OldStatus { get { return base.OldStatus as IePlatformStatus; } }
        public IePlatformStatus NewStatus { get { return base.NewStatus as IePlatformStatus; } }
    }
    public class ChromiumChangeInfo : ChangeInfo
    {
        public ChromiumChangeInfo(IPlatformStatus oldStatus, IPlatformStatus newStatus)
            : base(oldStatus, newStatus)
        {}

        public ChromiumPlatformStatus OldStatus { get { return base.OldStatus as ChromiumPlatformStatus; } }
        public ChromiumPlatformStatus NewStatus { get { return base.NewStatus as ChromiumPlatformStatus; } }
    }
    public class WebKitChangeInfo : ChangeInfo
    {
        public WebKitChangeInfo(IPlatformStatus oldStatus, IPlatformStatus newStatus)
            : base(oldStatus, newStatus)
        { }

        public WebKitPlatformStatus OldStatus { get { return base.OldStatus as WebKitPlatformStatus; } }
        public WebKitPlatformStatus NewStatus { get { return base.NewStatus as WebKitPlatformStatus; } }
    }

    public class MozillaChangeInfo : ChangeInfo
    {
        public MozillaChangeInfo(IPlatformStatus oldStatus, IPlatformStatus newStatus)
            : base(oldStatus, newStatus)
        { }

        public MozillaPlatformStatus OldStatus { get { return base.OldStatus as MozillaPlatformStatus; } }
        public MozillaPlatformStatus NewStatus { get { return base.NewStatus as MozillaPlatformStatus; } }

    }
}
