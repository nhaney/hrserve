using System;
using System.Threading.Tasks;
using HotReloadServer;

namespace hrserve
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = new HttpFileServer("localhost", 8000, "../");
            var task = Task.Run(() => server.Run());
            Console.WriteLine("Server should be running at localhost:8000");
            await Task.Delay(-1);
        }
    }
}
