using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformStatusTracker.Core.Repository;
using PlatformStatusTracker.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using PlatformStatusTracker.Web.Infrastracture.Middlewares;
using PlatformStatusTracker.Web.Infrastracture;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;

namespace PlatformStatusTracker.Web
{
    public class Startup
    {
        public Startup(IHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        private IHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<StartupService>();

            services.Configure<ConnectionStringOptions>(Configuration);

            services.AddTransient<IChangeSetRepository, ChangeSetAzureStorageRepository>();
            services.AddTransient<IStatusRawDataRepository, StatusRawDataAzureStorageRepository>();

            services.AddResponseCaching();

            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.DisableTelemetry = true;
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.EnableAdaptiveSampling = false;
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // Add framework services.
            services.AddMvc()
                .AddMvcOptions(options =>
                {
                    if (!Environment.IsDevelopment())
                    {
                        options.CacheProfiles.Add("DefaultCache", new CacheProfile { Duration = 60 * 60 * 2 });
                    }
                    else
                    {
                        options.CacheProfiles.Add("DefaultCache", new CacheProfile { NoStore = true });
                    }
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            // Redirect: Hostname ----------
            app.UseRedirectToCanonicalHost("platformstatustracker.azurewebsites.net", "platformstatus.io");
            app.UseHttpsRedirection();

            // Static File ----------
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = new LetsEncryptWellKnownContentTypeProvider()
            });

            // Response Caching ----------
            app.UseResponseCaching();

            // ASP.NET MVC Core ----------
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "Home/Feed",
                    pattern: @"Feed",
                    defaults: new { controller = "Home", action = "Feed" });
                endpoints.MapControllerRoute(
                    name: "Home/Changes",
                    pattern: @"Changes/{date:regex(^\d{{4}}-\d{{1,2}}-\d{{1,2}})}",
                    defaults: new { controller = "Home", action = "Changes" });
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}
