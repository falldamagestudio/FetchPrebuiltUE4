using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace FetchPrebuiltUE4Test
{
    public class UploadAndDownloadToLocalStore
    {
        private const string PackageName = "TestPackage";

        private const string LocalStoreSource = "LocalStoreSource";
        private const string LocalStore = "LocalStore";

        private const string TestPackageSource = "TestPackageSource";
        private const string TestPackage = "TestPackage";

        private void WriteConfigFile(string LocalStore)
        {
            string content =
            $@"{{
                ""ClientId"" : "" < client ID, for user authentication> "",
                ""ClientSecret"" : ""<client secret, for user authentication>"",
                ""BlockStorageURI"": ""{LocalStore}"",
                ""VersionIndexStorageURI"": ""{LocalStore}"",
                ""UE4Folder"" : ""<folder where UE4 is supposed to get downloaded to>""
            }}";

            System.IO.File.WriteAllText("FetchPrebuiltUE4.config.json", content);
        }

        private void InitializeLocalStore()
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(LocalStore);

            foreach (FileInfo file in directoryInfo.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in directoryInfo.EnumerateDirectories())
            {
                dir.Delete(true);
            }
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
        public void TestUpsyncPackageToLocalStore()
        {
            InitializeLocalStore();

            WriteConfigFile(LocalStore);

            Assert.False(File.Exists(Path.Combine(new string[] { LocalStore, "versions", $"{PackageName}.lvi" })));

            Task<int> result = FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.Run(new string[] { "upload-package", "--folder", TestPackageSource, "--package", PackageName });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.True(File.Exists(Path.Combine(new string[] { LocalStore, "versions", $"{PackageName}.lvi" })));
        }

        [Fact]
        public void TestDownsyncPackageFromLocalStore()
        {
            WriteConfigFile(LocalStoreSource);

            InitializeTestPackage();

            Assert.False(File.Exists(Path.Combine(new string[] { TestPackage, "hello.txt" })));

            Task<int> result = FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.Run(new string[] { "download-package", "--folder", TestPackage, "--package", PackageName });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.True(File.Exists(Path.Combine(new string[] { TestPackage, "hello.txt" })));
        }
    }
}
