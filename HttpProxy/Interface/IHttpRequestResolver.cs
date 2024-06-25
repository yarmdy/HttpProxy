using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Interface
{
    public interface IHttpRequestResolver
    {
        HttpRequest DataToRequest(Memory<byte> data);
        Memory<byte> RequestToData(HttpRequest request);
    }
    public class HttpRequest
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string HttpVersion { get; set; }
        public Dictionary<string, (int sort, string value)[]> Headers { get; set; }
        public string Body { get; set; }

        public HttpRequest(string method, string path, string httpVersion, Dictionary<string, (int sort, string value)[]> headers, string body)
        {
            Method = method;
            Path = path;
            HttpVersion = httpVersion;
            Headers = headers;
            Body = body;
        }
    }
}
