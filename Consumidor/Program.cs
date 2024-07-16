using Consumidor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerPropostaCredito>();

var host = builder.Build();
host.Run();

