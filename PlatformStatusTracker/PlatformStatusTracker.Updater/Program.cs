using System;
using System.Threading.Tasks;
using Cocona;
using Microsoft.Extensions.DependencyInjection;
using PlatformStatusTracker.Core.Configuration;
using PlatformStatusTracker.Core.Enum;
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

        public async Task UpdateDaily([FromService] DataUpdateAgent agent)
        {
            await agent.UpdateAllAsync();
        }

        public async Task UpdateRange([FromService] DataUpdateAgent agent, [Option('t')]StatusDataType statusDataType, DateTime from, DateTime to)
        {
            await agent.UpdateChangeSetByRangeAsync(statusDataType, from, to);
        }
    }
}
