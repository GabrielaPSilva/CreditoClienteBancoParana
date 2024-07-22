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
                channel.QueueDeclare(queue: "limite_credito_gerado_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: new Dictionary<string, object>
                                     {
                                         { "x-dead-letter-exchange", "dead_letter_exchange" },
                                         { "x-dead-letter-routing-key", "dead_letter" },
                                         { "x-message-ttl", 60000 }
                                     });

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var creditLimit = JsonSerializer.Deserialize<PropostaCredito>(message);

                    Console.WriteLine(creditLimit?.ToString());

                    try
                    {
                        var randomCvv = new Random();
                        var cvv = randomCvv.Next(100, 1000);

                        var randomCreditCard = new Random();
                        var numberCreditCard = string.Empty;
                        for (int i = 0; i < 16; i++)
                        {
                            numberCreditCard += randomCreditCard.Next(0, 10).ToString();
                        }

                        var randomData = new Random();
                        int month = randomData.Next(1, 13); 
                        int year = randomData.Next(2024, 2040 + 1);

                        var creditCardMessage = JsonSerializer.Serialize(
                            new CartaoCredito(
                                      Guid.NewGuid(),
                                      cvv,
                                      numberCreditCard,
                                      $"{month}/{year}",
                                      new PropostaCredito(
                                          creditLimit!.Id,
                                          creditLimit.Limite,
                                          creditLimit.Aprovado,
                                          creditLimit.ClienteId
                                      )));

                        var creditCardBody = Encoding.UTF8.GetBytes(creditCardMessage);

                        channel.QueueDeclare(queue: "cartao_credito_gerado_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: new Dictionary<string, object>
                                     {
                                         { "x-dead-letter-exchange", "dead_letter_exchange" },
                                         { "x-dead-letter-routing-key", "dead_letter" },
                                         { "x-message-ttl", 60000 }
                                     });

                        channel.BasicPublish(exchange: "",
                                             routingKey: "cartao_credito_gerado_queue",
                                             basicProperties: null,
                                             body: creditCardBody);

                        Console.WriteLine("Cartão Credito {0}", creditCardMessage);
                    }
                    catch (Exception ex)
                    {
                        var errorNotification = new
                        {
                            ErrorMessage = ex.Message,
                            CustomerId = creditLimit?.ClienteId
                        };

                        channel.QueueDeclare(queue: "notificacoes_erro_queue",
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

                        var errorNotificationMessage = JsonSerializer.Serialize(errorNotification);
                        var errorNotificationBody = Encoding.UTF8.GetBytes(errorNotificationMessage);

                        channel.BasicPublish(exchange: "",
                                             routingKey: "notificacoes_erro_queue",
                                             basicProperties: null,
                                             body: errorNotificationBody);

                        Console.WriteLine("Error {0}", errorNotificationMessage);

                        // Envia mensagem para dead-letter
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                };

                channel.BasicConsume(queue: "limite_credito_gerado_queue",
                                     autoAck: false,
                                     consumer: consumer);

                await Task.CompletedTask;

                Console.ReadLine();
                Console.WriteLine(" Pressione [enter] para sair.");
            };
        }
    }
}
