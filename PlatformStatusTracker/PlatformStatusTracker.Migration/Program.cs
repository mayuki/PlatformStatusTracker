using LZ4;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using PlatformStatusTracker.Core.Data;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Repository;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PlatformStatusTracker.Core.Repository.ChangeSetAzureStorageRepository;

namespace PlatformStatusTracker.Migration
{
    class Program
    {
        private static string _connectionString = ConfigurationManager.AppSettings["PlatformStatusTracker:Repository:AzureStoreageConnectionString"];
        private static string _connectionStringV2 = ConfigurationManager.AppSettings["PlatformStatusTracker:Repository:AzureStoreageV2ConnectionString"];

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            await DownloadAllAsync();
            await UploadAllToV2Async();
            await WriteUploadedDataTableAsync();
            Console.ReadLine();
        }

        private static async Task WriteUploadedDataTableAsync()
        {
            var baseDir = "output";

            //foreach (var type in Enum.GetValues(typeof(Core.Enum.StatusDataType)).Cast<Core.Enum.StatusDataType>())
            foreach (var type in Enum.GetValues(typeof(Core.Enum.StatusDataType)).Cast<Core.Enum.StatusDataType>())
            {
                var typeNameV2 = type == StatusDataType.InternetExplorer ? "Edge" : type.ToString();
                var baseDirByPlatform = Path.Combine(baseDir, type.ToString());

                var repo = new ChangeSetAzureStorageRepository(_connectionStringV2);
                string prev = null;
                DateTime prevDate = DateTime.MinValue;
                var entities = new List<ChangeSetEntity>();
                foreach (var file in Directory.GetFiles(baseDirByPlatform).OrderBy(x => x))
                {
                    var date = DateTime.ParseExact(Path.GetFileNameWithoutExtension(file), "yyyyMMdd", null);
                    if (prev == null)
                    {
                        // first time
                        prev = file;
                        prevDate = date;
                        continue;
                    }

                    Console.WriteLine("{0} -> {1}", prev, date);

                    try
                    {
                        var curStatuses = PlatformStatuses.Deserialize(type, File.ReadAllText(file));
                        var prevStatuses = PlatformStatuses.Deserialize(type, File.ReadAllText(prev));
                        var changeInfo = PlatformStatusTracking.GetChangeInfoSetFromStatuses(prevStatuses, curStatuses);

                        var changeInfoJson = JsonConvert.SerializeObject(changeInfo);

                        foreach (var cs in changeInfo)
                        {
                            Console.WriteLine("    - {0}: ", cs.NewStatus?.Name ?? cs.OldStatus?.Name);
                        }

                        var entity = new ChangeSetEntity(type, date, changeInfoJson, prevDate);
                        Console.WriteLine(entity.Content.Length);
                        entities.Add(entity);
                        if (entities.Count > 50)
                        {
                            await repo.InsertAsync(entities);
                            entities.Clear();
                        }

                        prev = file;
                        prevDate = date;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                if (entities.Any())
                {
                    await repo.InsertAsync(entities);
                    entities.Clear();
                }

            }
        }

        public static async Task UploadAllToV2Async()
        {
            var storageAccount = CloudStorageAccount.Parse(_connectionStringV2);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("statuses");
            await container.CreateIfNotExistsAsync();

            var baseDir = "output";
            foreach (var type in Enum.GetValues(typeof(Core.Enum.StatusDataType)).Cast<Core.Enum.StatusDataType>())
            {
                var typeNameV2 = type == StatusDataType.InternetExplorer ? "Edge" : type.ToString();
                var baseDirByPlatform = Path.Combine(baseDir, type.ToString());

                for (var year = 2014; year <= 2017; year++)
                {
                    for (var month = 1; month <= 12; month++)
                    {
                        for (var day = 1; day <= 31; day++)
                        {
                            var date = new DateTime(year, month, day);
                            var filePath = Path.Combine(baseDirByPlatform, $"{date.ToString("yyyyMMdd")}.json");
                            if (File.Exists(filePath))
                            {
                                Console.WriteLine("Uploading {0}", $"{typeNameV2}/{date.ToString("yyyyMMdd")}.json");
                                await container.GetBlockBlobReference($"{typeNameV2}/{date.ToString("yyyyMMdd")}.json").UploadFromFileAsync(filePath, FileMode.Open);
                            }
                        }
                    }
                }
            }
        }

        private static async Task DownloadAllAsync()
        {
            var repo = new StatusDataAzureStorageRepository(_connectionString);
            var baseDir = "output";
            var encoding = new UTF8Encoding(false);

            // Download all data
            foreach (var type in Enum.GetValues(typeof(Core.Enum.StatusDataType)).Cast<Core.Enum.StatusDataType>())
            {
                var baseDirByPlatform = Path.Combine(baseDir, type.ToString());
                if (!Directory.Exists(baseDirByPlatform))
                {
                    Directory.CreateDirectory(baseDirByPlatform);
                }

                for (var year = 2014; year <= 2017; year++)
                {
                    for (var month = 1; month <= 12; month++)
                    {
                        Console.WriteLine("Fetch Type={0}; Range={1}-{2}", type, new DateTime(year, month, 1), new DateTime(year, month, 1).AddMonths(1).AddDays(-1));

                        var statuses = await repo.GetPlatformStatusesRawRangeAsync(type, new DateTime(year, month, 1), new DateTime(year, month, 1).AddMonths(1).AddDays(-1), 31);
                        foreach (var status in statuses)
                        {
                            var outputPath = Path.Combine(baseDirByPlatform, $"{status.Date.ToString("yyyyMMdd")}.json");
                            Console.WriteLine(outputPath);
                            File.WriteAllText(outputPath, status.Data, encoding);
                        }
                    }
                }
            }
        }
    }
}
