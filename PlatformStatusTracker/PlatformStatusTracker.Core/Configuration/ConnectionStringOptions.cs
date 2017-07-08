using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformStatusTracker.Core.Configuration
{
    public class ConnectionStringOptions
    {
        public string UpdateKey { get; set; }
        public string AzureStoreageConnectionString { get; set; }
    }
}
