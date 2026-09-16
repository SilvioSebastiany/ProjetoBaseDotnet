// Código real, vivo em src/domain/commands/handlers/CriarClienteCommandHandler.cs — este arquivo é cópia de leitura, não edite aqui.
using BaseDotnet.Domain.Entities;
using BaseDotnet.Domain.Interfaces;
using BaseDotnet.Domain.Notifications;
using MediatR;

namespace BaseDotnet.Domain.Commands.Handlers
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
