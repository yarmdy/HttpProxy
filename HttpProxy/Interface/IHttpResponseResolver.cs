using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Interface
{
    public interface IHttpResponseResolver
    {
        HttpResponse DataToResponse(Memory<byte> data);
        Memory<byte> ResponseToData(HttpResponse response);
    }
    public class HttpResponse
    {
        public string HttpVersion { get; set; }
        public int StateCode { get; set; }
        public string StateName { get; set; }
        public Dictionary<string, (int sort, string value)[]> Headers { get; set; }
        public Memory<byte> Body { get; set; }

        public HttpResponse(string httpVersion, int stateCode, string stateName, Dictionary<string, (int sort, string value)[]> headers, Memory<byte> body)
        {
            HttpVersion = httpVersion;
            StateCode = stateCode;
            StateName = stateName;
            Headers = headers;
            Body = body;
        }
    }
}
