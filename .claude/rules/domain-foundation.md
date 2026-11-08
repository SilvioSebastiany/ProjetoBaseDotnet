---
paths:
  - "**/*.cs"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Fundação do domínio

> Leia este arquivo antes de criar/alterar: entidade, VO, `NotificationContext`, filtro de notificação ou de exceção da API, paginação — e ao começar um projeto novo.

As outras regras usam um punhado de tipos que, num projeto novo, não existem: `Validatable`,
`TrackableEntity`, `ValueObject`, `AddNotifications(entidade)`, a extensão `.IsValid()` do
FluentValidation, `NotificationKeys` e os dois filtros da API. Eles moram **no próprio projeto**,
uma vez por solução, e são pequenos. Este arquivo é a forma deles.

Antes de criar, confira se o projeto já tem algum com outro nome (`grep -rn "abstract class" src`).
Tendo, o que existe se completa até cumprir o contrato daqui, em vez de nascer um segundo.

| Tipo | Onde |
|---|---|
| `Notification`, `INotificationContext`, `NotificationContext`, `NotificationKeys` | `Domain/Notifications/` |
| `Validatable`, `TrackableEntity` | `Domain/Entities/Base/` |
| `ValueObject` | `Domain/ValueObjects/` |
| `ValidatableRuleExtensions` (o `.IsValid()`) | `Domain/Validators/` |
| `PagedRequest`, `PagedResponse<T>`, `ToPagedAsync` | `Domain/Pagination/` e `Infra.Data/Extensions/` |
| `CustomNotificationFilter`, `CustomExceptionFilter`, `CustomControllerBase` | `Api/Filters/` e `Api/Controllers/Base/` |

---

## 1. Notificação

```csharp
public interface INotificationContext
{
    bool HasNotifications { get; }
    IReadOnlyCollection<Notification> Notifications { get; }
    void AddNotification(string chave, string mensagem);
    void AddNotifications(IEnumerable<Notification> notificacoes);
    void AddNotifications(Validatable validatable);
}
```

`AddNotifications(validatable)` copia as notificações da entidade ou do VO. É ele que faz o 422
sair com mensagem: sem a chamada, a entidade sabe que está inválida e a resposta não diz por quê.

**`NotificationKeys`** são as únicas chaves de transporte, e nunca se escreve o literal:

```csharp
public static class NotificationKeys
{
    public const string FORBIDDEN = "Forbidden";
    public const string CONFLICT = "Conflict";
    public const string SERVICE_UNAVAILABLE = "ServiceUnavailable";
}
```

---

## 2. `Validatable` e `TrackableEntity`

```csharp
public abstract class Validatable
{
    private readonly List<Notification> _notificacoes = [];

    public IReadOnlyCollection<Notification> Notifications => _notificacoes.AsReadOnly();
    public bool IsValid => Validate() && _notificacoes.Count is 0;
    public bool IsInvalid => !IsValid;

    public virtual bool Validate() => true;

    public void AddNotification(string chave, string mensagem) =>
        _notificacoes.Add(new Notification(chave, mensagem));

    protected bool UseValidator<T>(T instancia, IValidator<T> validator)
    {
        var resultado = validator.Validate(instancia);
        foreach (var erro in resultado.Errors)
            if (!_notificacoes.Any(n => n.Chave == erro.PropertyName && n.Mensagem == erro.ErrorMessage))
                _notificacoes.Add(new Notification(erro.PropertyName, erro.ErrorMessage));
        return resultado.IsValid;
    }
}
```

- `IsValid` roda `Validate()` a cada leitura (`domain-modeling.md` § 4): por isso o
  `UseValidator` não duplica a notificação que já está lá.
- A notificação que a ação adicionou **não sai**: a entidade inválida continua inválida no escopo.
- O `Validatable` referencia o FluentValidation, que o Domain já usa para os validators.

