using BaseDotnet.Domain.Entities;
using BaseDotnet.Domain.Interfaces;
using BaseDotnet.Infra.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace BaseDotnet.Infra.Data.Repositories
{
    public class ClienteRepository(BaseDotnetContext context) : IClienteRepository
    {
        public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
        {
            await context.Clientes.AddAsync(cliente, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Cliente>> ListarAsync(CancellationToken cancellationToken)
        {
            return await context.Clientes
                .AsNoTracking()
                .OrderBy(x => x.DataInclusao)
                .ToListAsync(cancellationToken);
        }
    }
}
