using Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ConsumidorErros
{
    public class WorkerErros : BackgroundService
    {
        private readonly ILogger<WorkerErros> _logger;
        private readonly IConfiguration _configuration;

        public WorkerErros(ILogger<WorkerErros> logger, IConfiguration configuration)
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
                channel.QueueDeclare(queue: "error_notification_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var errorNotification = JsonSerializer.Deserialize<NotificacaoErros>(message);
                    NotifyClientAboutError(errorNotification!);
                };

                channel.BasicConsume(queue: "error_notification_queue", autoAck: true, consumer: consumer);

                await Task.CompletedTask;

                Console.ReadLine();
            }

            static void NotifyClientAboutError(dynamic errorEvent)
            {
                Console.WriteLine($"Erro notificado ao cliente: {errorEvent.Message}");
            }
        }
    }
}
