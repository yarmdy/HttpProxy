using HttpProxy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Interface
{
    public interface ITcpContext
    {
        Socket TcpClient { get; set; }
        Socket TcpServer { get; set; }
        EndPoint? MiddleEndPoint { get; set; }
        EnumConnectionType ConnectionType { get; set; }
        Memory<byte>? RequestData { get; set; }
        Memory<byte>? MiddleData { get; set; }
        HttpRequest? Request { get; set; }
        HttpResponse? Response { get; set; }
    }
}
