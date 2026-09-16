// Código real, vivo em src/domain/commands/handlers/CriarClienteCommand.cs — este arquivo é cópia de leitura, não edite aqui.
using MediatR;

namespace BaseDotnet.Domain.Commands.Handlers
{
    public class CriarClienteCommand : IRequest<CriarClienteCommandResult>
    {
        public string Nome { get; set; }
        public string Email { get; set; }
    }
}
