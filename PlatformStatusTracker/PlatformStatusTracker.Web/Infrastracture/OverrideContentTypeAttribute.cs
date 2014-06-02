using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PlatformStatusTracker.Web.Infrastracture
{
    public class OverrideContentTypeAttribute : ActionFilterAttribute
    {
        public String ContentType { get; set; }
        public OverrideContentTypeAttribute(String contentType)
        {
            ContentType = contentType;
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            filterContext.HttpContext.Response.ContentType = ContentType;
            base.OnResultExecuted(filterContext);
        }
    }
}