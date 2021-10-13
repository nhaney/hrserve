namespace HotReloadServer
{
    using System;
    using System.Net;
    using System.IO;
    using System.Text;
    using MimeTypes;

    public class HttpFileRequestHandler : IHttpRequestHandler
    {
        private readonly string[] _indexFileOptions = {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        public string WatchDir { get; private set; }

        public HttpFileRequestHandler(string watchDir)
        {
            WatchDir = watchDir;
        }

        public HttpResponse HandleRequest(HttpListenerRequest request)
        {
            var response = new HttpResponse();
            var filePath = GetRequestedFilePath(request);

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

        protected virtual void AddFileMetadataToResponse(HttpListenerResponse response, string filePath)
        {
            response.ContentType = GetMimeTypeOfFile(filePath);
            response.ContentEncoding = GetEncodingOfFile(filePath);
            FileInfo fileInfo = new FileInfo(filePath);
            response.ContentLength64 = fileInfo.Length;
            response.AddHeader("Last-Modified", fileInfo.LastWriteTime.ToString("r"));
            response.AddHeader("Date", DateTime.Now.ToString("r"));
        }

        private string? GetRequestedFilePath(HttpListenerRequest request)
        {
            var requestPath = request.Url.AbsolutePath.Substring(1);
            var requestedFilePath = Path.Combine(WatchDir, requestPath);

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