using System.Threading.Tasks;

namespace FetchPrebuiltUE4
{
    class Program
    {
        static int Main(string[] args)
        {
            FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib lib = new FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib();
            Task<int> result = lib.Run(args);
            result.Wait();
            return result.Result;
        }
    }
}
