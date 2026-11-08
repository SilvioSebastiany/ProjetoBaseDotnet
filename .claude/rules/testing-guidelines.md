---
paths:
  - "tests/**"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Testing Guidelines

> Leia este arquivo antes de criar/alterar: teste (unitario ou integracao).

Padrões recomendados para testes automatizados.

---

## 1. Frameworks Sugeridos

- **xUnit** – framework de testes.
- **Moq** – mocking.
- **AutoFixture** – geração automática de dados.

(Podem ser ajustados conforme o projeto, desde que a estratégia seja consistente.)

### 1.1. Assertions: os `Assert` do próprio xUnit (OBRIGATÓRIO)

Projeto em .NET 10 ou acima usa os `Assert` do xUnit (`xunit.v3`), sem biblioteca de assertion à
parte.

```csharp
Assert.Equal(3, itens.Count);
var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.AddAuthorization(cancellationToken));
Assert.Contains("sem access_token", excecao.Message);
Assert.Equivalent(esperado, resposta);
```

- **Por quê:** vem no `xunit.v3`, com licença Apache-2.0, e sobe junto com o framework. Não é
  uma dependência a mais para atualizar, auditar ou mudar de licença, como o FluentAssertions
  mudou na 8. Cobre o que se usa no dia a dia: `Equal`, com a diferença de coleção e de texto;
  `Equivalent`, objeto campo a campo; `Throws` e `ThrowsAsync`, que devolvem a exceção;
  `Multiple`; `Single`, que devolve o item; `IsType<T>`, que já devolve convertido.
- **A ordem dos argumentos:** em `Assert.Equal(esperado, obtido)`, o esperado vem primeiro.
  Invertida, a mensagem de falha diz o contrário do que aconteceu.
- **Projeto que chega ao .NET 10 com Shouldly ou FluentAssertions** troca para os `Assert` no PR
  da migração, que já mexe em todo projeto de teste (o `xunit` vira `xunit.v3`).

### 1.2. FluentAssertions: só onde já existe, e abaixo da 8

- **Não entra em projeto novo,** em nenhuma versão do .NET.
- **Projeto que já usa fica numa versão anterior à 8.** A partir da 8, a licença do
  FluentAssertions (Xceed) é paga para uso comercial; subir para ela é usar software sem licença.
- **Migrar é o recomendado:** a linha 7 é a última livre, e vai ficar defasada. O destino é o da
  § 1.1: os `Assert` do xUnit no .NET 10, o Shouldly abaixo.

---

## 2. O Que Deve Ser Testado

- **Entities: é aqui que a regra é provada**, sem mock — cada pré-condição da ação, com o caso que
  passa e o que notifica (`domain-modeling.md` § 11).
- Handlers de Commands: a orquestração — com a entidade inválida, `AddNotifications` é chamado e
  `SaveChangesAsync` não é.
- Validators de Commands e de entidade.
- Value Objects.
- Services.
- Strategies: cada uma isolada, mais o teste de cobertura do Provider.
- Externals: um teste por desfecho, com `HttpMessageHandler` falso (`external-services.md` § 10).

---

## 3. Organização de Projetos de Teste

- `*.Domain.Tests/`
- `*.Api.Tests/`
- Outros conforme necessidade.

Nome de testes sugerido:

```text
Deve_{ResultadoEsperado}_Quando_{Condicao}
```

Exemplo:

```csharp
public class AdicionarProdutoCommandHandlerTests
{
    [Fact]
    public async Task Deve_AdicionarProduto_Quando_DadosValidos()
    {
        // Arrange
        // ...

        // Act
        // ...

        // Assert
        // ...
    }
}
```

---

## 4. Abordagem Pragmática

- Nem todos os projetos terão a mesma cobertura.
- Projetos críticos devem priorizar testes para as regras de negócio principais.
- Em serviços menores, validação manual via Swagger/Postman pode ser suficiente,
  desde que seja um acordo consciente do time.

---

## Regras de código em projetos de teste

**Testes seguem as MESMAS regras de produção**:

- `<Nullable>disable</Nullable>` nos .csproj de teste; sem anotações `?` em tipos de referência; sem `null!`.
- **Sem `#region`/`#endregion`** — a organização vem dos nomes `Deve_{Resultado}_Quando_{Condicao}` e dos comentários AAA.
- **Marcadores AAA em todo teste:** `// Arrange`, `// Act` e `// Assert`; quando o assert é a própria chamada (`Assert.Throws`), `// Act e Assert`. Não contam como comentário para a regra de comentários (`coding-standards.md`, "Marcadores AAA"), que continua valendo para o resto do teste.
- **Reflection nunca espalhada pelos testes**: para popular coleções privadas de agregados (carregadas pelo EF em produção), usar o builder central em `tests/.../Base/` (ex.: `pedido.ComItem(...)`). Novos agregados ganham métodos novos no builder — nunca `GetField/BindingFlags` no corpo do teste.
- Exceção aceita: `<ImplicitUsings>enable</ImplicitUsings>` e GlobalUsings nos projetos de teste (conveniência do xUnit e da biblioteca de assertion).
- Testes não podem depender do relógio/dia real: cenários com período em andamento devem capar expectativas em `DateTime.UtcNow` ou derivar horários do instante atual.
