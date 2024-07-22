using Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Produtor.Services.Interfaces;
using RabbitMQ.Client;
using System.Text;

namespace Produtor.Controllers
{
    [ApiController]
    [Route("api/Cliente")]
    public class ClienteController : ControllerBase
    {
        private readonly IMessagePublisher _messagePublisher;

        public ClienteController(IMessagePublisher messagePublisher)
        {
            _messagePublisher = messagePublisher;
        }

        [HttpPost]
        public async Task<IActionResult> CadastroCliente([FromBody] Cliente cliente)
        {
            if (cliente == null)
            {
                return BadRequest();
            }

            await _messagePublisher.PublishAsync("cliente_registrado_queue", cliente);

            return Ok(cliente);
        }
    }
}
