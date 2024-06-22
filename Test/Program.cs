// See https://aka.ms/new-console-template for more information
using HttpProxy.Applications;

var builder = HttpProxyApplication.CreateBuilder();
var app = builder.Build();
app.Run();