using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Applications
{
    public class HttpProxyApplication
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ServiceCollection _serviceCollection;
        public IServiceProvider Services => _serviceProvider;

        public static HttpProxyApplicationBuilder CreateBuilder(string[]? args = null)
        {
            return new(args);
        }
        internal HttpProxyApplication(ServiceCollection services) { 
            _serviceCollection = services;
            _serviceProvider = services.BuildServiceProvider();
        }
        public void Run()
        {

        }
    }
}
