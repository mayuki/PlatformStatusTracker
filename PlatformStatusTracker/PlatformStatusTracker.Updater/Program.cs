using System;
using System.Threading.Tasks;
using Cocona;
using Microsoft.Extensions.DependencyInjection;
using PlatformStatusTracker.Core.Configuration;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Repository;

namespace PlatformStatusTracker.Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            CoconaApp.Create()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ConnectionStringOptions>(hostContext.Configuration);
                    services.AddTransient<IChangeSetRepository, ChangeSetAzureStorageRepository>();
                    services.AddTransient<IStatusRawDataRepository, StatusRawDataAzureStorageRepository>();
                    services.AddTransient<DataUpdateAgent>();
                })
                .Run<Program>(args);
        }

        public async Task Run([FromService] DataUpdateAgent agent)
        {
            await agent.UpdateAllAsync();
        }
    }
}
