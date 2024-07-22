using Produtor.Services.Interfaces;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Core;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System;
using Newtonsoft.Json;
using Polly;

namespace Consumidor
{
    public class WorkerPropostaCredito : BackgroundService
    {
        private readonly ILogger<WorkerPropostaCredito> _logger;
        private readonly IConfiguration _configuration;

        public WorkerPropostaCredito(ILogger<WorkerPropostaCredito> logger,
                      IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var retryPolicy = Policy
                                .Handle<Exception>()
                                .WaitAndRetry(
                                    retryCount: 3,
                                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                                    onRetry: (exception, timeSpan, attempt, context) =>
                                    {
                                        Console.WriteLine($"Tentativa {attempt} falhou. Tentando novamente em {timeSpan.Seconds} segundos.");
                                    }
                                );

            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "cliente_registrado_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                    channel.QueueDeclare(queue: "limite_credito_gerado_queue", durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
                         {
                             { "x-dead-letter-exchange", "dlx" },
                             { "x-dead-letter-routing-key", "credit_proposal_queue_dlx" }
                         });

                    channel.QueueDeclare(queue: "limite_credito_gerado_queue_dlx", durable: true, exclusive: false, autoDelete: false, arguments: null);

                    channel.QueueDeclare(queue: "error_notification_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var customer = System.Text.Json.JsonSerializer.Deserialize<Cliente>(message);

                        Console.WriteLine(customer?.ToString());

                        bool processingSuccess = false;

                        try
                        {
                            retryPolicy.Execute(() =>
                            {
                                var random = new Random();
                                bool isApproved = random.Next(0, 2) == 1;
                                decimal creditLimit = isApproved ? random.Next(500, 5000) : 0;

                                var responseMessage = System.Text.Json.JsonSerializer.Serialize(
                                                    new PropostaCredito(
                                                              Guid.NewGuid(),
                                                              creditLimit,
                                                              isApproved,
                                                              customer!.Id)
                                                    );

                                var responseBytes = Encoding.UTF8.GetBytes(responseMessage);

                                channel.BasicPublish(exchange: "", routingKey: "limite_credito_gerado_queue", basicProperties: null, body: responseBytes);

                                Console.WriteLine("Proposta Credito {0}", responseMessage.ToString());

                                processingSuccess = true;

                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Falha ao processar a mensagem: {ex.Message}. Enviando para DLQ e notificando erro.");
                            var dlqProperties = channel.CreateBasicProperties();
                            dlqProperties.Persistent = true;

                            channel.BasicPublish(exchange: "dlx", routingKey: "limite_credito_gerado_queue_dlx", basicProperties: dlqProperties, body: ea.Body);

                            var errorEvent = new
                            {
                                EventType = "Error",
                                Message = $"Falha ao processar a proposta de crédito: {ex.Message}",
                                Data = Encoding.UTF8.GetString(ea.Body.ToArray())
                            };

                            var errorEventJson = System.Text.Json.JsonSerializer.Serialize(errorEvent);
                            var errorEventBytes = Encoding.UTF8.GetBytes(errorEventJson);

                            var errorProperties = channel.CreateBasicProperties();
                            errorProperties.Persistent = true;

                            channel.BasicPublish(exchange: "", routingKey: "error_notification_queue", basicProperties: errorProperties, body: errorEventBytes);
                        }

                        if (processingSuccess)
                        {
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                        }
                    };

                    channel.BasicConsume(queue: "cliente_registrado_queue", autoAck: true, consumer: consumer);

                    await Task.CompletedTask;

                    Console.ReadLine();
                };
            }
        }
    }
}
