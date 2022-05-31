using PlatformStatusTracker.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatformStatusTracker.Web.ViewModels.Home
{
    public class ChangeSetsViewModel
    {
        public bool HideIcon { get; set; }
        public ChangeSet? IeChangeSet { get; set; }
        public ChangeSet? ChromeChangeSet { get; set; }
        public ChangeSet? WebKitWebCoreChangeSet { get; set; }
        public ChangeSet? WebKitJavaScriptCoreChangeSet { get; set; }
        public ChangeSet? MozillaChangeSet { get; set; }
    }
}