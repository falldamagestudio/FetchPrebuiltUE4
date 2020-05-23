
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static DistributionTools.JsonHelpers;

namespace DistributionTools
{
    public static class Longtail
    {
        [JsonConverter(typeof(ObjectToStringConverter<BlockStorageURI>))]
        public struct BlockStorageURI
        {
            private string URI;

            public BlockStorageURI(string uri)
            {
                URI = uri;
            }

            public static explicit operator string(BlockStorageURI blockStorageURI)
            {
                return blockStorageURI.URI;
            }

            public override string ToString()
            {
                return URI;
            }
        }

        [JsonConverter(typeof(ObjectToStringConverter<VersionIndexStorageURI>))]
        public struct VersionIndexStorageURI
        {
            private string URI;

            public VersionIndexStorageURI(string uri)
            {
                URI = uri;
            }

            public static explicit operator string(VersionIndexStorageURI versionIndexStorageURI)
            {
                return versionIndexStorageURI.URI;
            }

            public override string ToString()
            {
                return URI;
            }
        }

        public struct VersionIndexURI
        {
            private string URI;

            public VersionIndexURI(string uri)
            {
                URI = uri;
            }

            public static explicit operator string(VersionIndexURI versionIndexURI)
            {
                return versionIndexURI.URI;
            }

            public override string ToString()
            {
                return URI;
            }
        }

        private static async Task<bool> RunLongtailCommand(GoogleOAuthFlow.ApplicationDefaultCredentialsFile? applicationDefaultCredentialsFile, string[] args)
        {
            string longtailAppName = "longtail.exe";

            string arguments = string.Join(" ", args);

            ProcessStartInfo startInfo = new ProcessStartInfo { FileName = longtailAppName, Arguments = arguments, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (applicationDefaultCredentialsFile != null)
                startInfo.EnvironmentVariables["GOOGLE_APPLICATION_CREDENTIALS"] = (string)applicationDefaultCredentialsFile;

            Console.WriteLine($"Running command: {startInfo.FileName} {startInfo.Arguments}...");

            System.Diagnostics.Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            ProcessAsyncHelper.Result result = await ProcessAsyncHelper.RunAsync(startInfo, (s) => Console.WriteLine(s), (s) => Console.WriteLine(s));
            stopwatch.Stop();

            Console.WriteLine("Elapsed time: {0}s", (float)stopwatch.ElapsedMilliseconds / 1000.0f);

            return result.ExitCode == 0;
        }

        public static async Task<bool> UpsyncToGSBucket(GoogleOAuthFlow.ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile, BlockStorageURI blockStorageURI, string localPath, VersionIndexURI versionIndexURI)
        {
            return await RunLongtailCommand(applicationDefaultCredentialsFile, new string[] { "upsync", "--source-path", localPath, "--target-path", (string)versionIndexURI, "--storage-uri", (string)blockStorageURI });
        }

        public static async Task<bool> DownsyncFromGSBucket(GoogleOAuthFlow.ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile, BlockStorageURI blockStorageURI, string localPath, VersionIndexURI versionIndexURI)
        {
            return await RunLongtailCommand(applicationDefaultCredentialsFile, new string[] { "downsync", "--source-path", (string)versionIndexURI, "--target-path", localPath, "--storage-uri", (string)blockStorageURI });
        }

        public static async Task<bool> UpsyncToLocalStore(BlockStorageURI blockStorageURI, string localPath, VersionIndexURI versionIndexURI)
        {
            return await RunLongtailCommand(null, new string[] { "upsync", "--source-path", localPath, "--target-path", (string)versionIndexURI, "--storage-uri", (string)blockStorageURI });
        }

        public static async Task<bool> DownsyncFromLocalStore(BlockStorageURI blockStorageURI, string localPath, VersionIndexURI versionIndexURI)
        {
            return await RunLongtailCommand(null, new string[] { "downsync", "--source-path", (string)versionIndexURI, "--target-path", localPath, "--storage-uri", (string)blockStorageURI });
        }
    }
}
