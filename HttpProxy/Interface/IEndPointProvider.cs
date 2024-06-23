using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpProxy.Interface
{
    public interface IEndPointProvider
    {
        EndPoint? EndPoint { get; }
        EndPoint? EndPointTo { get; }
        EndPoint? EndPointHttp { get; }
        EndPoint? EndPointHttpTo { get; }
        EndPoint? EndPointHttps { get; }
        EndPoint? EndPointHttpsTo { get; }
    }
}
