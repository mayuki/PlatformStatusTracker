using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlatformStatusTracker.Core.Data;
using PlatformStatusTracker.Core.Model;

namespace PlatformStatusTracker.Test
{
    [TestClass]
    public class PlatformStatusTrackingTest
    {
        [TestMethod]
        public void GetChangeSetFromStatuses_TestData_1()
        {
            var changeInfoSet = PlatformStatusTracking.GetChangeInfoSetFromStatuses(
                PlatformStatuses.DeserializeForIeStatus(File.ReadAllText("TestData\\status-modern-ie_20140529.json")),
                PlatformStatuses.DeserializeForIeStatus(File.ReadAllText("TestData\\status-modern-ie_20140531.json"))
            );

        }
        [TestMethod]
        public void GetChangeSetFromStatuses_TestData_2()
        {
            var changeInfoSet = PlatformStatusTracking.GetChangeInfoSetFromStatuses(
                PlatformStatuses.DeserializeForChromiumStatus(File.ReadAllText("TestData\\chromestatus-com_20140529.json")),
                PlatformStatuses.DeserializeForChromiumStatus(File.ReadAllText("TestData\\chromestatus-com_20140531.json"))
            );

        }
        [TestMethod]
        public void GetChangeSetFromStatuses_Empty()
        {
            var changeInfoSet = PlatformStatusTracking.GetChangeInfoSetFromStatuses(
                PlatformStatuses.DeserializeForIeStatus(@"[]"),
                PlatformStatuses.DeserializeForIeStatus(@"[]")
            );

            changeInfoSet.Length.Is(0);
        }

        [TestMethod]
        public void GetChangeSetFromStatuses_AddNewStatus()
        {
            var changeInfoSet = PlatformStatusTracking.GetChangeInfoSetFromStatuses(
                PlatformStatuses.DeserializeForIeStatus(@"[]"),
                PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""Promises (ES6)"",""category"":""JavaScript"",""link"":""https://people.mozilla.org/~jorendorff/es6-draft.html#sec-promise-objects"",""summary"":""Allows easier and cleaner asynchronous coding. Adds the Promise constructor, along with the 'all' and 'race' utility methods to the language itself."",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""In Development"",""iePrefixed"":"""",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")
            );

            changeInfoSet.Length.Is(1);
            changeInfoSet[0].IsAdded.IsTrue();
            changeInfoSet[0].IsRemoved.IsFalse();
            changeInfoSet[0].IsChanged.IsFalse();
            changeInfoSet[0].OldStatus.IsNull();
            changeInfoSet[0].NewStatus.IsNotNull();
            changeInfoSet[0].NewStatus.Name.Is("Promises (ES6)");
        }

        [TestMethod]
        public void GetChangeSetFromStatuses_RemoveStatus()
        {
            var changeInfoSet = PlatformStatusTracking.GetChangeInfoSetFromStatuses(
                PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""Promises (ES6)"",""category"":""JavaScript"",""link"":""https://people.mozilla.org/~jorendorff/es6-draft.html#sec-promise-objects"",""summary"":""Allows easier and cleaner asynchronous coding. Adds the Promise constructor, along with the 'all' and 'race' utility methods to the language itself."",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""In Development"",""iePrefixed"":"""",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]"),
                PlatformStatuses.DeserializeForIeStatus(@"[]")
            );

            changeInfoSet.Length.Is(1);
            changeInfoSet[0].IsAdded.IsFalse();
            changeInfoSet[0].IsRemoved.IsTrue();
            changeInfoSet[0].IsChanged.IsFalse();

            changeInfoSet[0].OldStatus.IsNotNull();
            changeInfoSet[0].OldStatus.Name.Is("Promises (ES6)");

            changeInfoSet[0].NewStatus.IsNull();
        }

        [TestMethod]
        public void GetChangeSetFromStatuses_ChangeStatus_IeStatus_Text()
        {
            var changeInfoSet = PlatformStatusTracking.GetChangeInfoSetFromStatuses(
                PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""Promises (ES6)"",""category"":""JavaScript"",""link"":""https://people.mozilla.org/~jorendorff/es6-draft.html#sec-promise-objects"",""summary"":""Allows easier and cleaner asynchronous coding. Adds the Promise constructor, along with the 'all' and 'race' utility methods to the language itself."",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""In Development"",""iePrefixed"":"""",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]"),
                PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""Promises (ES6)"",""category"":""JavaScript"",""link"":""https://people.mozilla.org/~jorendorff/es6-draft.html#sec-promise-objects"",""summary"":""Allows easier and cleaner asynchronous coding. Adds the Promise constructor, along with the 'all' and 'race' utility methods to the language itself."",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Under Consideration"",""iePrefixed"":"""",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")
            );

            changeInfoSet.Length.Is(1);
            changeInfoSet[0].IsAdded.IsFalse();
            changeInfoSet[0].IsRemoved.IsFalse();
            changeInfoSet[0].IsChanged.IsTrue();

            ((IePlatformStatus)changeInfoSet[0].OldStatus).IsNotNull();
            ((IePlatformStatus)changeInfoSet[0].OldStatus).Name.Is("Promises (ES6)");
            ((IePlatformStatus)changeInfoSet[0].OldStatus).IeStatus.Text.Is("In Development");

            ((IePlatformStatus)changeInfoSet[0].NewStatus).IsNotNull();
            ((IePlatformStatus)changeInfoSet[0].NewStatus).Name.Is("Promises (ES6)");
            ((IePlatformStatus)changeInfoSet[0].NewStatus).IeStatus.Text.Is("Under Consideration");
        }

        [TestMethod]
        public void GetChangeSetFromStatuses_ChangeStatus_IeStatus_TextAndPrefixed_1()
        {
            var changeInfoSet = PlatformStatusTracking.GetChangeInfoSetFromStatuses(
                PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""A"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""10"",""ieUnprefixed"":""""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]"),
                PlatformStatuses.DeserializeForIeStatus(@"[{""name"":""A"",""category"":""JavaScript"",""link"":""http://www.example.com/"",""summary"":""a summary of the status"",""standardStatus"":""Editor's Draft"",""ieStatus"":{""text"":""Shipped"",""iePrefixed"":""10"",""ieUnprefixed"":""11""},""msdn"":"""",""wpd"":"""",""demo"":"""",""id"":5681726336532480}]")
            );

            changeInfoSet.Length.Is(1);
            changeInfoSet[0].IsAdded.IsFalse();
            changeInfoSet[0].IsRemoved.IsFalse();
            changeInfoSet[0].IsChanged.IsTrue();
            
            changeInfoSet[0].OldStatus.IsNotNull();
            changeInfoSet[0].OldStatus.Name.Is("A");
            ((IePlatformStatus)changeInfoSet[0].OldStatus).IeStatus.Text.Is("Shipped");
            ((IePlatformStatus)changeInfoSet[0].OldStatus).IeStatus.IePrefixed.Is("10");
            ((IePlatformStatus)changeInfoSet[0].OldStatus).IeStatus.IeUnprefixed.Is("");

            changeInfoSet[0].NewStatus.IsNotNull();
            changeInfoSet[0].NewStatus.Name.Is("A");
            ((IePlatformStatus)changeInfoSet[0].NewStatus).IeStatus.Text.Is("Shipped");
            ((IePlatformStatus)changeInfoSet[0].NewStatus).IeStatus.IePrefixed.Is("10");
            ((IePlatformStatus)changeInfoSet[0].NewStatus).IeStatus.IeUnprefixed.Is("11");
        }
    }
}
