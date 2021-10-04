using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ChangeTrackingPoc
{
    class Logger
    {
        public static async Task LogAsync(Exception ex, string callerMethod, string otherStuff)
        {
            var done = false;
            while (!done)
            {
                try
                {
                    var performance = string.Empty;
                    try
                    {
                        performance = GetPerformance();
                    }
                    catch (Exception exx)
                    {
                        performance = "Exception thrown while getting performance";
                        Console.WriteLine(exx.Message);
                    }

                    if (otherStuff.Length > 1000)
                        otherStuff = otherStuff.Substring(0, 998);
                    await File.AppendAllTextAsync("Errors.txt", $"{DateTime.Now} \n Caller method {callerMethod} \n {ex.Message} \n {otherStuff} \n Diagnostics:\n {performance} \n -------- \n");

                    done = true;
                }
                catch (Exception)
                {
                    await Task.Run(async () => await Task.Delay(3000));
                }
            }
        }

        private static string GetPerformance()
        {
            var performanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var performanceCounter2 = new PerformanceCounter("Processor", "% Privileged Time", "_Total");
            var performanceCounter3 = new PerformanceCounter("Processor", "% Interrupt Time", "_Total");
            var performanceCounter4 = new PerformanceCounter("Processor", "% DPC Time", "_Total");
            var performanceCounter5 = new PerformanceCounter("Memory", "Available MBytes", null);
            var performanceCounter6 = new PerformanceCounter("Memory", "Committed Bytes", null);
            var performanceCounter7 = new PerformanceCounter("Memory", "Commit Limit", null);
            var performanceCounter8 = new PerformanceCounter("Memory", "% Committed Bytes In Use", null);
            var performanceCounter9 = new PerformanceCounter("Memory", "Pool Paged Bytes", null);
            var performanceCounter10 = new PerformanceCounter("Memory", "Pool Nonpaged Bytes", null);
            var performanceCounter11 = new PerformanceCounter("Memory", "Cache Bytes", null);
            var performanceCounter12 = new PerformanceCounter("Paging File", "% Usage", "_Total");
            var performanceCounter13 = new PerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", "_Total");
            var performanceCounter14 = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            var performanceCounter15 = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            var performanceCounter16 = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total");
            var performanceCounter17 = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", "_Total");
            var performanceCounter18 = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            var performanceCounter19 = new PerformanceCounter("Process", "Handle Count", "_Total");
            var performanceCounter20 = new PerformanceCounter("Process", "Thread Count", "_Total");
            var performanceCounter21 = new PerformanceCounter("System", "Context Switches/sec", null);
            var performanceCounter22 = new PerformanceCounter("System", "System Calls/sec", null);
            var performanceCounter23 = new PerformanceCounter("System", "Processor Queue Length", null);
            var array = new[]
            {
                performanceCounter, performanceCounter2, performanceCounter3,
                performanceCounter4, performanceCounter5, performanceCounter6,
                performanceCounter7, performanceCounter8, performanceCounter9,
                performanceCounter10, performanceCounter11,
                performanceCounter12, performanceCounter13,
                performanceCounter14,
                performanceCounter15, performanceCounter16, performanceCounter17,
                performanceCounter18, performanceCounter19, performanceCounter20,
                performanceCounter21,
                performanceCounter22, performanceCounter23
            };
            var performanceString = string.Empty;
            foreach (var performer in array)
            {
                using (performer)
                    performanceString += $"{performer.CategoryName} {performer.CounterName}: {performer.NextValue()}\n";
            }

            var drives = DriveInfo.GetDrives();
            foreach (var info in drives)
            {
                try
                {
                    performanceString += $"Name: {info.Name}\nSize: {info.TotalSize} \n Free space: {info.AvailableFreeSpace}\nDrive Format: {info.DriveFormat}";
                }
                catch (Exception e)
                {
                }
            }

            return performanceString;
        }
    }
}
