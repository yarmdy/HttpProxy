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
                        request.Headers["connection"][0].value="close";
                        await clientStandIn.SendAsync(_requestResolver.RequestToData(request));
                        data = await receiveAsync(clientStandIn);
                        var response = _responseResolver.DataToResponse(data);
                        Memory<byte>? dataEx=null;
                        if (response.Headers.ContainsKey("content-length") 
                        && int.TryParse(response.Headers["content-length"][0].value, out int maxLen)
                        && maxLen>response.Body.Length)
                        {
                            dataEx = await receiveAsync(clientStandIn, maxLen - response.Body.Length);
                            
                        }
                        if(response.Headers.ContainsKey("transfer-encoding")
                        && response.Headers["transfer-encoding"][0].value=="chunked" && new string(data.Slice(data.Length - 5, 5).ToArray().Select(a => (char)a).ToArray()) != endOfChunked)
                        {
                            dataEx = await receiveAsync(clientStandIn, 0,true);
                            
                        }
                        if (dataEx != null)
                        {
                            var dataRes = new byte[data.Length + dataEx.Value.Length];

                            data.CopyTo(dataRes);
                            dataEx.Value.CopyTo(new Memory<byte>(dataRes, data.Length, dataEx.Value.Length));

                            data = dataRes;
                            response = _responseResolver.DataToResponse(data);
                        }
                        
                        var ret = await client.SendAsync(_responseResolver.ResponseToData(response));
                    } while (true);
                }catch(Exception ex)
                {
                    _logger.LogError(ex, "客户端连接出错");
                }
                _logger.LogInformation($"client {clientEndPoint.Address}:{clientEndPoint.Port} end");
            });
        }
        const string endOfChunked = "0\r\n";
        private async Task<Memory<byte>> receiveAsync(Socket client,int maxlen=0,bool isChunked = false)
        {
            if (client == null)
            {
                return new Memory<byte>();
            }
            var res = new List<byte>();
            do
            {
                Memory<byte> buffer = new byte[10240];
                var len = await client.ReceiveAsync(buffer);
                maxlen -= len;
                if (maxlen < 0)
                {
                    maxlen = 0;
                }
                if (len == 0)
                {
                    break;
                }
                res.AddRange(buffer.Slice(0,len).ToArray());
                if (isChunked)
                {
                    isChunked = new string(res.TakeLast(5).Select(a => (char)a).ToArray())!= endOfChunked;
                }
                if (len < buffer.Length && maxlen==0 && !isChunked)
                {
                    break;
                }
            } while (true);

            return res.ToArray();
        }

    }
}
