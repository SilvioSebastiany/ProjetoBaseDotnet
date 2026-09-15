using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaseDotnet.Infra.Data.DbContext
{
    public class BaseDotnetContextFactory : IDesignTimeDbContextFactory<BaseDotnetContext>
    {
        public BaseDotnetContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BaseDotnetContext>();

            optionsBuilder
                .UseNpgsql("Host=localhost;Port=5432;Database=basedotnet;Username=basedotnet;Password=basedotnet")
                .UseSnakeCaseNamingConvention();

            return new BaseDotnetContext(optionsBuilder.Options);
        }
    }
}
