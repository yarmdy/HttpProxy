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
        public TcpApplication(IEndPointProvider endPointProvider, ILogger<TcpApplication> logger)
        {
            _logger = logger;
            _endPointProvider = endPointProvider;
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "套接字错误");
                    }
                }
            });
        }
        private void processClient(Socket client) {
            Task.Run(() => {
                client.BeginReceive();
            });
        }

    }
}
