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
        public IChangeInfo[] Changes { get; set; }

        /// <summary>
        /// Status changed from date.
        /// </summary>
        public DateTime From { get; set; }

        public static ChangeSet[] GetChangeSetsFromPlatformStatuses(PlatformStatuses[] platformStatuses)
        {
            // FIXME: We must handle unexpected errors.
            return Enumerable.Range(0, platformStatuses.Length - 1)
                             .Select(x => new { New = platformStatuses[x], Old = platformStatuses[x + 1] })
                             .Select(x =>
                             {
                                 try
                                 {
                                     return new ChangeSet()
                                     {
                                         Date = x.New.Date,
                                         Changes =
                                             PlatformStatusTracking.GetChangeInfoSetFromStatuses(x.Old.Statuses,
                                                 x.New.Statuses)
                                     };
                                 }
                                 catch (Exception)
                                 {
                                     return null;
                                 }
                             })
                             .Where(x => x != null)
                             .ToArray();
        }
    }

    public interface IChangeInfo
    {
        IPlatformStatus OldStatus { get; }
        IPlatformStatus NewStatus { get; }

        Boolean IsChanged { get; }
        Boolean IsAdded { get; }
        Boolean IsRemoved { get; }
    }

    public abstract class ChangeInfo<T> : IChangeInfo where T:IPlatformStatus
    {
        public T OldStatus { get; set; }
        public T NewStatus { get; set; }

        public Boolean IsChanged => OldStatus != null && NewStatus != null;
        public Boolean IsAdded => OldStatus == null && NewStatus != null;
        public Boolean IsRemoved => OldStatus != null && NewStatus == null;

        IPlatformStatus IChangeInfo.OldStatus => OldStatus;
        IPlatformStatus IChangeInfo.NewStatus => NewStatus;

        public ChangeInfo(T oldStatus, T newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }

    public static class ChangeInfo
    {

        public static IChangeInfo Create(IPlatformStatus oldStatus, IPlatformStatus newStatus)
        {
            if (oldStatus is ChromiumPlatformStatus || newStatus is ChromiumPlatformStatus)
            {
                return new ChromiumChangeInfo(oldStatus as ChromiumPlatformStatus, newStatus as ChromiumPlatformStatus);
            }
            if (oldStatus is WebKitPlatformStatus || newStatus is WebKitPlatformStatus)
            {
                return new WebKitChangeInfo(oldStatus as WebKitPlatformStatus, newStatus as WebKitPlatformStatus);
            }
            if (oldStatus is MozillaPlatformStatus || newStatus is MozillaPlatformStatus)
            {
                return new MozillaChangeInfo(oldStatus as MozillaPlatformStatus, newStatus as MozillaPlatformStatus);
            }
            else
            {
                return new IeChangeInfo(oldStatus as IePlatformStatus, newStatus as IePlatformStatus);
            }
        }
    }

    public class IeChangeInfo : ChangeInfo<IePlatformStatus>
    {
        public IeChangeInfo(IePlatformStatus oldStatus, IePlatformStatus newStatus)
            : base(oldStatus, newStatus)
        {}
    }
    public class ChromiumChangeInfo : ChangeInfo<ChromiumPlatformStatus>
    {
        public ChromiumChangeInfo(ChromiumPlatformStatus oldStatus, ChromiumPlatformStatus newStatus)
            : base(oldStatus, newStatus)
        {}
    }
    public class WebKitChangeInfo : ChangeInfo<WebKitPlatformStatus>
    {
        public WebKitChangeInfo(WebKitPlatformStatus oldStatus, WebKitPlatformStatus newStatus)
            : base(oldStatus, newStatus)
        { }
    }

    public class MozillaChangeInfo : ChangeInfo<MozillaPlatformStatus>
    {
        public MozillaChangeInfo(MozillaPlatformStatus oldStatus, MozillaPlatformStatus newStatus)
            : base(oldStatus, newStatus)
        { }
    }
}
