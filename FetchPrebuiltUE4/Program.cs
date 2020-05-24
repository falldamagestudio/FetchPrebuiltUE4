using System.Threading.Tasks;

namespace FetchPrebuiltUE4
{
    class Program
    {
        static int Main(string[] args)
        {
            Task<int> result = FetchPrebuiltUE4Lib.FetchPrebuiltUE4Lib.Run(args);
            result.Wait();
            return result.Result;
        }
    }
}
