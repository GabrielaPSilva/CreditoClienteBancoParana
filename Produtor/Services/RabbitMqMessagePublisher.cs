using Newtonsoft.Json;
using Produtor.Services.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

namespace Produtor.Services
{
    public class RabbitMqMessagePublisher : IMessagePublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqMessagePublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task PublishAsync(string queue, object message)
        {
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
                    channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                    channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: body);

                    await Task.CompletedTask;

                    channel.Close();
                    channel.Dispose();
                };

                connection.Close();
                connection.Dispose();
            };
        }
    }
}
