﻿using HttpProxy.Enums;
using HttpProxy.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace HttpProxy.Internal
{
    internal class TcpApplication
    {
        IServiceProvider _serviceProvider;
        IEndPointProvider _endPointProvider;
        ILogger<TcpApplication> _logger;
        IHttpRequestResolver _requestResolver;
        IHttpResponseResolver _responseResolver;
        public TcpApplication(IServiceProvider serviceProvider,IEndPointProvider endPointProvider, ILogger<TcpApplication> logger, IHttpRequestResolver requestResolver, IHttpResponseResolver httpResponseResolver)
        {
            _serviceProvider = serviceProvider;
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
                    tasks.Add(startProxy(socket,_endPointProvider.EndPointTo,EnumConnectionType.Tcp));
                }
                if (_endPointProvider.EndPointHttp != null)
                {
                    var socket = getSocket(_endPointProvider.EndPointHttp);
                    tasks.Add(startProxy(socket,_endPointProvider.EndPointHttpTo, EnumConnectionType.Http));
                }
                if (_endPointProvider.EndPointHttps != null)
                {
                    var socket = getSocket(_endPointProvider.EndPointHttps);
                    tasks.Add(startProxy(socket,_endPointProvider.EndPointHttpsTo, EnumConnectionType.Https));
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
        private Task startProxy(Socket socket,EndPoint? endPointTo,EnumConnectionType connectionType)
        {
            socket.Listen(0);
            return Task.Run(() =>
            {
                while (true)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ITcpContext>();
                        context.TcpServer = socket;
                        context.ConnectionType = connectionType;
                        context.MiddleEndPoint = endPointTo;
                        try
                        {
                            var client = socket.Accept();
                            context.TcpClient = client;
                            processClient(context);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "套接字错误");
                        }
                    }
                }
            });
        }
        private void processClient(ITcpContext context) {
            Task.Run(async () => {
                Socket client = context.TcpClient;
                
                var clientEndPoint = (client.RemoteEndPoint as IPEndPoint)!;
                _logger.LogInformation($"client {clientEndPoint.Address}:{clientEndPoint.Port} connected");
                try
                {
                    Socket? clientStandIn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var ipendpoint = context.MiddleEndPoint as IPEndPoint;
                    await clientStandIn.ConnectAsync(ipendpoint!);
                    do
                    {
                        var data = await receiveAsync(client);
                        if (data.Length == 0)
                        {
                            break;
                        }
                        //_logger.LogInformation($"client {clientEndPoint.Address}:{clientEndPoint.Port} receive data {data.Length} Byte");
                        var request = _requestResolver.DataToRequest(data);
                        request.Headers["host"][0].value=$"{_endPointProvider.Domain??ipendpoint!.Address.ToString()}{(ipendpoint!.Port==80 || ipendpoint.Port==443?"":"")}{(ipendpoint!.Port == 80 || ipendpoint.Port == 443 ? "" : ipendpoint.Port+"")}";
                        request.Headers["connection"][0].value="close";
                        //request.Headers["user-agent"] = [(1, "Mozilla/5.0 (iPhone; CPU iPhone OS 15_7_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.6.3 Mobile/15E148 Safari/604.1")];
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
                        
                        if(response.Headers.ContainsKey("transfer-encoding")
                        && response.Headers["transfer-encoding"][0].value == "chunked" && response.Headers["content-type"][0].value.StartsWith("text/html"))
                        {
                            var bodyList = new List<Memory<byte>>();
                            var start = 0;
                            var times = 0;
                            for (int i = 0; i <response.Body.Length;i++)
                            {
                                if (i == 0)
                                {
                                    continue;
                                }
                                if (response.Body.Span[i]!='\n' || response.Body.Span[i-1] != '\r')
                                {
                                    continue;
                                }
                                times++;
                                var tstart = start;
                                start = i + 1;
                                if (times % 2 != 0)
                                {
                                    continue;
                                }
                                var bodySlice = response.Body.Slice(tstart, start - tstart - 2);
                                if (bodySlice.Length == 0)
                                {
                                    continue;
                                }
                                bodyList.Add(bodySlice);
                            }
                            string bodyStr;
                            using (var memoryStream = new MemoryStream(bodyList.SelectMany(a => a.ToArray()).ToArray()))
                            {
                                using var gzip = new GZipStream(memoryStream, CompressionMode.Decompress);
                                using var resualtStream=new MemoryStream();
                                await gzip.CopyToAsync(resualtStream);
                                gzip.Close();
                                bodyStr = Encoding.UTF8.GetString(resualtStream.ToArray());
                                bodyStr = bodyStr.Replace("</body>", """<div style="position: fixed;width: 33vw;height: 25vh;box-sizing: border-box;border: 10px solid #0f0;top: 33vh;left: 33vw;z-index: 9999999999;background: #ffb400;font-size: 30px;text-align: center;line-height: 22vh;">被我劫持了</div></body>""");
                            }
                            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyStr)))
                            {
                                using var resualtStream = new MemoryStream();
                                using var gzip = new GZipStream(resualtStream, CompressionLevel.Optimal);
                                await memoryStream.CopyToAsync(gzip);
                                gzip.Close();
                                var bodyData = resualtStream.ToArray();
                                var bodyLen = (bodyData.Length.ToString("X") + "\r\n").Select(a=>(byte)a).ToArray();
                                var bodyEnd = new byte[] {13,10,48,13,10,13,10 };
                                response.Body =  bodyLen.Concat(bodyData).Concat(bodyEnd).ToArray();
                            }
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
        const string endOfChunked = "0\r\n\r\n";
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
