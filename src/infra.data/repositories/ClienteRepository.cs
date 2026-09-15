using BaseDotNet.Domain.Entities;
using BaseDotNet.Domain.Interfaces;
using BaseDotNet.Infra.Data.DbContext;

namespace BaseDotNet.Infra.Data.Repositories
{
    public class ClienteRepository(BaseDotNetContext context) : IClienteRepository
    {
        public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
        {
            await context.Clientes.AddAsync(cliente, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
