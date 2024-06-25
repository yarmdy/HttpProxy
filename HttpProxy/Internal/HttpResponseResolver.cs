using HttpProxy.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Internal
{
    internal class HttpResponseResolver : IHttpResponseResolver
    {
        public HttpResponse DataToResponse(Memory<byte> data)
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
            var httpVersion = firstArr.FirstOrDefault()?.Split('/')?.Skip(1)?.FirstOrDefault()!;
            int.TryParse(firstArr.Skip(1).FirstOrDefault()!, out int stateCode);
            var stateName = firstArr.Skip(2).FirstOrDefault()!;
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
            var body = data.Slice(Math.Min(index + 4, data.Length));
            return new HttpResponse(httpVersion,stateCode,stateName,headers,body);
        }

        public Memory<byte> ResponseToData(HttpResponse response)
        {
            var sb = new StringBuilder();
            sb.Append($"HTTP/{response.HttpVersion} {response.StateCode} {response.StateName}\r\n");
            response.Headers.SelectMany(a => a.Value.Select(b => new { b.sort, a.Key, b.value })).OrderBy(b => b.sort).ToList().ForEach(a =>
                sb.Append($"{a.Key}: {a.value}\r\n")
            );
            sb.Append("\r\n");
            Memory<byte> header = Encoding.ASCII.GetBytes(sb.ToString());
            var res = new byte[header.Length+response.Body.Length];
            var spanHeader = new Memory<byte>(res,0,header.Length);
            var spanBody = new Memory<byte>(res,header.Length,response.Body.Length);
            header.CopyTo(spanHeader);
            response.Body.CopyTo(spanBody);
            return res;
        }

    }
}
