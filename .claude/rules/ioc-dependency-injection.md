---
paths:
  - "src/infra.crosscutting.ioc/**"
  - "src/api/Program.cs"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# IoC and Dependency Injection

> Leia este arquivo antes de criar/alterar: registro de DI, Domain Service, lifetime, cache de estado em memoria.

Padrões para injeção de dependência e uso de `*.Infra.CrossCutting.Ioc`
como Composition Root.

---

## 1. Projeto de IoC

- Toda solução deve ter um projeto do tipo:
  - `{ProjectName}.Infra.CrossCutting.Ioc`
- Este projeto contém extension methods de `IServiceCollection` para registrar:
  - Repositórios
  - DbContexts
  - UnitOfWork
  - Domain Services
  - NotificationContext
  - HttpClients tipados
  - Autenticação / Autorização
  - Swagger
  - HealthChecks
  - Settings (`IOptions<T>`)

---

## 2. Arquivos Sugeridos

- `DependencyInjection.cs` – Repositórios, DbContexts, Domain Services.
- `MediatorInjection.cs` – MediatR, Handlers, Behaviors, Validators.
- `ApiServiceInjection.cs` – HttpClients tipados (APIs externas).
- `HealthChecksInjection.cs` – Health checks de DB e serviços externos.
- `IdentityServerInjection.cs` – JWT/OAuth2, policies de autorização.
- `SwaggerInjection.cs` – Configuração de OpenAPI.
- `SettingsInjection.cs` – Bind de configurações para classes tipadas.

---

## 3. Lifetimes

- **Scoped**
  - Repositórios
  - DbContext (EF Core)
  - UnitOfWork
  - NotificationContext
  - Serviços com estado por request

- **Singleton**
  - Configurações (`IOptions<T>`)
  - MongoDB context stateless
  - Caches em memória quando apropriado

- **Transient**
  - HTTP message handlers
  - Serviços leves e stateless

---

## 4. Exemplo de Registro

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ICalculoPrecoService, CalculoPrecoService>();
        services.AddScoped<INotificationContext, NotificationContext>();
        return services;
    }
}
```

---

## 5. Regras

- Domain não conhece implementações concretas, apenas interfaces.
- Services injeta **interfaces** do domínio; IoC resolve implementações.
- Implementações concretas devem ficar na Infra (`Infra.Data`, `Infra.ExternalServices`, etc.).

---

## 6. Services e externals

**Service** existe para o trecho reaproveitado por 2 ou mais handlers (`domain-modeling.md` § 7):

- pode fazer I/O: ler repositório, chamar external;
- **nunca chama `SaveChanges`** — quem salva é o handler; service que salva espalha o momento da
  gravação pelo sistema e ninguém mais sabe o que foi gravado junto;
- não decide regra que é de uma entidade ou de um VO;
- é nomeado pelo **contexto**, com métodos pela operação: `PedidoService.GerarPedidoCompraAvulsoAsync`,
  nunca `GerarPedidoCompraAvulsoService`. Um service por operação espalha o mesmo contexto em várias
  classes.

**External** é a integração com outro sistema: a interface fica no Domain e a implementação em
`Infra.ExternalServices`. O detalhe de transporte (`HttpStatusCode`, `System.Net`) não atravessa
para o Domain — a Infra traduz para um valor de domínio, como diz `external-services.md`. O SDK da
casa é a exceção: a interface dele entra direto no Domain, sem external de embrulho, porque ele já
é essa tradução (`external-services.md` § 11).

**Estrutura:**

```
Domain/Interfaces/Services/I{Contexto}Service.cs
Domain/Services/{Contexto}Service.cs
Domain/Interfaces/ExternalServices/I{Sistema}Service.cs
Infra.ExternalServices.{Sistema}/...
```

**Quando criar um Service:**

- ✅ Mesmo trecho em 2+ handlers
- ✅ Várias chamadas a um mesmo SDK que andam juntas
- ❌ Operação única em um handler — fica no handler
- ❌ Regra que olha só uma entidade — vai para a entidade

**Exemplo:**

```csharp
public interface IUsuarioExternoService
{
    Task CriarOuAtualizarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken);
    Task DesabilitarUsuarioAsync(string identificador, CancellationToken cancellationToken);
}
```

**Registro no DI:**

```csharp
// DependencyInjection.cs
services.AddScoped<IUsuarioExternoService, UsuarioExternoService>();
```

---

## 7. Health Checks para Servicos Externos

Para verificar disponibilidade de servicos externos (Identity Provider, Storage, APIs),
usar `AspNetCore.HealthChecks.Uris`:

```csharp
public static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
{
    var healthChecksBuilder = services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("DbContext do EF Core", tags: new[] { "db", "efcore" });

    // Health check para Identity Provider
    var authEndpoint = configuration.GetValue<string>("Autenticacao:Endpoint");
    if (!string.IsNullOrWhiteSpace(authEndpoint))
    {
        healthChecksBuilder.AddUrlGroup(
            uri: new Uri(authEndpoint.TrimEnd('/')),
            name: "Identity Provider",
            tags: new[] { "auth" });
    }

    // Health check para Storage
    var storageEndpoint = configuration.GetValue<string>("Storage:Endpoint");
    if (!string.IsNullOrWhiteSpace(storageEndpoint))
    {
        healthChecksBuilder.AddUrlGroup(
            uri: new Uri(storageEndpoint.TrimEnd('/') + "/health"),
            name: "Storage Service",
            tags: new[] { "storage" });
    }

    // Health check para API de E-mails
    var emailEndpoint = configuration.GetValue<string>("ApiEmail:Endpoint");
    if (!string.IsNullOrWhiteSpace(emailEndpoint))
    {
        healthChecksBuilder.AddUrlGroup(
            uri: new Uri(emailEndpoint.TrimEnd('/') + "/health"),
            name: "API de E-mails",
            tags: new[] { "email" });
    }

    services.AddHealthChecksUI(setupSettings: setup =>
    {
        setup.SetEvaluationTimeInSeconds(300);
        setup.SetMinimumSecondsBetweenFailureNotifications(300);
    })
    .AddInMemoryStorage();
}
```

**Pacote necessario:** `AspNetCore.HealthChecks.Uris`

---

## Caches de estado em memória

- **Interface no Domain** (`Domain/Interfaces/Services/I*Cache.cs`); **implementação na Infra** — caches de estado são detalhe técnico, não regra de negócio. Local padrão: no projeto de Infra que hospeda a integração.
- Registro como **Singleton** no composition root (`DependencyInjection.cs`).
- **Premissa explícita: API single-instance**. Escalar horizontalmente exigiria backplane (ex.: Redis) — tratar como mudança arquitetural, não como troca de registro.
- Estado mutável guardado no cache deve ser mutado SOMENTE através do cache (que serializa o acesso); hidratação a partir do banco deve usar agregação no servidor (SUM/COUNT), nunca materializar eventos em massa.
