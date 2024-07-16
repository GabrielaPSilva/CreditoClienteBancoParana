using ConsumidorCliente;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerCliente>();

var host = builder.Build();
host.Run();
