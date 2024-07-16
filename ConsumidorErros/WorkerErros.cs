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
                channel.QueueDeclare(queue: "error_notifications_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var errorConsumer = new EventingBasicConsumer(channel);
                errorConsumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var errorNotification = JsonSerializer.Deserialize<NotificacaoErros>(message);

                    Console.WriteLine(" [x] Received error notification {0}", message);

                    // Lógica para processar a notificação de erro
                    // Por exemplo, notificar os administradores ou atualizar o status do cliente
                };

                channel.BasicConsume(queue: "error_notifications_queue", autoAck: true, consumer: errorConsumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            };
        }
    }
}
