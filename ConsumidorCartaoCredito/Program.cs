using Consumidor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerCartaoCredito>();

var host = builder.Build();
host.Run();
