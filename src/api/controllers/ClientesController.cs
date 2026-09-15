using BaseDotNet.Domain.Commands.Handlers;
using BaseDotNet.Domain.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BaseDotNet.Api.Controllers
{
    [ApiController]
    [Route("clientes")]
    public class ClientesController(IMediator mediator, INotificationContext notificationContext) : ControllerBase
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
    }
}
