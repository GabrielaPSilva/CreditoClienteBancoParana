using Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ConsumidorCliente
{
    public class WorkerCliente : BackgroundService
    {
        private readonly ILogger<WorkerCliente> _logger;
        private readonly IConfiguration _configuration;

        public WorkerCliente(ILogger<WorkerCliente> logger, IConfiguration configuration)
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
                channel.QueueDeclare(queue: "cartao_credito_gerado_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var creditCard = JsonSerializer.Deserialize<CartaoCredito>(message);

                    NotifyCustomer(creditCard!);
                };

                channel.BasicConsume(queue: "cartao_credito_gerado_queue", autoAck: true, consumer: consumer);

                await Task.CompletedTask;
                Console.ReadLine();
            }

            static void NotifyCustomer(CartaoCredito creditCardInfo)
            {
                Console.WriteLine(creditCardInfo);
            }
        }
    }
}
