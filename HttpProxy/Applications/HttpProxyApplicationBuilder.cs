using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Applications
{
    public class HttpProxyApplicationBuilder
    {
        internal HttpProxyApplicationBuilder(string[]? args) {
            var argslist = args?.ToList() ?? [];
        }
        public ServiceCollection Services { get;}= new ServiceCollection();
        public HttpProxyApplication Build()
        {
            return new HttpProxyApplication(Services);
        }
    }
}
