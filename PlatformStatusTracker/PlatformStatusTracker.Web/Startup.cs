﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlatformStatusTracker.Core.Repository;
using PlatformStatusTracker.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using PlatformStatusTracker.Web.Infrastracture.Middlewares;
using PlatformStatusTracker.Web.Infrastracture;
using Microsoft.AspNetCore.HttpOverrides;

namespace PlatformStatusTracker.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
#if DEBUG
                .AddJsonFile($"appsettings.Local.json", optional: true)
                .AddUserSecrets<Startup>()
#endif
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            Environment = env;
        }

        public IConfigurationRoot Configuration { get; }
        private IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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
                        options.CacheProfiles.Add("DefaultCache", new CacheProfile { Duration = 60 * 5 });
                    }
                    else
                    {
                        options.CacheProfiles.Add("DefaultCache", new CacheProfile { NoStore = true });
                    }
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseForwardedHeaders();

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

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
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Home/Feed",
                    template: @"Feed",
                    defaults: new { controller = "Home", action = "Feed" });
                routes.MapRoute(
                    name: "Home/Changes",
                    template: @"Changes/{date:regex(^\d{{4}}-\d{{1,2}}-\d{{1,2}})}",
                    defaults: new { controller = "Home", action = "Changes" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
