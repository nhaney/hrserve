namespace HotReloadServer
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Maps HttpListenerRequest and HttpListenerResponse to more conveniant
    /// HttpRequestMessage and HttpResponseMessage classes.
    /// </summary>
    public static class HttpListenerMappingUtils
    {
        internal static HttpRequestMessage GetRequestFromListener(HttpListenerRequest request)
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

        public static async Task PopulateResponseFromResponseMessage(HttpResponseMessage inputResponse, HttpListenerResponse outputResponse)
        {
            outputResponse.ContentLength64 = inputResponse.Content.Headers.ContentLength ?? 0;
            outputResponse.StatusCode = (int)inputResponse.StatusCode;
            outputResponse.ContentType = inputResponse.Content.Headers.ContentType?.ToString();
            outputResponse.ContentEncoding = inputResponse.Content.Headers.ContentEncoding;
            await inputResponse.Content.CopyToAsync(outputResponse.OutputStream);
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