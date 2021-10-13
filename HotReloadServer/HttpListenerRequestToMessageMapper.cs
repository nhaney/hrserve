namespace HotReloadServer
{
    using System.Net;
    using System.Net.Http;

    internal static class HttpListenerRequestToMessageMapper
    {
        internal static HttpRequestMessage FromHttpListenerRequest(HttpListenerRequest request)
        {
            var content = new StreamContent(request.InputStream);
            var method = request.HttpMethod;

            var outputRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), request.Url);
            outputRequest.Content = content;

            foreach (var header in request.Headers.AllKeys)
            {
                if (header == null)
                {
                    continue;
                }

                var headerValues = request.Headers.GetValues(header);

                if (headerValues == null)
                {
                    continue;
                }

                outputRequest.Headers.Add(header, headerValues);
            }
            outputRequest.Version = request.ProtocolVersion;
            return outputRequest;
        }
    }
}