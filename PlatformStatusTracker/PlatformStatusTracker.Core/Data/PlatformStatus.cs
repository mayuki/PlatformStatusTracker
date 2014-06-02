using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PlatformStatusTracker.Core.Data
{
    public class PlatformStatuses
    {
        public static IePlatformStatus[] DeserializeForIeStatus(string jsonValue)
        {
            return JsonConvert.DeserializeObject<IePlatformStatus[]>(jsonValue);
        }
        public static ChromiumPlatformStatus[] DeserializeForChromiumStatus(string jsonValue)
        {
            return JsonConvert.DeserializeObject<ChromiumPlatformStatus[]>(jsonValue);
        }

        public DateTime Date { get; private set; }
        public PlatformStatus[] Statuses { get; private set; }

        public PlatformStatuses(DateTime date, PlatformStatus[] statuses)
        {
            Date = date;
            Statuses = statuses;
        }
    }

    public class PlatformStatus
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("category")]
        public string Category { get; set; }
        [JsonProperty("summary")]
        public string Summary { get; set; }
        [JsonProperty("id")]
        public long? Id { get; set; }
        [JsonProperty("impl_status_chrome")]
        public string ImplStatusChrome { get; set; }
        [JsonProperty("ff_views")]
        public ViewsStatus FfViews { get; set; }
        [JsonProperty("safari_views")]
        public ViewsStatus SafariViews { get; set; }

        public virtual Boolean CompareStatus(PlatformStatus status)
        {
            return false;
        }
    }

    public class IePlatformStatus : PlatformStatus
    {
        [JsonProperty("link")]
        public string Link { get; set; }
        [JsonProperty("summary")]
        public string StandardStatus { get; set; }
        [JsonProperty("msdn")]
        public string Msdn { get; set; }
        [JsonProperty("wpd")]
        public string Wpd { get; set; }
        [JsonProperty("demo")]
        public string Demo { get; set; }
        [JsonProperty("opera_views")]
        public ViewsStatus OperaViews { get; set; }
        [JsonProperty("ieStatus")]
        public IeStatus IeStatus { get; set; }

        public override Boolean CompareStatus(PlatformStatus status)
        {
            var ieStatus = status as IePlatformStatus;
            return this.IeStatus.IePrefixed == ieStatus.IeStatus.IePrefixed &&
                   this.IeStatus.IeUnprefixed == ieStatus.IeStatus.IeUnprefixed &&
                   this.IeStatus.Text == ieStatus.IeStatus.Text;
        }
    }

    public class ChromiumPlatformStatus : PlatformStatus
    {
        [JsonProperty("bug_url")]
        public string BugUrl { get; set; }
        [JsonProperty("ff_views_link")]
        public string FfViewsLink { get; set; }
        [JsonProperty("ie_views")]
        public ViewsStatus IeViews { get; set; }
        [JsonProperty("prefixed")]
        public bool Prefixed { get; set; }

        [JsonProperty("shipped_android_milestone")]
        public int? ShippedAndroidMilestone { get; set; }
        [JsonProperty("shipped_ios_milestone")]
        public int? ShippedIosMilestone { get; set; }
        [JsonProperty("shipped_milestone")]
        public int? ShippedMilestone { get; set; }
        [JsonProperty("shipped_opera_android_milestone")]
        public int? ShippedOperaAndroidMilestone { get; set; }
        [JsonProperty("shipped_opera_milestone")]
        public int? ShippedOperaMilestone { get; set; }
        [JsonProperty("shipped_webview_milestone")]
        public int? ShippedWebViewMilestone { get; set; }

        [JsonProperty("spec_link")]
        public string SpecLink { get; set; }
        [JsonProperty("standardization")]
        public ViewsStatus Standardization { get; set; }
        [JsonProperty("web_dev_views")]
        public ViewsStatus WebDevViews { get; set; }

        public override Boolean CompareStatus(PlatformStatus status)
        {
            var chStatus = status as ChromiumPlatformStatus;
            return this.Prefixed == chStatus.Prefixed &&
                    this.ImplStatusChrome == chStatus.ImplStatusChrome &&
                    this.ShippedAndroidMilestone == chStatus.ShippedAndroidMilestone &&
                    this.ShippedIosMilestone == chStatus.ShippedIosMilestone &&
                    this.ShippedMilestone == chStatus.ShippedMilestone &&
                    this.ShippedOperaAndroidMilestone == chStatus.ShippedOperaAndroidMilestone &&
                    this.ShippedOperaMilestone == chStatus.ShippedOperaMilestone &&
                    this.ShippedWebViewMilestone == chStatus.ShippedWebViewMilestone
                ;
        }
    }

    public class IeStatus
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("iePrefixed")]
        public string IePrefixed { get; set; }
        [JsonProperty("ieUnprefixed")]
        public string IeUnprefixed { get; set; }
    }

    public class ViewsStatus
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("value")]
        public int Value { get; set; }
    }
}
