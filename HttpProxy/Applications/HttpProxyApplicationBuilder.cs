using HttpProxy.Interface;
using HttpProxy.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Applications
{
    public class HttpProxyApplicationBuilder
    {
        
        internal HttpProxyApplicationBuilder(string[]? args) {
            injectServices(args);
        }
        public ServiceCollection Services { get;}= new ServiceCollection();
        public HttpProxyApplication Build()
        {
            return new HttpProxyApplication(Services);
        }
        
        private void injectServices(string[]? args) {
            Services.AddLogging(a=>a.AddConsole());
            Services.AddSingleton<IEndPointProvider>(a=>new EndPointProvider(args));
            Services.AddSingleton<TcpApplication>();
        }
    }
}
