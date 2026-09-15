using BaseDotNet.Domain.Entities;

namespace BaseDotNet.Domain.Interfaces
{
    public interface IClienteRepository
    {
        Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken);
    }
}
