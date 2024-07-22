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
                channel.QueueDeclare(queue: "limite_credito_gerado_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueDeclare(queue: "cartao_credito_gerado_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var creditLimit = JsonSerializer.Deserialize<PropostaCredito>(message);

                    Console.WriteLine(creditLimit?.ToString());

                    try
                    {
                        if (creditLimit!.Aprovado)
                        {
                            var numCartao = GenerateRandomCardNumber();
                            var cvv = new Random().Next(100, 999).ToString();
                            var mesExp = new Random().Next(1, 13);
                            var anoExp = new Random().Next(2024, 2030);
                            
                            var creditCardMessage = JsonSerializer.Serialize(
                               new CartaoCredito(
                                         Guid.NewGuid(),
                                         cvv,
                                         numCartao,
                                         mesExp,
                                         anoExp,
                                         new PropostaCredito(
                                             creditLimit!.Id,
                                             creditLimit.Limite,
                                             creditLimit.Aprovado,
                                             creditLimit.ClienteId
                                         )));

                            var creditCardInfoJson = JsonSerializer.Serialize(creditCardMessage);
                            var responseBytes = Encoding.UTF8.GetBytes(creditCardInfoJson);

                            var properties = channel.CreateBasicProperties();
                            properties.Persistent = true; 

                            channel.BasicPublish(exchange: "", routingKey: "cartao_credito_gerado_queue", basicProperties: properties, body: responseBytes);

                            Console.WriteLine("Cartão Credito {0}", creditCardMessage);
                        }
                        else
                        {
                            var creditCardMessage = JsonSerializer.Serialize(
                                        new PropostaCredito(
                                            creditLimit!.Id,
                                            creditLimit.Limite,
                                            creditLimit.Aprovado,
                                            creditLimit.ClienteId
                                        ));

                            var creditCardInfoJson = JsonSerializer.Serialize(creditCardMessage);
                            var responseBytes = Encoding.UTF8.GetBytes(creditCardInfoJson);

                            var properties = channel.CreateBasicProperties();
                            properties.Persistent = true;

                            channel.BasicPublish(exchange: "", routingKey: "cartao_credito_gerado_queue", basicProperties: properties, body: responseBytes);

                            Console.WriteLine("Não foi possível gerar um cartão de crédito! Limite não foi aprovado.");
                        }
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

                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    
                };

                channel.BasicConsume(queue: "limite_credito_gerado_queue", autoAck: true, consumer: consumer);

                await Task.CompletedTask;

                Console.ReadLine();
            }

            static string GenerateRandomCardNumber()
            {
                var random = new Random();
                var cardNumber = new StringBuilder();
                for (int i = 0; i < 16; i++)
                {
                    cardNumber.Append(random.Next(0, 10));
                }
                return cardNumber.ToString();
            };
        }
    }
}
