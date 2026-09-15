using BaseDotnet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseDotnet.Infra.Data.DbContext
{
    public class BaseDotnetContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public BaseDotnetContext(DbContextOptions<BaseDotnetContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseDotnetContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
