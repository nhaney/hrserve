namespace HotReloadServer
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// Class that wraps `HttpListenerResponse` to prevent access to the underlying output stream
    /// until the request/response cycle is complete.
    /// </summary>
    public class HttpResponse
    {
        private HttpListenerResponse _response;

        public HttpResponse(HttpListenerResponse response)
        {
            _response = response; 
            ResponseStream = null;
            ResponseMessage = new HttpResponseMessage();
        }

        public Stream? ResponseStream { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }

        public long ContentLength64
        {
            get
            {
                return ResponseMessage.Content.Headers
                return _response.ContentLength64;
            }

            set
            {
                _response.ContentLength64 = value;
            }
        }

        public bool SendChunked
        {
            get
            {
                return _response.SendChunked;
            }

            set
            {
                _response.SendChunked = value;
            }
        }

        public string? RedirectLocation
        {
            get
            {
                return _response.RedirectLocation;
            }

            set
            {
                _response.RedirectLocation = value;
            }
        }


        public Version ProtocolVersion
        {
            get
            {
                return _response.ProtocolVersion;
            }

            set
            {
                _response.ProtocolVersion = value;
            }
        }

        public bool KeepAlive
        {
            get
            {
                return _response.KeepAlive;
            }

            set
            {
                _response.KeepAlive = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return _response.Headers;
            }

            set
            {
                _response.Headers = value;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                return _response.Cookies;
            }

            set
            {
                _response.Cookies = value;
            }
        }

        public string? ContentType
        {
            get
            {
                return _response.ContentType;
            }

            set
            {
                _response.ContentType = value;
            }
        }

        public int StatusCode
        {
            get
            {
                return _response.StatusCode;
            }

            set
            {
                _response.StatusCode = value;
            }
        }

        public string StatusDescription 
        {
            get
            {
                return _response.StatusDescription;
            }

            set
            {
                _response.StatusDescription = value;
            } 
        }

        public Encoding? ContentEncoding 
        {
            get
            {
                return _response.ContentEncoding;
            }

            set
            {
                _response.ContentEncoding = value;
            } 
        }


        public void Abort()
        {
            _response.Abort();
        }

        public void AddHeader(string name, string value)
        {
            _response.AddHeader(name, value);
        }

        public void AppendCookie(Cookie cookie)
        {
            _response.AppendCookie(cookie);
        }

        public void SetCookie(Cookie cookie)
        {
            _response.SetCookie(cookie);
        }

        public void AppendHeader(string name, string value)
        {
            _response.AppendHeader(name, value);
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            _response.CopyFrom(templateResponse);
        }

        public void Redirect(string url)
        {
            _response.Redirect(url);
        }
    }
}