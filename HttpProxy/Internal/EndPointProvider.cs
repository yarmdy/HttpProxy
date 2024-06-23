using HttpProxy.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Internal
{
    internal class EndPointProvider : IEndPointProvider
    {
        public EndPointProvider(string[]? args)
        {
            config(args?.ToList() ?? []);
            if (_endPoint == null && _endPointHttp == null && _endPointHttps == null)
            {
                _endPointHttp = new IPEndPoint(IPAddress.Any, 80);
                _endPointHttps = new IPEndPoint(IPAddress.Any, 443);
            }
        }
        private EndPoint? _endPoint;
        private EndPoint? _endPointHttp;
        private EndPoint? _endPointHttps;

        private EndPoint? _endPointTo;
        private EndPoint? _endPointHttpTo;
        private EndPoint? _endPointHttpsTo;

        public EndPoint? EndPoint => _endPoint;
        public EndPoint? EndPointHttp => _endPointHttp;
        public EndPoint? EndPointHttps => _endPointHttps;

        public EndPoint? EndPointTo => _endPointTo;
        public EndPoint? EndPointHttpTo => _endPointHttpTo;
        public EndPoint? EndPointHttpsTo => _endPointHttpsTo;

        EndPoint? createEndPoint(string? str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            var arr = str.Split(':');
            if (arr.Length < 2)
            {
                return null;
            }
            if (!IPAddress.TryParse(arr[0], out IPAddress? address) || !int.TryParse(arr[1], out int port) || port < 1 || port > 65534)
            {
                return null;
            }
            return new IPEndPoint(address!, port);
        }
        private void config(List<string> args)
        {
            var tcpIndex = args.FindIndex(a => a.ToLower() == "-tcp");
            if (tcpIndex != -1)
            {
                var str = args.Skip(tcpIndex + 1).FirstOrDefault();
                _endPoint = createEndPoint(str);
            }
            var httpIndex = args.FindIndex(a => a.ToLower() == "-http");
            if (httpIndex != -1)
            {
                var str = args.Skip(httpIndex + 1).FirstOrDefault();
                _endPointHttp = createEndPoint(str);
            }
            var httpsIndex = args.FindIndex(a => a.ToLower() == "-https");
            if (httpsIndex != -1)
            {
                var str = args.Skip(httpsIndex + 1).FirstOrDefault();
                _endPointHttps = createEndPoint(str);
            }

            var tcpToIndex = args.FindIndex(a => a.ToLower() == "-tcpto");
            if (tcpToIndex != -1)
            {
                var str = args.Skip(tcpToIndex + 1).FirstOrDefault();
                _endPointTo = createEndPoint(str);
            }
            var httpToIndex = args.FindIndex(a => a.ToLower() == "-httpto");
            if (httpToIndex != -1)
            {
                var str = args.Skip(httpToIndex + 1).FirstOrDefault();
                _endPointHttpTo = createEndPoint(str);
            }
            var httpsToIndex = args.FindIndex(a => a.ToLower() == "-httpsto");
            if (httpsToIndex != -1)
            {
                var str = args.Skip(httpsToIndex + 1).FirstOrDefault();
                _endPointHttpsTo = createEndPoint(str);
            }

        }
    }
}
