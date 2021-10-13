using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using HtmlAgilityPack;

namespace HotReloadServer
{
    public class BrowserReloadingHttpFileServer : HttpFileServer
    {
        private static int _socketCounter = 0;
        private readonly string _javaScriptToInject;
        private readonly ConcurrentDictionary<int, Channel<string>> _clientChannels;
        private readonly ConcurrentDictionary<int, Task> _clientTasks;

        public BrowserReloadingHttpFileServer(string address, int port, string watchDir, int maxConcurrentRequests = 1) : base(address, port, watchDir, maxConcurrentRequests)
        {
            _javaScriptToInject = GenerateJavaScriptToInject(address, port);
            _clientChannels = new ConcurrentDictionary<int, Channel<string>>();
            _clientTasks = new ConcurrentDictionary<int, Task>();
        }


        /// <summary>
        /// Refresh every websocket client connected to the server. 
        /// </summary>
        /// <returns></returns>
        public async Task RefreshClients()
        {
            foreach (var clientEntry in _clientChannels)
            {
                Console.WriteLine($"Refreshing client {clientEntry.Key}");
                await clientEntry.Value.Writer.WriteAsync("test");
            }
        }

        /// <summary>
        /// Detects if there is a websocket request and starts a thread to handle it. If it is a normal request, proceed with
        /// processing like the normal HttpFileServer.
        /// </summary>
        /// <param name="ctx"></param>
        protected override async Task ProcessRequest(HttpListenerContext ctx)
        {
            if (ctx.Request.IsWebSocketRequest)
            {
                try
                {
                    var wsContext = await ctx.AcceptWebSocketAsync(null);
                    int socketId = Interlocked.Increment(ref _socketCounter);
                    var socketChannel = Channel.CreateUnbounded<string>();
                    _clientChannels.TryAdd(socketId, socketChannel);

                    Console.WriteLine($"Socket {socketId}: New connection.");
                    _ = HandleWebsocketClient(wsContext, socketId);
                }
                catch (Exception)
                {
                    // server error if upgrade from HTTP to WebSocket fails
                    ctx.Response.StatusCode = 500;
                    ctx.Response.Close();
                    return;
                }
            }
            else
            {
                await base.ProcessRequest(ctx);
            }
        }

        protected override void AddFileMetadataToResponse(HttpListenerResponse response, string filePath)
        {
            base.AddFileMetadataToResponse(response, filePath);
        }

        protected override async Task FinalizeResponse(HttpListenerResponse response, Stream finalResponseBodyStream)
        {
            if (response.ContentType != "text/html")
            {
                await base.FinalizeResponse(response, finalResponseBodyStream);
                return;
            }

            // inject the javascript into the response
            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(finalResponseBodyStream);

            var scriptElement = HtmlNode.CreateNode($"<script>\n{_javaScriptToInject}\n</script>");
            var headElement = htmlDoc.DocumentNode.SelectSingleNode("//head");
            var htmlElement = htmlDoc.DocumentNode.SelectSingleNode("//html") ?? htmlDoc.DocumentNode;

            if (headElement == null)
            {
                headElement = HtmlNode.CreateNode("<head></head>");
                htmlElement.PrependChild(headElement);
            }

            headElement.AppendChild(scriptElement);

            // set the new content length of the injected html file
            if (response.ContentEncoding == null)
            {
                throw new Exception("Content encoding not set before injecting javascript!");
            }
            response.ContentLength64 = response.ContentEncoding.GetBytes(htmlDoc.DocumentNode.OuterHtml).Length;

            htmlDoc.Save(response.OutputStream);
        }


        private string GenerateJavaScriptToInject(string address, int port)
        {
            var javaScriptTemplate = @"var webSocket = new WebSocket('ws://{0}:{1}');
webSocket.onmessage = function (event) {{
    console.log('Refreshing...');
    window.location.reload();
}};";
            return String.Format(javaScriptTemplate, address, port);
        }
        
        private async Task HandleWebsocketClient(HttpListenerWebSocketContext ctx, int id)
        {
            using (var webSocket = ctx.WebSocket)
            {
                await _clientChannels[id].Reader.ReadAsync();
                var buffer = Encoding.ASCII.GetBytes("refresh");
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                Console.WriteLine($"Socket client {id} has been refreshed");
                _clientChannels.TryRemove(id, out _);
            }
        }
    }
}