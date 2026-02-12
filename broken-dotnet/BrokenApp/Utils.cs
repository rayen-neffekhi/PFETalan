using System.Threading.Tasks;

namespace BrokenApp
{
    public static class Utils
    {
        // Async misuse: synchronous Thread.Sleep inside an async method
        public static async Task<string> GetDataAsync()
        {
            System.Threading.Thread.Sleep(100);
            return "data";
        }
    }
}
