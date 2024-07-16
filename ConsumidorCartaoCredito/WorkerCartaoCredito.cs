using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Core;

namespace Consumidor
{
    public class WorkerCartaoCredito : BackgroundService
    {
        private readonly ILogger<WorkerCartaoCredito> _logger;
        private readonly IConfiguration _configuration;

        public WorkerCartaoCredito(ILogger<WorkerCartaoCredito> logger,
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
                             { "x-message-ttl", 60000 } // TTL de 60 segundos para retries
                                     });

                channel.QueueDeclare(queue: "credit_card_generated_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: new Dictionary<string, object>
                                     {
                             { "x-dead-letter-exchange", "dead_letter_exchange" },
                             { "x-dead-letter-routing-key", "dead_letter" },
                             { "x-message-ttl", 60000 } // TTL de 60 segundos para retries
                                     });

                channel.QueueDeclare(queue: "error_notifications_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var customer = JsonSerializer.Deserialize<Cliente>(message);

                    Console.WriteLine(customer?.ToString());

                    try
                    {
                        var random = new Random();
                        bool isApproved = random.Next(0, 2) == 1;

                        var proposalMessage = JsonSerializer.Serialize(
                            new CartaoCredito(
                                      Guid.NewGuid(),
                                      1999,
                                      isApproved,
                                      "4566788765453",
                                      "07/2032",
                                      customer!.Id
                                      ));

                        var proposalBody = Encoding.UTF8.GetBytes(proposalMessage);

                        channel.BasicPublish(exchange: "",
                                             routingKey: "credit_card_generated_queue",
                                             basicProperties: null,
                                             body: proposalBody);

                        Console.WriteLine("Cartão Credito {0}", proposalMessage);
                    }
                    catch (Exception ex)
                    {
                        var errorNotification = new
                        {
                            ErrorMessage = ex.Message,
                            CustomerId = customer?.Id
                        };

                        var errorNotificationMessage = JsonSerializer.Serialize(errorNotification);
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

                channel.BasicConsume(queue: "customer_registered_queue",
                                     autoAck: false,
                                     consumer: consumer);

                Console.ReadLine();
            };

            Console.WriteLine(" Press [enter] to exit.");
        }
    }
}
