using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Web.Infrastracture.TagHelpers
{
    [HtmlTargetElement(Attributes = VisibilityAttributeName)]
    public class VisibilityTagHelper : TagHelper
    {
        private const string VisibilityAttributeName = "p-visibility";

        [HtmlAttributeName(VisibilityAttributeName)]
        public bool Visibility { get; set; }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!Visibility)
            {
                output.SuppressOutput();
            }

            return Task.CompletedTask;
        }
    }
}
