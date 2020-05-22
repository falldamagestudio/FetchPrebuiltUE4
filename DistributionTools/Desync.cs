
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DistributionTools
{
    public class Desync
    {
        public struct BucketURI
        {
            private string URI;

            public BucketURI(string uri)
            {
                URI = uri;
            }

            public static explicit operator string(BucketURI bucketURI)
            {
                return bucketURI.URI;
            }

            public override string ToString()
            {
                return URI;
            }
        }

        public struct HTTPSStoreCredentials
        {
            public readonly OAuth.AccessToken AccessToken;
            public readonly BucketURI BucketURI;

            public HTTPSStoreCredentials(OAuth.AccessToken accessToken, BucketURI bucketURI)
            {
                AccessToken = accessToken;
                BucketURI = bucketURI;
            }

            public override string ToString()
            {
                return $"AccessToken: {AccessToken}, BucketURI: {BucketURI}";
            }
        }

        /// <summary>
        /// Convert names to kebab case, i.e. "my-variable-name"
        /// </summary>
        private class KebabCaseNamingStrategy : NamingStrategy
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="KebabCaseNamingStrategy"/> class.
            /// </summary>
            /// <param name="processDictionaryKeys">
            /// A flag indicating whether dictionary keys should be processed.
            /// </param>
            /// <param name="overrideSpecifiedNames">
            /// A flag indicating whether explicitly specified property names should be processed,
            /// e.g. a property name customized with a <see cref="JsonPropertyAttribute"/>.
            /// </param>
            public KebabCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames)
            {
                ProcessDictionaryKeys = processDictionaryKeys;
                OverrideSpecifiedNames = overrideSpecifiedNames;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="KebabCaseNamingStrategy"/> class.
            /// </summary>
            /// <param name="processDictionaryKeys">
            /// A flag indicating whether dictionary keys should be processed.
            /// </param>
            /// <param name="overrideSpecifiedNames">
            /// A flag indicating whether explicitly specified property names should be processed,
            /// e.g. a property name customized with a <see cref="JsonPropertyAttribute"/>.
            /// </param>
            /// <param name="processExtensionDataNames">
            /// A flag indicating whether extension data names should be processed.
            /// </param>
            public KebabCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames, bool processExtensionDataNames)
                : this(processDictionaryKeys, overrideSpecifiedNames)
            {
                ProcessExtensionDataNames = processExtensionDataNames;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="KebabCaseNamingStrategy"/> class.
            /// </summary>
            public KebabCaseNamingStrategy()
            {
            }

            /// <summary>
            /// Resolves the specified property name.
            /// </summary>
            /// <param name="name">The property name to resolve.</param>
            /// <returns>The resolved property name.</returns>
            protected override string ResolvePropertyName(string name)
            {
                return PascalCaseToKebabCase(name);
            }

            private static string PascalCaseToKebabCase(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return value;

                return Regex.Replace(
                    value,
                    "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])",
                    "-$1",
                    RegexOptions.Compiled)
                    .Trim()
                    .ToLower();
            }
        }

        [JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
        private struct DesyncConfigFile
        {
            [JsonObject(NamingStrategyType = typeof(KebabCaseNamingStrategy))]
            public struct StoreConfig
            {
                public int ErrorRetry;
                public string HttpAuth;
            }

            public Dictionary<string, StoreConfig> StoreOptions;
        }

        private void WriteDesyncConfigFile(List<HTTPSStoreCredentials> storeCredentials, int retryCount, string configFileName)
        {
            DesyncConfigFile configFileContents = new DesyncConfigFile();
            configFileContents.StoreOptions = new Dictionary<string, DesyncConfigFile.StoreConfig>();

            foreach (HTTPSStoreCredentials storeCredential in storeCredentials)
                configFileContents.StoreOptions[(string)storeCredential.BucketURI] = new DesyncConfigFile.StoreConfig { ErrorRetry = retryCount, HttpAuth = $"Bearer {(string)storeCredential.AccessToken}" };

            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamWriter streamWriter = new StreamWriter(configFileName))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                jsonSerializer.Serialize(jsonTextWriter, configFileContents);
            }
        }

        private async Task<bool> RunDesyncCommand(List<HTTPSStoreCredentials> storeCredentials, BucketURI chunkStore, string[] args)
        {
            string desyncAppName = "desync.exe";

            string desyncConfigFileName = "desync.config";

            int retryCount = 5;

            WriteDesyncConfigFile(storeCredentials, retryCount, desyncConfigFileName);

            string arguments = $"--config \"{desyncConfigFileName}\" --store \"{(string)chunkStore}\" {string.Join(" ", args)}";

            ProcessStartInfo startInfo = new ProcessStartInfo { FileName = desyncAppName, Arguments = arguments, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

            Console.WriteLine($"Running command: {startInfo.FileName} {startInfo.Arguments}...");

            ProcessAsyncHelper.Result result = await ProcessAsyncHelper.RunAsync(startInfo, (s) => Console.WriteLine(s), (s) => Console.WriteLine(s));

            return result.ExitCode == 0;
        }

        private async Task<bool> RunDesyncCommand(GoogleOAuthFlow.ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile, BucketURI chunkStore, string[] args)
        {
            string desyncAppName = "desync.exe";

            string arguments = $"--store \"{(string)chunkStore}\" {string.Join(" ", args)}";

            ProcessStartInfo startInfo = new ProcessStartInfo { FileName = desyncAppName, Arguments = arguments, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            startInfo.EnvironmentVariables["GOOGLE_APPLICATION_CREDENTIALS"] = (string)applicationDefaultCredentialsFile;

            Console.WriteLine($"Running command: {startInfo.FileName} {startInfo.Arguments}...");

            System.Diagnostics.Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            ProcessAsyncHelper.Result result = await ProcessAsyncHelper.RunAsync(startInfo, (s) => Console.WriteLine(s), (s) => Console.WriteLine(s));
            stopwatch.Stop();

            Console.WriteLine("Elapsed time: {0}s", (float)stopwatch.ElapsedMilliseconds / 1000.0f);

            return result.ExitCode == 0;
        }

        public async Task<bool> TarToHTTPSStore(List<HTTPSStoreCredentials> storeCredentials, BucketURI chunkStore, string path, string caibxName)
        {
            return await RunDesyncCommand(storeCredentials, chunkStore, new string[] { "tar", "--index", caibxName, path, "--concurrency", "10" });
        }

        public async Task<bool> UntarFromHTTPSStore(List<HTTPSStoreCredentials> storeCredentials, BucketURI chunkStore, string path, string caibxName)
        {
            return await RunDesyncCommand(storeCredentials, chunkStore, new string[] { "untar", "--index", caibxName, path, "--concurrency", "100" });
        }

        public async Task<bool> TarToGSBucket(GoogleOAuthFlow.ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile, BucketURI chunkStore, string path, string caibxName)
        {
            return await RunDesyncCommand(applicationDefaultCredentialsFile, chunkStore, new string[] { "tar", "--index", caibxName, path, "--concurrency", "10" });
        }

        public async Task<bool> UntarFromGSBucket(GoogleOAuthFlow.ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile, BucketURI chunkStore, string path, string caibxName)
        {
            return await RunDesyncCommand(applicationDefaultCredentialsFile, chunkStore, new string[] { "untar", "--index", caibxName, path, "--concurrency", "100" });
        }
    }
}
