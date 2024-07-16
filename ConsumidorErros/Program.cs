using ConsumidorErros;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerErros>();

var host = builder.Build();
host.Run();
