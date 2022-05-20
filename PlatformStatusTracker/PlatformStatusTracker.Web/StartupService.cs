using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlatformStatusTracker.Core.Configuration;

namespace PlatformStatusTracker.Web
{
    public class StartupService : IHostedService
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public StartupService(IHostEnvironment hostEnvironment, IConfiguration configuration, ILogger<StartupService> logger)
        {
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{_hostEnvironment.ApplicationName}; Environment={_hostEnvironment.EnvironmentName}; EntryAssemblyName={Assembly.GetEntryAssembly()!.FullName!}");
            _logger.LogInformation("Configurations:\n" + string.Join("\n", ConfigurationHelper.BuildConfigurationLog(_configuration).Select(x => x.Key + ": " + x.Value)));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}