using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MimeTypes;

namespace HotReloadServer
{
    public class HttpServer : IDisposable
    {

        private static string _notFoundTemplate = "<h2>File not found</h2><p>File {0} not found on server.";
        private static string _methodNotAllowedTemplate = "<h2>{0} method not allowed</h2>";
        private static string _serverErrorTemplate = "<h2>Server Error Occurred</h2><h3>{0}</h3><p>{1}</p>";
        private readonly HttpListener _listener;
        private readonly string _watchDir;
        private readonly int _maxConcurrentRequests;

        public HttpServer(string address, int port, string watchDir, int maxConcurrentRequests = 1)
        {
            _ = address ?? throw new ArgumentNullException(nameof(address));
            this._watchDir = watchDir ?? throw new ArgumentNullException(nameof(watchDir));

            if (!Directory.Exists(this._watchDir))
            {
                throw new ArgumentException($"Directory {this._watchDir} doesn't exist.");
            }

            this._maxConcurrentRequests = maxConcurrentRequests;
            this._listener = new HttpListener();
            this._listener.Prefixes.Add($"http://{address}:{port}/");
        }

        public async Task Run(CancellationToken token)
        {
            this._listener.Start();
            Console.Out.WriteLine($"Serving directory {this._watchDir} on {String.Join(",", this._listener.Prefixes)}");

            var semaphore = new SemaphoreSlim(_maxConcurrentRequests);

            while (this._listener.IsListening)
            {
                await semaphore.WaitAsync();

                var task = this._listener.GetContextAsync().ContinueWith(
                    async contextTask =>
                    {
                        try
                        {
                            await _httpRequestHandler.HandleRequest(contextTask.Result);
                            await ProcessRequest(contextTask.Result);
                        }
                        catch (Exception e)
                        {
                            await AddServerErrorResponse(contextTask.Result.Response, e);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                );
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

        protected virtual async Task ProcessRequest(HttpListenerContext ctx)
        {
            Console.WriteLine($"Request: {ctx.Request.HttpMethod}: {ctx.Request.Url}");
            using (var response = ctx.Response)
            {
                try 
                {
                    if (ctx.Request.HttpMethod != "GET")
                    {
                        await AddMethodNotAllowedResponse(ctx.Response, ctx.Request.HttpMethod);
                        return;
                    }

                    var filePath = GetRequestedFilePath(ctx.Request);
                    Console.WriteLine($"Full file path requested: {filePath}");

                    if (filePath == null)
                    {
                        await AddNotFoundResponse(ctx.Response, ctx.Request.Url.AbsolutePath.Substring(1));
                        return;
                    }

                    Console.WriteLine($"Got request for file: {filePath}");

                    AddFileMetadataToResponse(ctx.Response, filePath);
                    using (var inputStream = File.OpenRead(filePath))
                    {
                        await FinalizeResponse(response, inputStream);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occured while processing request: {e.Message}");
                    await AddServerErrorResponse(response, e);
                }
            }
        }
        protected virtual void AddFileMetadataToResponse(HttpListenerResponse response, string filePath)
        {
            response.ContentType = GetMimeTypeOfFile(filePath);
            response.ContentEncoding = GetEncodingOfFile(filePath);
            FileInfo fileInfo = new FileInfo(filePath);
            response.ContentLength64 = fileInfo.Length;
            response.AddHeader("Last-Modified", fileInfo.LastWriteTime.ToString("r"));
            response.AddHeader("Date", DateTime.Now.ToString("r"));
        }

        protected virtual async Task FinalizeResponse(HttpListenerResponse response, Stream finalResponseBodyStream)
        {
            await finalResponseBodyStream.CopyToAsync(response.OutputStream);
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

        private async Task AddNotFoundResponse(HttpListenerResponse response, string invalidPath)
        {
            var formattedResponse = String.Format(_notFoundTemplate, invalidPath);
            var buffer = Encoding.UTF8.GetBytes(formattedResponse);

            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;

            using (var responseStream = new MemoryStream(buffer))
            {
                await FinalizeResponse(response, new MemoryStream(buffer, true));
            }
        }

        private async Task AddMethodNotAllowedResponse(HttpListenerResponse response, string unallowedMethod)
        {
            var formattedResponse = String.Format(_methodNotAllowedTemplate, unallowedMethod);
            var buffer = Encoding.UTF8.GetBytes(formattedResponse);

            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;

            using (var responseStream = new MemoryStream(buffer))
            {
                await FinalizeResponse(response, new MemoryStream(buffer, true));
            }
        }

        private async Task AddServerErrorResponse(HttpListenerResponse response, Exception exception)
        {

            var formattedResponse = String.Format(_serverErrorTemplate, exception.Message, exception.StackTrace);
            var buffer = Encoding.UTF8.GetBytes(formattedResponse);

            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;

            using (var responseStream = new MemoryStream(buffer))
            {
                await FinalizeResponse(response, new MemoryStream(buffer, true));
            }
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

        private static Encoding GetEncodingOfFile(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.Default, true))
            {
                if (reader.Peek() >= 0)
                {
                    reader.Read();
                }

                return reader.CurrentEncoding;
            }
        }
    }
}