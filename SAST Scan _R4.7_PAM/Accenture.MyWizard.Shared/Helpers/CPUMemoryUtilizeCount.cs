using Accenture.MyWizard.Ingrain.DataModels.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CONSTANTS = Accenture.MyWizard.Shared.Constants.IngrainAppConstants;

namespace Accenture.MyWizard.Shared.Helpers
{
    public class CPUMemoryUtilizeCount
    {
        private bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        public SystemUsageDetails GetMetrics(string environment, bool isForSaaS)
        {
            SystemUsageDetails res = new SystemUsageDetails();
            if (IsLinux())
            {
                if (!isForSaaS)
                {
                    res.MemoryUsageInMB = GetLinuxRAMusage();
                } else
                {
                    res.MemoryUsageInMB = 0;
                    res.CPUUsage = 0;
                }
                if (environment != CONSTANTS.PAMEnvironment && !isForSaaS)
                    res.CPUUsage = GetLinuxCPUusage();
                return res;
            }
            else
            {
                res.MemoryUsageInMB = 0;
                res.CPUUsage = 0;
                //res.MemoryUsageInMB = GetWindowsRAMusage();
                //res.CPUUsage = GetWindowsCPUusage();
                return res;
            }
        }


        private double GetWindowsRAMusage()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            return Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0) - Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);

        }

        private double GetWindowsCPUusage()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "cpu get loadpercentage /value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var loadPercentage = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);


            double perc = Math.Round(double.Parse(loadPercentage[1]), 0);
            return perc;
        }


        private double GetLinuxRAMusage()
        {
            var output = "";

            var info = new ProcessStartInfo("free -m");
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"free -m\"";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
            }

            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            //return double.Parse(memory[2]);
            var result = Math.Round((double.Parse(memory[2]) / double.Parse(memory[1])) * 100);
            return result;
        }


        private double GetLinuxCPUusage()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"top -bn 2 -d 0.01 | grep '^%Cpu' | tail -n 1 | gawk '{print $2+$4+$6}'\"";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }

            var lines = output.Split("\n");
            var memory = lines[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);


            var cpuperc = double.Parse(memory[0]);
            return cpuperc;
        }

    }
}
