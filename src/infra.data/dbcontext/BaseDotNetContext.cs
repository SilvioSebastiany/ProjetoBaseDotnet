using BaseDotNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseDotNet.Infra.Data.DbContext
{
    public class BaseDotNetContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public BaseDotNetContext(DbContextOptions<BaseDotNetContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseDotNetContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
