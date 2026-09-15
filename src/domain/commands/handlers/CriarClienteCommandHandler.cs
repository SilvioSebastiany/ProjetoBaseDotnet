using BaseDotNet.Domain.Entities;
using BaseDotNet.Domain.Interfaces;
using BaseDotNet.Domain.Notifications;
using MediatR;

namespace BaseDotNet.Domain.Commands.Handlers
{
    public class CriarClienteCommandHandler(
        IClienteRepository clienteRepository,
        INotificationContext notificationContext)
        : IRequestHandler<CriarClienteCommand, CriarClienteCommandResult>
    {
        public async Task<CriarClienteCommandResult> Handle(
            CriarClienteCommand command,
            CancellationToken cancellationToken)
        {
            if (notificationContext.HasNotifications)
                return default;

            var cliente = new Cliente(command.Nome, command.Email);

            await clienteRepository.AdicionarAsync(cliente, cancellationToken);

            return new CriarClienteCommandResult
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Email = cliente.Email
            };
        }
    }
}
