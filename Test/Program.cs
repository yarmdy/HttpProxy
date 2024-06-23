// See https://aka.ms/new-console-template for more information
using HttpProxy.Applications;

var builder = HttpProxyApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();