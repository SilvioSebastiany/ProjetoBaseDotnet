using Microsoft.AspNetCore.Mvc.Testing;

namespace BaseDotnet.Api.Tests.Infrastructure
{
    // Aponta para o mesmo Postgres de appsettings.json (nao usa banco descartavel) —
    // decisao documentada em docs/patterns/testes-e2e-webapplicationfactory.md.
    public class ApiWebApplicationFactory : WebApplicationFactory<global::Program>
    {
    }
}
