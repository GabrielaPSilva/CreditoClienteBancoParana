using Produtor.Services.Interfaces;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Core;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System;
using Newtonsoft.Json;

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
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "customer_registered_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: new Dictionary<string, object>
                                     {
                             { "x-dead-letter-exchange", "dead_letter_exchange" },
                             { "x-dead-letter-routing-key", "dead_letter" },
                             { "x-message-ttl", 60000 } 
                                     });

                channel.QueueDeclare(queue: "creditLimit_generated_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: new Dictionary<string, object>
                                     {
                             { "x-dead-letter-exchange", "dead_letter_exchange" },
                             { "x-dead-letter-routing-key", "dead_letter" },
                             { "x-message-ttl", 60000 } 
                                     });

                channel.QueueDeclare(queue: "error_notifications_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var customer = System.Text.Json.JsonSerializer.Deserialize<Cliente>(message);

                    Console.WriteLine(customer?.ToString());

                    try
                    {
                        var random = new Random();
                        bool isApproved = random.Next(0, 2) == 1;
                        decimal creditLimit = isApproved ? random.Next(500, 5000) : 0;

                        var propostaMessage = System.Text.Json.JsonSerializer.Serialize(
                                            new PropostaCredito(
                                                      Guid.NewGuid(),
                                                      creditLimit,
                                                      isApproved,
                                                      customer!.Id)
                                            );

                        var propostaBody = Encoding.UTF8.GetBytes(propostaMessage);

                        channel.BasicPublish(exchange: "", routingKey: "creditLimit_generated_queue", basicProperties: null, body: propostaBody);

                        await Task.CompletedTask;

                        Console.WriteLine("Proposta Credito {0}", propostaMessage.ToString());
                    }
                    catch (Exception ex)
                    {
                        var errorNotification = new
                        {
                            ErrorMessage = ex.Message,
                            CustomerId = customer?.Id
                        };

                        var errorNotificationMessage = System.Text.Json.JsonSerializer.Serialize(errorNotification);
                        var errorNotificationBody = Encoding.UTF8.GetBytes(errorNotificationMessage);

                        channel.BasicPublish(exchange: "",
                                             routingKey: "error_notifications_queue",
                                             basicProperties: null,
                                             body: errorNotificationBody);

                        Console.WriteLine("Error {0}", errorNotificationMessage);

                        // Envia mensagem para dead-letter
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                };

                channel.BasicConsume(queue: "customer_registered_queue", autoAck: false, consumer: consumer);

                await Task.CompletedTask;

                Console.ReadLine();
            };

            Console.WriteLine(" Press [enter] to exit.");
        }
    }
}
