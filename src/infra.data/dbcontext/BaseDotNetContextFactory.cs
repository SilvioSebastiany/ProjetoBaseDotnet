using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaseDotNet.Infra.Data.DbContext
{
    public class BaseDotNetContextFactory : IDesignTimeDbContextFactory<BaseDotNetContext>
    {
        public BaseDotNetContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BaseDotNetContext>();

            optionsBuilder
                .UseNpgsql("Host=localhost;Port=5432;Database=basedotnet;Username=basedotnet;Password=basedotnet")
                .UseSnakeCaseNamingConvention();

            return new BaseDotNetContext(optionsBuilder.Options);
        }
    }
}
