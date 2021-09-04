using System;
using System.IO;
using System.Net;
using System.Text;

using MimeTypes;

namespace HotReloadServer
{
    public class HttpFileServer : IDisposable
    {
        private readonly string[] _indexFileOptions = {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private static string _notFoundTemplate = "<h2>File not found</h2><p>File {0} not found on server.";
        private static string _methodNotAllowedTemplate = "<h2>{0} method not allowed</h2>";
        private readonly HttpListener _listener;
        private readonly string _watchDir;

        public HttpFileServer(string address, int port, string watchDir)
        {
            this._listener = new HttpListener();
            this._listener.Prefixes.Add($"http://{address}:{port}/");
            this._watchDir = watchDir;
        }

        public void Run()
        {
            this._listener.Start();

            Console.Out.WriteLine($"Serving directory {this._watchDir} on {this._listener.Prefixes}");

            while (true)
            {
                var ctx = this._listener.GetContext();
                Console.WriteLine($"Request: {ctx.Request.HttpMethod}: {ctx.Request.Url}");
                ProcessRequest(ctx);

            }
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            Console.Out.Write(
                "Stopping server...");

            if (this._listener.IsListening)
            {
                this._listener.Stop();
                this._listener.Close();
            }
        }
        protected virtual void ProcessRequest(HttpListenerContext ctx)
        {
            using (var response = ctx.Response)
            {
                if (ctx.Request.HttpMethod != "GET")
                {
                    AddMethodNotAllowedResponse(ctx.Response, ctx.Request.HttpMethod);
                    return;
                }

                var filePath = GetRequestedFilePath(ctx.Request);

                if (filePath == null)
                {
                    AddNotFoundResponse(ctx.Response, ctx.Request.Url.AbsolutePath.Substring(1));
                    return;
                }

                Console.WriteLine($"Got request for file: {filePath}");

                AddFileMetadataToResponse(ctx.Response, filePath);
                AddFileContentsToResponseBody(ctx.Response, filePath);
            }
        }
        protected virtual void AddFileMetadataToResponse(HttpListenerResponse response, string filePath)
        {
            response.ContentType = GetMimeTypeOfFile(filePath);
            FileInfo fileInfo = new FileInfo(filePath);
            response.ContentLength64 = fileInfo.Length;
            response.AddHeader("Last-Modified", fileInfo.LastWriteTime.ToString("r"));
            response.AddHeader("Date", DateTime.Now.ToString("r"));
        }

        protected virtual void AddFileContentsToResponseBody(HttpListenerResponse response, string filePath)
        {
            using (var inputStream = File.OpenRead(filePath))
            {
                inputStream.CopyTo(response.OutputStream);
            }
        }

        private string? GetRequestedFilePath(HttpListenerRequest request)
        {
            var requestPath = request.Url.AbsolutePath.Substring(1);
            var requestedFilePath = Path.Combine(this._watchDir, requestPath);

            if (!File.Exists(requestedFilePath))
            {
                // Look to see if this is pointing at the index file of a directory
                foreach (var indexFileOption in _indexFileOptions)
                {
                    var indexFilePath = Path.Combine(requestedFilePath, indexFileOption);
                    if (File.Exists(indexFilePath))
                    {
                        return indexFilePath;
                    }
                }

                return null;
            }

            return requestedFilePath;
        }


        private void AddNotFoundResponse(HttpListenerResponse response, string invalidPath)
        {
            var formattedResponse = String.Format(_notFoundTemplate, invalidPath);
            var buffer = Encoding.UTF8.GetBytes(formattedResponse);

            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        private void AddMethodNotAllowedResponse(HttpListenerResponse response, string unallowedMethod)
        {
            var formattedResponse = String.Format(_methodNotAllowedTemplate, unallowedMethod);
            var buffer = Encoding.UTF8.GetBytes(formattedResponse);

            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        private static string GetMimeTypeOfFile(string filePath)
        {
            string mimeType;
            MimeTypeMap.TryGetMimeType(Path.GetExtension(filePath), out mimeType);

            if (mimeType == null)
            {
                mimeType = "application/octet-stream";
            }

            return mimeType;
        }
    }
}