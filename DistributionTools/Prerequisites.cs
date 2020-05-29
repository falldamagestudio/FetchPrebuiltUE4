using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DistributionTools
{
    public static class Prerequisites
    {
        public static async Task<int> RunPrerequisitesInstaller(string UE4Folder)
        {
            string prerequisitesAppName = $"{UE4Folder}/Engine/Extras/Redist/en-us/UE4PrereqSetup_x64.exe";

            if (File.Exists(prerequisitesAppName))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = prerequisitesAppName, Arguments = "/SILENT", UseShellExecute = false };

                System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                ProcessAsyncHelper.Result result = await ProcessAsyncHelper.RunAsync(startInfo);
                stopwatch.Stop();

                Console.WriteLine("Elapsed time: {0}s", (float)stopwatch.ElapsedMilliseconds / 1000.0f);

                return result.ExitCode.HasValue ? result.ExitCode.Value : -1;
            }
            else
                return 0;
        }
    }
}
