using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using MimeTypes;

public class HttpFileServer
{
    private readonly string[] indexFileOptions = { 
        "index.html", 
        "index.htm", 
        "default.html", 
        "default.htm" 
    };

    private static string notFoundTemplate = "<h2>File not found</h2><p>File {0} not found on server.";
    private static string methodNotAllowedTemplate = "<h2>{0} method not allowed</h2>";
    private readonly HttpListener listener;
    private readonly string watchDir;

    public HttpFileServer(string address, int port, string watchDir)
    {
        this.listener = new HttpListener();
        this.listener.Prefixes.Add($"http://{address}:{port}/");
        this.watchDir = watchDir;
    }

    public async Task Run()
    {
        this.listener.Start();

        await Console.Out.WriteLineAsync($"Serving directory {this.watchDir} on {this.listener.Prefixes}");

        while (true)
        {
            var ctx = await this.listener.GetContextAsync();

            Console.WriteLine($"{ctx.Request.HttpMethod}: {ctx.Request.Url}");

            if (ctx.Request.HttpMethod == "GET")
            {
                await addFileToResponse(ctx);
            }
            else
            {
                await addMethodNotAllowedResponse(ctx.Response, ctx.Request.HttpMethod);
            }
        }
    }

    private async Task addFileToResponse(HttpListenerContext ctx)
    {
        var requestPath = ctx.Request.Url.AbsolutePath.Substring(1);
        var filePath = Path.Combine(this.watchDir, requestPath);

        if (!File.Exists(filePath))
        {
            // Look to see if this is pointing at the index file of a directory
            var fileExists = false;
            foreach (var indexFileOption in indexFileOptions)
            {
                var indexFilePath = Path.Combine(filePath, indexFileOption);
                if (File.Exists(indexFilePath))
                {
                    filePath = indexFilePath;
                    fileExists = true;
                    break;
                }
            }

            if (!fileExists)
            {
                await addNotFoundResponse(ctx.Response, filePath);
                return;
            }
        }

        Console.WriteLine($"Got request for file: {filePath}");

        ctx.Response.ContentType = getMimeTypeOfFile(filePath);
        ctx.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filePath).ToString("r"));
        ctx.Response.AddHeader("Date", DateTime.Now.ToString("r"));

        using (var sw = new StreamWriter(ctx.Response.OutputStream))
        {
            using (var input = new FileStream(filePath, FileMode.Open))
            {
                await input.CopyToAsync(sw.BaseStream);
            }
        }
    }

    private async Task addNotFoundResponse(HttpListenerResponse response, string invalidPath)
    {
        response.StatusCode = (int)HttpStatusCode.NotFound;
        response.ContentType = "text/html";

        using (var sw = new StreamWriter(response.OutputStream))
        {
            await sw.WriteAsync(String.Format(notFoundTemplate, invalidPath));
        }
    }
    private async Task addMethodNotAllowedResponse(HttpListenerResponse response, string unallowedMethod)
    {
        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
        response.ContentType = "text/html";

        using (var sw = new StreamWriter(response.OutputStream))
        {
            await sw.WriteAsync(String.Format(methodNotAllowedTemplate, unallowedMethod));
        }
    }

    private static string getMimeTypeOfFile(string filePath)
    {
        string mimeType;
        MimeTypeMap.TryGetMimeType(Path.GetExtension(filePath), out mimeType);

        if (mimeType == null)
        {
            mimeType = "application/octet-stream";
        }

        return mimeType;
    }

    public async Task Stop()
    {
        await Console.Out.WriteLineAsync(
            "Stopping server...");

        if (this.listener.IsListening)
        {
            this.listener.Stop();
            this.listener.Close();
        }
    }
}