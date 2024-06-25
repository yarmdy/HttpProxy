using HttpProxy.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Internal
{
    internal class HttpRequestResolver : IHttpRequestResolver
    {
        public HttpRequest DataToRequest(Memory<byte> data)
        {
            var list = data.ToArray().ToList();
            var cur = 0;
            var index = list.FindIndex(x => cur++<list.Count-4 && x=='\r' && list[cur]=='\n' && list[cur+1]=='\r' && list[cur+2]=='\n');
            if (index == -1)
            {
                index = list.Count;
            }
            var headerStr = Encoding.ASCII.GetString(data.Slice(0, index).ToArray());
            var headerArr = headerStr.Split(new[] { "\r\n"},StringSplitOptions.RemoveEmptyEntries);
            var first = headerArr.FirstOrDefault()??"";
            var firstArr = first.Split(' ');
            var method = firstArr.FirstOrDefault()!;
            var path = firstArr.Skip(1).FirstOrDefault()!;
            var httpVersion = firstArr.Skip(2).FirstOrDefault()?.Split('/')?.Skip(1)?.FirstOrDefault()!;
            cur = 0;
            var headers = headerArr.Skip(1).Select(a=>{
                var dic = a.Split(':');
                return new
                {
                    sort = cur,
                    key = dic.FirstOrDefault()?.ToLower()?.Trim()??"",
                    value = dic.Skip(1).FirstOrDefault()?.Trim()??"",
                };
            }).GroupBy(a=>a.key).ToDictionary(a=>a.Key,a=>a.Select(b=>(sort:b.sort,value:b.value)).ToArray());
            var body = Encoding.ASCII.GetString(data.Slice(index).ToArray());
            return new HttpRequest(method,path,httpVersion,headers,body);
        }

        public Memory<byte> RequestToData(HttpRequest request)
        {
            var sb = new StringBuilder();
            sb.Append($"{request.Method} {request.Path} HTTP/{request.HttpVersion}\r\n");
            request.Headers.SelectMany(a => a.Value.Select(b => new { b.sort, a.Key, b.value })).OrderBy(b => b.sort).ToList().ForEach(a =>
                sb.Append($"{a.Key}: {a.value}\r\n")
            );
            sb.Append("\r\n");
            sb.Append(request.Body);
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

    }
}
