using System.Threading.Tasks;
using Xunit;

namespace FetchPrebuiltUE4Test
{
    public class AuthCommands
    {
        private const string CredentialsFile = "application-default-credentials.json";

        [Fact]
        public void TestClearAuthCommandWhenAuthExists()
        {
            System.IO.File.WriteAllText(CredentialsFile, "hello");

            Task<int> result = FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.Run(new string[] { "clear-auth" });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.False(System.IO.File.Exists(CredentialsFile));
        }

        [Fact]
        public void TestClearAuthCommandWhenAuthDoesNotExist()
        {
            if (System.IO.File.Exists(CredentialsFile))
                System.IO.File.Delete(CredentialsFile);

            Task<int> result = FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.Run(new string[] { "clear-auth" });
            result.Wait();
            Assert.Equal(0, result.Result);

            Assert.False(System.IO.File.Exists(CredentialsFile));
        }
    }
}
