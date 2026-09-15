using MediatR;

namespace BaseDotNet.Domain.Commands.Handlers
{
    public class CriarClienteCommand : IRequest<CriarClienteCommandResult>
    {
        public string Nome { get; set; }
        public string Email { get; set; }
    }
}
