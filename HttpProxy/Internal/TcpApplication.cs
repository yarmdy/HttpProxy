using HttpProxy.Enums;
using HttpProxy.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Internal
{
    internal class TcpApplication
    {
        IEndPointProvider _endPointProvider;
        ILogger<TcpApplication> _logger;
        IHttpRequestResolver _requestResolver;
        IHttpResponseResolver _responseResolver;
        public TcpApplication(IEndPointProvider endPointProvider, ILogger<TcpApplication> logger, IHttpRequestResolver requestResolver, IHttpResponseResolver httpResponseResolver)
        {
            _logger = logger;
            _endPointProvider = endPointProvider;
            _requestResolver = requestResolver;
            _responseResolver = httpResponseResolver;
        }

        public Task Start()
        {
            try
            {
                List<Task> tasks = new List<Task>();
                if (_endPointProvider.EndPoint != null)
                {
                    var socket = getSocket(_endPointProvider.EndPoint);
                    tasks.Add(startProxy(socket));
                }
                if (_endPointProvider.EndPointHttp != null)
                {
                    var socket = getSocket(_endPointProvider.EndPointHttp);
                    tasks.Add(startProxy(socket));
                }
                if (_endPointProvider.EndPointHttps != null)
                {
                    var socket = getSocket(_endPointProvider.EndPointHttps);
                    tasks.Add(startProxy(socket));
                }
                _logger.LogInformation("tcp启动成功");
                return Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        private Socket getSocket(EndPoint endPoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            socket.Bind(endPoint);
            return socket;
        }
        private Task startProxy(Socket socket)
        {
            socket.Listen(0);
            return Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var client = socket.Accept();
                        processClient(client);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "套接字错误");
                    }
                }
            });
        }
        private void processClient(Socket client) {
            Task.Run(async () => {
                var clientEndPoint = (client.RemoteEndPoint as IPEndPoint)!;
                _logger.LogInformation($"client {clientEndPoint.Address}:{clientEndPoint.Port} connected");
                try
                {
                    Socket? clientStandIn= new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await clientStandIn.ConnectAsync(new IPEndPoint(IPAddress.Parse("101.226.101.175"), 80));
                    do
                    {
                        var data = await receiveAsync(client);
                        if (data.Length == 0)
                        {
                            break;
                        }
                        //_logger.LogInformation($"client {clientEndPoint.Address}:{clientEndPoint.Port} receive data {data.Length} Byte");
                        var request = _requestResolver.DataToRequest(data);
                        request.Headers["host"][0].value="sogou.com";
                        await clientStandIn.SendAsync(_requestResolver.RequestToData(request));
                        data = await receiveAsync(clientStandIn);
                        var response = _responseResolver.DataToResponse(data);
                        var ret = await client.SendAsync(_responseResolver.ResponseToData(response));
                    } while (true);
                }catch(Exception ex)
                {
                    _logger.LogError(ex, "客户端连接出错");
                }
                _logger.LogInformation($"client {clientEndPoint.Address}:{clientEndPoint.Port} end");
            });
        }

        private async Task<Memory<byte>> receiveAsync(Socket client)
        {
            if (client == null)
            {
                return new Memory<byte>();
            }
            var res = new List<Memory<byte>>();
            do
            {
                Memory<byte> buffer = new byte[102400];
                var len = await client.ReceiveAsync(buffer);
                if (len == 0)
                {
                    break;
                }
                res.Add(buffer.Slice(0,len));
                if (len < buffer.Length)
                {
                    break;
                }
            } while (true);

            return res.SelectMany(a=>a.ToArray()).ToArray();
        }

    }
}
