using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Web.ViewModels.Home
{
    public class ChangesTableRowViewModel
    {
        public string Title { get; }
        public object Old { get; }
        public object New { get; }
        public bool IsNewOnly { get; }

        public ChangesTableRowViewModel(string title, object newStatus, object oldStatus)
            : this(title, newStatus, oldStatus, isNewOnly: false)
        { }
        public ChangesTableRowViewModel(string title, object newStatus)
            : this(title, newStatus, null, isNewOnly: true)
        { }
        private ChangesTableRowViewModel(string title, object newStatus, object oldStatus, bool isNewOnly)
        {
            Title = title;
            Old = oldStatus;
            New = newStatus;
            IsNewOnly = isNewOnly;
        }
    }
}
