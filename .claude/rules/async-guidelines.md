---
paths:
  - "**/*.cs"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Async Guidelines

> Leia este arquivo antes de criar/alterar: metodo assincrono / CancellationToken.

Padrões para desenvolvimento assíncrono em .NET.

---

## 1. Regras Gerais

- Toda operação de I/O (DB, HTTP, fila, disco) deve ser assíncrona.
- Métodos assíncronos retornam `Task` ou `Task<T>`.
- Nome de método assíncrono deve possuir sufixo `Async`.
- `CancellationToken` deve ser aceito e propagado sempre que possível.

---

## 2. Repositórios

```csharp
public interface IProdutoRepository
{
    Task<Produto> ObterPorIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Produto>> ListarAsNoTrackingAsync(CancellationToken cancellationToken);
    Task AdicionarAsync(Produto produto, CancellationToken cancellationToken);
}
```

Não existe `UpdateAsync`: `Update`, `Attach` e `Remove` do EF só mexem no change tracker, em memória.
A entidade carregada rastreada muda pela ação dela e é gravada pelo `SaveChangesAsync`.

---

## 3. Handlers

```csharp
public async Task<bool> Handle(ReajustarPrecoProdutoCommand request, CancellationToken cancellationToken)
{
    var produto = await produtoRepository.ObterPorIdAsync(request.ProdutoId, cancellationToken);

    if (produto is null)
        return default;

    produto.ReajustarPreco(request.NovoPreco);

    if (produto.IsInvalid)
    {
        notificationContext.AddNotifications(produto);
        return default;
    }

    return await unitOfWork.SaveChangesAsync(cancellationToken);
}
```

---

## 4. CancellationToken

- Não ignorar o token.
- Não usar `CancellationToken.None` em código de produção.
  - **Exceção: a chamada externa cobrada mesmo quando abortada.** O fornecedor cobra a consulta
    que já saiu, e cancelar no meio perde a resposta e o registro da cobrança: o reenvio paga de
    novo, e a conta não fecha com a fatura. Ali a chamada vai até a resposta ser gravada, mesmo
    que quem chamou desista.
  - **O `None` fica só em quem chama, nunca na origem.** O external, o service e a chain são
    reaproveitados por outros fluxos, e nesses o cancelamento vale: eles respeitam e repassam o
    token como sempre. Quem passa `CancellationToken.None` é o `Handle`, e só no caminho em que a
    chamada cobrada roda; o resto do handler segue com o token. A linha leva um comentário com o
    porquê.

    ```csharp
    // Sem o token de quem chamou, de propósito: a consulta cobrada vai até ser gravada, ou o reenvio pagaria de novo.
    var elegibilidade = await VerificarElegibilidadeAsync(aquisicao, avaliacaoCD, produto, request, CancellationToken.None);
    ```

  - A gravação da resposta cobrada fica no mesmo trecho sem o token: um método privado do handler
    consulta, registra a cobrança e salva. Um teste do handler prova que, com o token cancelado
    durante a consulta, a cobrança é gravada
    (`Deve_GravarAConsultaCobrada_Quando_QuemChamaDesisteDuranteAVerificacao`).
- **NUNCA atribuir valor default** na passagem de parâmetro para `CancellationToken`.
  - **Exceção: a API pública de pacote NuGet publicado.** Ali o `CancellationToken` é o último
    parâmetro, com `= default`, como no BCL (`HttpClient`, `ReadFromJsonAsync`,
    `SaveChangesAsync`) e no `MediatR` (`Send`). Obrigar o token na assinatura do pacote quebra o
    consumidor em toda chamada feita de método que não tem token, e o obriga a mudar as
    assinaturas acima dela; quem tem o token à mão é cobrado pelo CA2016 (§ 4.2). Dentro do
    pacote vale a regra: o código interno repassa o token, sem default.
  - **Em método de pacote já publicado, a assinatura antiga continua, como sobrecarga sem o
    token.** Parâmetro novo, mesmo opcional, não quebra quem recompila, mas muda a assinatura no
    binário: o pacote compilado contra a versão anterior cai em `MissingMethodException` na
    primeira chamada, com o build verde. É o que o BCL faz: o `HttpContent.ReadAsStringAsync()`
    continuou ao lado do `ReadAsStringAsync(CancellationToken)`. A sobrecarga antiga não tem valor
    padrão, chama a nova com `cancellationToken: default` e sai do IntelliSense com
    `[EditorBrowsable(EditorBrowsableState.Never)]`. O C# escolhe ela para a chamada sem token, sem
    ambiguidade. Não leva `[Obsolete]`: com a trava de avisos, o aviso derrubaria o build de todo
    consumidor que chama sem token. Um teste fixa a assinatura antiga pelo tipo exato dos
    parâmetros, porque, se ela sair, a chamada passa a ligar na nova e o teste de comportamento
    continua verde.
- `CancellationToken` deve sempre ser passado explicitamente pelo chamador.
- ASP.NET Core fornece `HttpContext.RequestAborted` automaticamente.
- Workers/Robôs devem criar `CancellationTokenSource` e cancelar no shutdown.

Correto:

```csharp
// Correto - sem valor default
public async Task<Produto> ObterAsync(int id, CancellationToken cancellationToken)
```

Incorreto:

```csharp
// NAO usar valor default
public async Task<Produto> ObterAsync(int id, CancellationToken cancellationToken = default)
```

Exemplo de Worker:

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await ProcessarPedidos(cts.Token);
    }
}
```

---

### 4.1. UnitOfWork: todo método recebe o `CancellationToken`

Todos os métodos assíncronos do EF Core recebem o token — inclusive os de transação —, então o
UnitOfWork também recebe e repassa, sem valor default:

```csharp
public interface IUnitOfWork
{
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);

    Task BeginTransactionAsync(CancellationToken cancellationToken);

    Task CommitTransactionAsync(CancellationToken cancellationToken);

    Task RollbackTransactionAsync(CancellationToken cancellationToken);

    void ClearTracking();
}
```

Na implementação: `_context.SaveChangesAsync(cancellationToken)`,
`_context.Database.BeginTransactionAsync(cancellationToken)`,
`_context.Database.CommitTransactionAsync(cancellationToken)` e
`_context.Database.RollbackTransactionAsync(cancellationToken)`. `ClearTracking` é síncrono
(`ChangeTracker.Clear()`), e é o que isola cada item no laço do Worker.

---

### 4.2. O repasse cobrado pelo compilador

`.editorconfig` na raiz do repositório, com o CA2016 como aviso:

```ini
root = true

[*.cs]
dotnet_diagnostic.CA2016.severity = warning
```

O CA2016 acusa a chamada que não repassa o token quando o método que a faz tem um à mão,
inclusive para método com `= default`, que é onde o token some sem ninguém ver. Com a trava de
avisos (`coding-standards.md` § 9), vira erro de build. Método sem token não é acusado. No nível
padrão de análise, o CA2016 é só sugestão e não aparece no build: é a linha do `.editorconfig`
que o liga. Repositório que já tem `.editorconfig` ganha a linha nele.

---

## 5. Boas Práticas

- Evitar `Result`/`Wait` em código assíncrono (deadlocks).
- Evitar misturar métodos síncronos que fazem I/O dentro de handlers.
- Tratar exceções em pipelines apropriados (middleware, behaviors).
