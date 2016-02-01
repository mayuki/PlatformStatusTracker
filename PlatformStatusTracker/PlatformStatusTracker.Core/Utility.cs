using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;

namespace PlatformStatusTracker.Core
{
    public class Utility
    {
        public static async Task<T> Measure<T>(Func<Task<T>> taskFactory, string label = "-", [CallerMemberName] string name = "")
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await taskFactory();
            stopwatch.Stop();
            Debug.WriteLine(String.Format("{0}({1}): {2}ms", name, label, stopwatch.ElapsedMilliseconds));
            return result;
        }
        public static async Task Measure(Func<Task> taskFactory, string label = "-", [CallerMemberName] string name = "")
        {
            var stopwatch = Stopwatch.StartNew();
            await taskFactory();
            stopwatch.Stop();
            Debug.WriteLine(String.Format("{0}({1}): {2}ms", name, label, stopwatch.ElapsedMilliseconds));
        }
    }
}