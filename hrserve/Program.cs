using System;
using System.Threading;
using System.Threading.Tasks;
using HotReloadServer;


namespace hrserve
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = new BrowserReloadingHttpFileServer("localhost", 8000, "../");
            var source = new CancellationTokenSource();
            var token = source.Token;
            try 
            {
                Console.WriteLine("Running server on http://localhost:8000");
                await server.Run(token);
            }
            finally
            {
                server.Stop();
            }
        }

        static async Task RefreshAfterSomeTime(BrowserReloadingHttpFileServer server)
        {
            Console.WriteLine("Waiting 10 seconds before refreshing server...");
            Thread.Sleep(10 * 1000);
            Console.WriteLine("Trying to refresh the server");
            await server.RefreshClients();

            Console.WriteLine("Refreshing again after another 10 seconds...");
            Thread.Sleep(10 * 1000);
            await server.RefreshClients();
        }
    }
}
