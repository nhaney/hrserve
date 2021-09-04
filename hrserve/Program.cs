using System;
using System.Threading.Tasks;
using HotReloadServer;

namespace hrserve
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new BrowserReloadingHttpFileServer("localhost", 8000, "../");
            try 
            {
                Console.WriteLine("Running server on http://localhost:8000");
                server.Run();
            }
            finally
            {
                server.Stop();
            }
        }
    }
}
