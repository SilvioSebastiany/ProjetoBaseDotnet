# Projeto Base .NET

Referência viva de arquitetura para novos projetos .NET. Não é um template genérico de Clean Architecture — é uma estrutura e um conjunto de decisões arquiteturais próprias, pensadas para servir de ponto de partida para os próximos projetos.

A ideia é simples: em vez de recomeçar cada projeto do zero, este repositório mantém a arquitetura, as convenções e os padrões documentados e prontos para clonar. Quando uma feature real for implementada e estabilizada neste projeto, ela é portada para cá como exemplo canônico em `examples/`.

---

## Princípios deste projeto

- **Simplicidade antes de abstração** — resolve o problema de hoje; não cria camada, interface ou abstração para um cenário hipotético futuro.
- **Cada responsabilidade tem um lugar definido** — Command decide, Controller busca (na leitura) ou dispara o Handler (na escrita), Repository persiste. Nada mora em dois lugares.
- **Código testável** — se é difícil de testar, o desenho está errado, não o teste.
- **Não criar abstrações sem necessidade** — uma interface só existe quando há (ou vai haver de fato) mais de uma implementação, ou quando é necessária para teste/DI.
- **Toda nova feature segue o fluxo existente** — Command ou Query, na estrutura de pastas já definida. Desvio do padrão é exceção documentada, não regra nova por feature.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 |
| API | ASP.NET Core Web API |
| CQRS | MediatR — somente para escrita (Commands) |
| Validação | FluentValidation + NotificationContext |
| ORM | Entity Framework Core 10 (Npgsql), Fluent API |
| Banco | PostgreSQL, convenção snake_case, rodando em Docker |
| Testes | xUnit, Moq, AutoFixture, Shouldly |

---

## Estrutura de pastas

```
README.md
.claude/
  CLAUDE.md                  # Padrão de código .NET (genérico)
  rules/                     # Regras detalhadas por tema
src/
  api/                       # Controllers (Commands e Queries juntos por recurso), mappers, responses
  domain/                    # Entidades, commands/handlers, interfaces, notifications — sem dependências externas
  infra.data/                # DbContext, conventions (Fluent API), migrations, repositories
  infra.crosscutting/        # Enums, constantes, extensions, settings
  infra.crosscutting.ioc/    # Composition root (dependencyinjection.cs)
tests/
  BaseDotnet.Domain.Tests/    # Unitario: xUnit + Moq + AutoFixture + Shouldly
  BaseDotnet.Api.Tests/        # E2E: WebApplicationFactory + HttpClient real
examples/                    # Vazio até a primeira feature real ser aprovada e portada
.editorconfig
Directory.Build.props
Directory.Packages.props
```

O padrão de código — camadas, fluxo de dependências, CQRS, banco, testes — está em `.claude/CLAUDE.md` e, por tema, em `.claude/rules/`.

---

## Como usar este projeto como base para um novo projeto

1. Clone ou copie este repositório com o nome do novo projeto.
2. Leia `.claude/CLAUDE.md` antes de escrever qualquer código, e a regra do tema em `.claude/rules/` antes de criar ou alterar algo dele.
3. Use `examples/` como referência de código real ao implementar cada peça (Handler, Repository, Configuration do EF Core, testes etc.).
4. Não mexa nos nomes de camada nem invente uma camada `Application` — a divisão em 3 camadas (api / domain / infra) é uma decisão deliberada.
5. Ao final da primeira feature, se algo tiver ficado ambíguo, esclareça no `CLAUDE.md` da raiz do novo projeto — não decida caso a caso a cada feature nova.
