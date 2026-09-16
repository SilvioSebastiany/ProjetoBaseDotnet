// Código real, vivo em src/api/controllers/ClientesController.cs — este arquivo é cópia de leitura, não edite aqui.
using BaseDotnet.Api.Mappers;
using BaseDotnet.Api.Responses;
using BaseDotnet.Domain.Commands.Handlers;
using BaseDotnet.Domain.Interfaces;
using BaseDotnet.Domain.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BaseDotnet.Api.Controllers
{
    [ApiController]
    [Route("clientes")]
    public class ClientesController(
        IMediator mediator,
        INotificationContext notificationContext,
        IClienteRepository clienteRepository) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(CriarClienteCommandResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CriarAsync(
            CriarClienteCommand command,
            CancellationToken cancellationToken)
        {
            var resultado = await mediator.Send(command, cancellationToken);

            if (notificationContext.HasNotifications)
                return UnprocessableEntity(notificationContext.Notifications);

            return Created($"/clientes/{resultado.Id}", resultado);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ClienteResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListarAsync(CancellationToken cancellationToken)
        {
            var clientes = await clienteRepository.ListarAsync(cancellationToken);

            var resposta = clientes.Select(ClienteMapper.Map).ToList();

            return Ok(resposta);
        }
    }
}
