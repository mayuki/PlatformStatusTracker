using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Web.Infrastracture.Middlewares
{
    public static class RedirectToCanonicalHostExtension
    {
        public static IApplicationBuilder UseRedirectToCanonicalHost(this IApplicationBuilder app, string matchHostName, string canonicalHostName)
        {
            return app.Use((context, next) =>
            {
                var req = context.Request;
                if (context.Request.Host.Host == matchHostName)
                {
                    var newUrl = new StringBuilder().Append("https://").Append(canonicalHostName).Append(req.PathBase).Append(req.Path).Append(req.QueryString);
                    context.Response.Redirect(newUrl.ToString(), permanent: true);
                    return Task.CompletedTask;
                }

                return next();
            });
        }
    }
}