```csharp
public abstract class TrackableEntity : Validatable
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string UsuarioInclusao { get; private set; }
    public DateTime DataInclusao { get; private set; }
    public string UsuarioAlteracao { get; private set; }
    public DateTime? DataAlteracao { get; private set; }
}
```

- O Id nasce na entidade, e não se atribui (`Guid.CreateVersion7`, do .NET 9 em diante: ordenado
  no tempo, o que poupa o índice).
- A auditoria se preenche num `SaveChangesInterceptor` da Infra, que lê o usuário de um
  `IUsuarioLogado` e escreve pelos campos (`Entry(...).Property(...).CurrentValue`): a entidade não
  expõe setter público, e o handler nunca preenche.

---

## 3. `ValueObject`

```csharp
public abstract class ValueObject : Validatable
{
    protected abstract IEnumerable<object> ComponentesDeIgualdade();

    public override bool Equals(object obj) =>
        obj is ValueObject outro && outro.GetType() == GetType()
        && ComponentesDeIgualdade().SequenceEqual(outro.ComponentesDeIgualdade());

    public override int GetHashCode() =>
        ComponentesDeIgualdade().Aggregate(0, (hash, c) => HashCode.Combine(hash, c));
}
```

A comparação por componentes é a do guia da Microsoft
([Implement value objects](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects)).
Como cada VO é feito: `valueobjects.md`.

---

## 4. O `.IsValid()` no validator da raiz

```csharp
public static class ValidatableRuleExtensions
{
    public static IRuleBuilderOptions<T, TValidatable> IsValid<T, TValidatable>(
        this IRuleBuilder<T, TValidatable> regra)
        where TValidatable : Validatable =>
        regra.Custom((valor, contexto) =>
        {
            if (valor is null || valor.IsValid)
                return;
            foreach (var n in valor.Notifications)
                contexto.AddFailure(n.Chave, n.Mensagem);
        }) as IRuleBuilderOptions<T, TValidatable>;
}
```

É o que leva a notificação da filha e do VO para a raiz (`validation-and-cqrs.md` § 14). Nulo
passa: o obrigatório é um `NotNull()` à parte, com mensagem própria.

---

## 5. Paginação

```csharp
public class PagedRequest : IPaginationInfo
{
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}

public class PagedResponse<T>
{
    public IReadOnlyCollection<T> Itens { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
    public int Total { get; set; }
}
```

O `ToPagedAsync(paginationInfo, cancellationToken)` é uma extensão de `IQueryable<T>` na
Infra.Data: faz o `CountAsync` e o `Skip/Take` na mesma consulta ordenada, e limita o
`TamanhoPagina` a um teto (100). Resposta paginada também é objeto, nunca array
(`api-standards.md` § 6.1).

---

## 6. Os filtros da API

- **`CustomNotificationFilter`** (`IAsyncResultFilter`, registrado global em `AddControllers`):
  com notificação no contexto, troca o resultado por um `ProblemDetails`. O status sai da chave,
  nesta ordem: `FORBIDDEN` → 403, `CONFLICT` → 409, `SERVICE_UNAVAILABLE` → 503, qualquer outra →
  422, como `ValidationProblemDetails` com as mensagens agrupadas pela chave. É por ele que o
  Controller não injeta `INotificationContext`.
- **`CustomExceptionFilter`** (`IExceptionFilter`, global): loga a exceção com a mensagem dela e
  responde 500 com um `ProblemDetails` sem detalhe interno. É por ele que não há `try/catch` em
  Controller nem em Handler.
- **`CustomControllerBase`**: base dos controllers, com o `Created(response)` que monta a rota do
  recurso criado (`coding-standards.md`, a forma padrão do `Created`).

Num projeto que já trata o 422 em cada controller, a troca para o filtro é refatoração da rota que
a tarefa toca, com teste, e o resto vai em "Fora do padrão, não tocado" (`workflow.md` § 5).
