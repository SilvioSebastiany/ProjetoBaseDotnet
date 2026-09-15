using BaseDotnet.Domain.Entities;

namespace BaseDotnet.Domain.Interfaces
{
    public interface IClienteRepository
    {
        Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken);
    }
}
