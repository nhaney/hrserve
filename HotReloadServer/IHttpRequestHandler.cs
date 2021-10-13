namespace HotReloadServer
{
    using System.Net;
    public interface IHttpRequestHandler
    {
        HttpResponse HandleRequest(HttpListenerRequest request);
    }
}