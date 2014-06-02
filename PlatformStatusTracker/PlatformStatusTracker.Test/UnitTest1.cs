using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformStatusTracker.Core.Data;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Core.Repository;

namespace PlatformStatusTracker.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Deserialize_ModernIe_Json_1()
        {
            var jsonData = File.ReadAllText("TestData\\status-modern-ie_20140529.json");
            var deserializedData = PlatformStatuses.DeserializeForIeStatus(jsonData);
            deserializedData.IsNotNull();
        }

        [TestMethod]
        public void Deserialize_ChromeStatus_Json_1()
        {
            var jsonData = File.ReadAllText("TestData\\chromestatus-com_20140529.json");
            var deserializedData = PlatformStatuses.DeserializeForIeStatus(jsonData);
            deserializedData.IsNotNull();
        }

        //[TestMethod]
        //public void BulkInsert_1()
        //{
        //    Task.WhenAll(
        //        new StatusDataAzureStorageRepository("").InsertAsync(StatusDataType.InternetExplorer, DateTime.Parse("2014/05/29"), File.ReadAllText("TestData\\status-modern-ie_20140529.json")),
        //        new StatusDataAzureStorageRepository("").InsertAsync(StatusDataType.InternetExplorer, DateTime.Parse("2014/05/31"), File.ReadAllText("TestData\\status-modern-ie_20140531.json")),
        //        new StatusDataAzureStorageRepository("").InsertAsync(StatusDataType.Chromium, DateTime.Parse("2014/05/29"), File.ReadAllText("TestData\\chromestatus-com_20140529.json")),
        //        new StatusDataAzureStorageRepository("").InsertAsync(StatusDataType.Chromium, DateTime.Parse("2014/05/31"), File.ReadAllText("TestData\\chromestatus-com_20140531.json"))
        //    ).Wait();
        //}

        //[TestMethod]
        //public void BulkInsert_2()
        //{
        //    var jsons = Directory.EnumerateFiles(@"", "*.json")
        //                         .Select(x => new
        //                                      {
        //                                          Date = new DateTime(Int64.Parse(Path.GetFileNameWithoutExtension(x))).Date,
        //                                          Content = File.ReadAllText(x, Encoding.UTF8)
        //                                      })
        //                         .ToArray();

        //    Task.WhenAll(jsons.Select(x => new StatusDataAzureStorageRepository("").InsertAsync(StatusDataType.InternetExplorer, x.Date, x.Content))).Wait();
        //}
    }
}
