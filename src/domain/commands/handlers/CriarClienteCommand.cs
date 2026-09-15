using MediatR;

namespace BaseDotnet.Domain.Commands.Handlers
{
    public class CriarClienteCommand : IRequest<CriarClienteCommandResult>
    {
        public string Nome { get; set; }
        public string Email { get; set; }
    }
}
