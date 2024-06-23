using HttpProxy.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Applications
{
    public class HttpProxyApplication
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ServiceCollection _serviceCollection;
        private readonly ILogger<HttpProxyApplication> _logger;
        private TcpApplication _tcpApplication=default!;
        public IServiceProvider Services => _serviceProvider;

        public static HttpProxyApplicationBuilder CreateBuilder(string[]? args = null)
        {
            return new(args);
        }
        internal HttpProxyApplication(ServiceCollection services) { 
            _serviceCollection = services;
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<HttpProxyApplication>>();
            _tcpApplication = _serviceProvider.GetRequiredService<TcpApplication>();
        }

        public void Run()
        {
            _tcpApplication.Start().Wait();
        }
        public async void RunAsync()
        {
            await _tcpApplication.Start();
        }
    }
}
