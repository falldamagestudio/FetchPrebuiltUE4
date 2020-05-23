using System.Threading.Tasks;
using Xunit;

namespace FetchPrebuiltUE4Test
{
    public class AuthCommands
    {
        private const string CredentialsFile = "application-default-credentials.json";

        private static void WriteConfigFile()
        {
            const string content =
            @"{
                ""ClientId"" : "" < client ID, for user authentication> "",
                ""ClientSecret"" : ""<client secret, for user authentication>"",
                ""BlockStorageURI"": ""<location where blocks will be stored>"",
                ""VersionIndexStorageURI"": ""<location within which .lvi files will be created>"",
                ""UE4Folder"" : ""<folder where UE4 is supposed to get downloaded to>""
            }";

            System.IO.File.WriteAllText("FetchPrebuiltUE4.config.json", content);
        }

        [Fact]
        public void TestClearAuthCommandWhenAuthExists()
        {
            FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib lib = new FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib();

            WriteConfigFile();

            System.IO.File.WriteAllText(CredentialsFile, "hello");

            Task<int> result = lib.Run(new string[] { "clear-auth" });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.False(System.IO.File.Exists(CredentialsFile));
        }

        [Fact]
        public void TestClearAuthCommandWhenAuthDoesNotExist()
        {
            FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib lib = new FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib();

            WriteConfigFile();

            if (System.IO.File.Exists(CredentialsFile))
                System.IO.File.Delete(CredentialsFile);

            Task<int> result = lib.Run(new string[] { "clear-auth" });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.False(System.IO.File.Exists(CredentialsFile));
        }
    }
}
