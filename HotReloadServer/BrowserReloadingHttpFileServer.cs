using System.IO;
using System.Net;

namespace HotReloadServer
{
    public class BrowserReloadingHttpFileServer : HttpFileServer
    {
        public BrowserReloadingHttpFileServer(string address, int port, string watchDir) : base(address, port, watchDir)
        {
        }

        /// <summary>
        /// Detects if there is a websocket request and starts a thread to handle it. If it is a normal request, proceed with
        /// processing like the normal HttpFileServer.
        /// </summary>
        /// <param name="ctx"></param>
        protected override void ProcessRequest(HttpListenerContext ctx)
        {
            if (ctx.Request.IsWebSocketRequest)
            {
                var wsContext = ctx.AcceptWebSocketAsync();
            }
            else
            {
                base.ProcessRequest(ctx);
            }
        }

        protected override void AddFileMetadataToResponse(HttpListenerResponse response, string filePath)
        {
            base.AddFileMetadataToResponse(response, filePath);
        }

        protected override void AddFileContentsToResponseBody(HttpListenerResponse response, string filePath)
        {
            base.AddFileContentsToResponseBody(response, filePath);
        }

        public void RefreshClients()
        {

        }

        private void HandleWebsocketClient()
        {

        }
    }
}