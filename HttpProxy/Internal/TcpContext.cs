using HttpProxy.Enums;
using HttpProxy.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Internal
{
    internal class TcpContext : ITcpContext
    {
        public Socket TcpClient { get; set; } = default!;
        public Socket TcpServer { get; set; } = default!;
        public EndPoint? MiddleEndPoint { get; set; }
        public EnumConnectionType ConnectionType { get; set; }
        public Memory<byte>? RequestData { get; set; }
        public Memory<byte>? MiddleData { get; set; }
        public HttpRequest? Request { get; set; }
        public HttpResponse? Response { get; set; }
    }
}
