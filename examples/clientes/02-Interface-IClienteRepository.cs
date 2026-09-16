// Código real, vivo em src/domain/interfaces/IClienteRepository.cs — este arquivo é cópia de leitura, não edite aqui.
using BaseDotnet.Domain.Entities;

namespace BaseDotnet.Domain.Interfaces
{
    public interface IClienteRepository
    {
        Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken);
        Task<List<Cliente>> ListarAsync(CancellationToken cancellationToken);
    }
}
