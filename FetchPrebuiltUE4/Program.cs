using DistributionTools;
using System;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using Newtonsoft.Json;
using System.IO;

namespace FetchPrebuiltUE4
{
    class Program
    {
        private static GoogleOAuthFlow.ApplicationOAuthConfiguration applicationOAuthConfiguration;

        private struct Config
        {
            public OAuth.ClientID ClientID;
            public OAuth.ClientSecret ClientSecret;
            public string ChunkStoreURI;
            public string IndexStoreURI;
            public string UE4Folder;
        }

        private static Config config;

        private static Config ReadConfig()
        {
            string configFile = "FetchPrebuiltUE4.config.json";

            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamReader streamReader = new StreamReader(configFile))
            using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
            {
                return jsonSerializer.Deserialize<Config>(jsonTextReader);
            }
        }

        static int Main(string[] args)
        {
            config = ReadConfig();
            applicationOAuthConfiguration = new GoogleOAuthFlow.ApplicationOAuthConfiguration {
                ClientID = config.ClientID,
                ClientSecret = config.ClientSecret,
                ApplicationDefaultCredentialsFile = new GoogleOAuthFlow.ApplicationDefaultCredentialsFile("application-default-credentials.json")
            };

            RootCommand rootCommand = new RootCommand();

            Command uploadPackage = new Command("upload-package");
            uploadPackage.Add(new Option<string>("--folder") { Required = true });
            uploadPackage.Add(new Option<string>("--package") { Required = true });
            uploadPackage.Handler = CommandHandler.Create<string, string>(UploadPackage);
            rootCommand.Add(uploadPackage);

            Command downloadPackage = new Command("download-package");
            downloadPackage.Add(new Option<string>("--folder") { Required = true });
            downloadPackage.Add(new Option<string>("--package") { Required = true });
            downloadPackage.Handler = CommandHandler.Create<string, string>(DownloadPackage);
            rootCommand.Add(downloadPackage);

            Command updateLocalUE4Version = new Command("update-local-ue4-version");
            updateLocalUE4Version.Handler = CommandHandler.Create(UpdateLocalUE4Version);
            rootCommand.Add(updateLocalUE4Version);

            Command clearAuth = new Command("clear-auth");
            clearAuth.Handler = CommandHandler.Create(ClearAuth);
            rootCommand.Add(clearAuth);

            Command createUserAuth = new Command("create-user-auth");
            createUserAuth.Handler = CommandHandler.Create(CreateUserAuth);
            rootCommand.Add(createUserAuth);

            Task<int> result = rootCommand.InvokeAsync(args);
            result.Wait();

            return result.Result;
        }

        private static async Task<int> UploadPackage(string folder, string package)
        {
            await GoogleOAuthFlow.RefreshUserApplicationDefaultCredentials(applicationOAuthConfiguration);

            Desync desync = new Desync();

            string caidxName = $"{config.IndexStoreURI}/{package}.caidx";

            if (!await desync.TarToGSBucket(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile, new Desync.BucketURI(config.ChunkStoreURI), folder, caidxName))
            {
                Console.WriteLine("Tar operation failed.");
                return 1;
            }

            return 0;
        }

        private static async Task<int> DownloadPackage(string folder, string package)
        {
            await GoogleOAuthFlow.RefreshUserApplicationDefaultCredentials(applicationOAuthConfiguration);

            Desync desync = new Desync();

            string caidxName = $"{config.IndexStoreURI}/{package}.caidx";

            if (!await desync.UntarFromGSBucket(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile, new Desync.BucketURI(config.ChunkStoreURI), folder, caidxName))
            {
                Console.WriteLine("Untar operation failed.");
                return 1;
            }

            return 0;
        }

        private struct UE4Version
        {
            public string BuildId;
        }

        private static UE4Version ReadUE4Version(string versionFile)
        {
            JsonSerializer jsonSerializer = new JsonSerializer();

            try
            {
                using (StreamReader streamReader = new StreamReader(versionFile))
                using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                {
                    return jsonSerializer.Deserialize<UE4Version>(jsonTextReader);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // credentials file does not exist
                return default(UE4Version);
            }
        }

        private static void WriteUE4Version(UE4Version version, string versionFile)
        {
            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamWriter streamWriter = new StreamWriter(versionFile))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                jsonSerializer.Serialize(jsonTextWriter, version);
            }
        }

        private static async Task<int> UpdateLocalUE4Version()
        {
            const string installedUE4VersionFile = "InstalledUE4Version.json";
            const string desiredUE4VersionFile = "DesiredUE4Version.json";

            UE4Version installedUE4Version = ReadUE4Version(installedUE4VersionFile);
            UE4Version desiredUE4Version = ReadUE4Version(desiredUE4VersionFile);

            if (installedUE4Version.BuildId != desiredUE4Version.BuildId)
            {
                Console.WriteLine($"Installing UE4 version {desiredUE4Version.BuildId}...");

                await GoogleOAuthFlow.RefreshUserApplicationDefaultCredentials(applicationOAuthConfiguration);

                Desync desync = new Desync();

                string caidxName = $"{config.IndexStoreURI}/{desiredUE4Version.BuildId}.caidx";

                if (!await desync.UntarFromGSBucket(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile, new Desync.BucketURI(config.ChunkStoreURI), Path.GetFullPath(config.UE4Folder), caidxName))
                {
                    Console.WriteLine("Download failed.");
                    return 1;
                }
                else
                {
                    WriteUE4Version(desiredUE4Version, installedUE4VersionFile);
                    Console.WriteLine($"UE4 version {desiredUE4Version.BuildId} has been installed");
                    return 0;
                }
            }
            else
            {
                Console.WriteLine($"UE4 version {desiredUE4Version.BuildId} is already installed");
                return 0;
            }
        }

        private static void ClearAuth()
        {
            GoogleOAuthFlow.RemoveApplicationDefaultCredentials(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile);
        }

        private static async Task CreateUserAuth()
        {
            await GoogleOAuthFlow.CreateUserApplicationDefaultCredentials(applicationOAuthConfiguration);
        }
    }
}
