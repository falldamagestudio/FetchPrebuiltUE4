using DistributionTools;
using Newtonsoft.Json;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace FetchPrebuiltUE4Lib
{
    public class FetchPrebuiltUE4Lib
    {
        private GoogleOAuthFlow.ApplicationOAuthConfiguration applicationOAuthConfiguration;

        private struct Config
        {
            public OAuth.ClientID ClientID;
            public OAuth.ClientSecret ClientSecret;
            public Longtail.BlockStorageURI BlockStorageURI;
            public Longtail.VersionIndexStorageURI VersionIndexStorageURI;
            public string UE4Folder;
        }

        private Longtail.VersionIndexURI PackageNameToURI(Longtail.VersionIndexStorageURI versionIndexStorageURI, string packageName)
        {
            return new Longtail.VersionIndexURI($"{versionIndexStorageURI}/versions/{packageName}.lvi");
        }

        private Config config;

        private Config ReadConfig()
        {
            string configFile = "FetchPrebuiltUE4.config.json";

            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamReader streamReader = new StreamReader(configFile))
            using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
            {
                return jsonSerializer.Deserialize<Config>(jsonTextReader);
            }
        }

        private void Initialize()
        {
            config = ReadConfig();
            applicationOAuthConfiguration = new GoogleOAuthFlow.ApplicationOAuthConfiguration
            {
                ClientID = config.ClientID,
                ClientSecret = config.ClientSecret,
                ApplicationDefaultCredentialsFile = new GoogleOAuthFlow.ApplicationDefaultCredentialsFile("application-default-credentials.json")
            };
        }

        private RootCommand CreateCommand()
        {
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

            return rootCommand;
        }

        public async Task<int> Run(string[] args)
        {
            Initialize();

            RootCommand rootCommand = CreateCommand();

            Task<int> result = rootCommand.InvokeAsync(args);
            return await result;
        }

        private async Task<int> UploadPackage(string folder, string package)
        {
            await GoogleOAuthFlow.RefreshUserApplicationDefaultCredentials(applicationOAuthConfiguration);

            if (!await Longtail.UpsyncToGSBucket(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile, config.BlockStorageURI, folder, PackageNameToURI(config.VersionIndexStorageURI, package)))
            {
                Console.WriteLine("Tar operation failed.");
                return 1;
            }

            return 0;
        }

        private async Task<int> DownloadPackage(string folder, string package)
        {
            await GoogleOAuthFlow.RefreshUserApplicationDefaultCredentials(applicationOAuthConfiguration);

            if (!await Longtail.DownsyncFromGSBucket(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile, config.BlockStorageURI, folder, PackageNameToURI(config.VersionIndexStorageURI, package)))
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

        private async Task<int> UpdateLocalUE4Version()
        {
            const string installedUE4VersionFile = "InstalledUE4Version.json";
            const string desiredUE4VersionFile = "DesiredUE4Version.json";

            UE4Version installedUE4Version = ReadUE4Version(installedUE4VersionFile);
            UE4Version desiredUE4Version = ReadUE4Version(desiredUE4VersionFile);

            if (installedUE4Version.BuildId != desiredUE4Version.BuildId)
            {
                Console.WriteLine($"Installing UE4 version {desiredUE4Version.BuildId}...");

                await GoogleOAuthFlow.RefreshUserApplicationDefaultCredentials(applicationOAuthConfiguration);

                if (!await Longtail.DownsyncFromGSBucket(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile, config.BlockStorageURI, Path.GetFullPath(config.UE4Folder), PackageNameToURI(config.VersionIndexStorageURI, desiredUE4Version.BuildId)))
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

        private void ClearAuth()
        {
            GoogleOAuthFlow.RemoveApplicationDefaultCredentials(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile);
        }

        private async Task CreateUserAuth()
        {
            await GoogleOAuthFlow.CreateUserApplicationDefaultCredentials(applicationOAuthConfiguration);
        }
    }
}
