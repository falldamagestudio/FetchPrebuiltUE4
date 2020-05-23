using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace FetchPrebuiltUE4Test
{
    public class UpdateVersionAgainstLocalStore
    {
        private const string PackageName = "TestPackage";

        private const string LocalStoreSource = "LocalStoreSource";
        private const string LocalStore = "LocalStore";

        private const string TestPackageSource = "TestPackageSource";
        private const string TestPackage = "TestPackage";

        private const string DesiredVersionFile = "DesiredUE4Version.json";
        private const string InstalledVersionFile = "InstalledUE4Version.json";

        private void WriteConfigFile(string LocalStore, string InstallFolder)
        {
            string content =
            $@"{{
                ""ClientId"" : "" < client ID, for user authentication> "",
                ""ClientSecret"" : ""<client secret, for user authentication>"",
                ""BlockStorageURI"": ""{LocalStore}"",
                ""VersionIndexStorageURI"": ""{LocalStore}"",
                ""UE4Folder"" : ""{InstallFolder}""
            }}";

            System.IO.File.WriteAllText("FetchPrebuiltUE4.config.json", content);
        }

        private void WriteDesiredVersion(string desiredVersion)
        {
            FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.WriteUE4Version(new FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.UE4Version { BuildId = desiredVersion }, DesiredVersionFile);
        }

        private void WriteInstalledVersion(string desiredVersion)
        {
            FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.WriteUE4Version(new FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.UE4Version { BuildId = desiredVersion }, InstalledVersionFile);
        }

        private string ReadInstalledVersion()
        {
            return FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.ReadUE4Version(InstalledVersionFile).BuildId;
        }

        private void InitializeTestPackage()
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(TestPackage);

            foreach (FileInfo file in directoryInfo.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in directoryInfo.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }

        [Fact]
        public void UpdateWithDifferentVersion()
        {
            FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib lib = new FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib();

            WriteConfigFile(LocalStore, TestPackage);
            WriteDesiredVersion(PackageName);
            WriteInstalledVersion("");

            InitializeTestPackage();

            Assert.False(File.Exists(Path.Combine(new string[] { TestPackage, "hello.txt" })));
            Assert.NotEqual(PackageName, ReadInstalledVersion());

            Task<int> result = lib.Run(new string[] { "update-local-ue4-version" });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.True(File.Exists(Path.Combine(new string[] { TestPackage, "hello.txt" })));
            Assert.Equal(PackageName, ReadInstalledVersion());
        }

        [Fact]
        public void UpdateWithSameVersion()
        {
            FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib lib = new FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib();

            WriteConfigFile(LocalStore, TestPackage);
            WriteDesiredVersion(PackageName);
            WriteInstalledVersion(PackageName);

            InitializeTestPackage();

            Assert.False(File.Exists(Path.Combine(new string[] { TestPackage, "hello.txt" })));
            Assert.Equal(PackageName, ReadInstalledVersion());

            Task<int> result = lib.Run(new string[] { "update-local-ue4-version" });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.False(File.Exists(Path.Combine(new string[] { TestPackage, "hello.txt" })));
            Assert.Equal(PackageName, ReadInstalledVersion());
        }
    }
}
