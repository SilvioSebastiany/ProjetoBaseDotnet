# Projeto Base .NET

Referência viva de arquitetura para novos projetos .NET. Não é um template genérico de Clean Architecture — é uma estrutura e um conjunto de decisões arquiteturais próprias, pensadas para servir de ponto de partida para os próximos projetos.

A ideia é simples: em vez de recomeçar cada projeto do zero, este repositório mantém a arquitetura, as convenções e os padrões documentados e prontos para clonar. Quando uma feature real for implementada e estabilizada neste projeto, ela é portada para cá como exemplo canônico em `examples/`.

---

## Princípios deste projeto

- **Simplicidade antes de abstração** — resolve o problema de hoje; não cria camada, interface ou abstração para um cenário hipotético futuro.
- **Cada responsabilidade tem um lugar definido** — Command decide, QueryController busca, Handler executa, Repository persiste. Nada mora em dois lugares.
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
docs/
  architecture.md
  conventions.md
  patterns/
    clean-architecture-enxuta.md
    cqrs-assimetrico.md
    mediatr.md
    fluentvalidation-notificationcontext.md
    repository.md
    ef-core-configurations.md
    dependency-injection.md
    testes-xunit-moq-autofixture-shouldly.md
  git.md
src/
  api/                       # Controllers (Commands) e QueryControllers (Queries), mappers, responses
  domain/                    # Entidades, commands/handlers, interfaces, notifications — sem dependências externas
  infra.data/                # DbContext, conventions (Fluent API), migrations, repositories
  infra.crosscutting/        # Enums, constantes, extensions, settings
  infra.crosscutting.ioc/    # Composition root (dependencyinjection.cs)
tests/
examples/                    # Vazio até a primeira feature real ser aprovada e portada
.editorconfig
Directory.Build.props
Directory.Packages.props
```

Veja `docs/architecture.md` para a explicação de cada camada e o fluxo de dependências.

---

## Como usar este projeto como base para um novo projeto

1. Clone ou copie este repositório com o nome do novo projeto.
2. Leia `docs/architecture.md` e `docs/conventions.md` antes de escrever qualquer código — eles explicam o porquê das decisões, não só o quê.
3. Para cada feature nova, siga o fluxo descrito em `docs/patterns/cqrs-assimetrico.md`: decida primeiro se é Command (escrita, com regra de negócio) ou Query (leitura pura) — isso define todo o resto do caminho.
4. Use os arquivos em `docs/patterns/` como referência ao implementar cada peça (Handler, Repository, Configuration do EF Core, testes etc.). Cada um tem um "Exemplo no código" que será preenchido assim que a primeira feature real for portada para `examples/`.
5. Não mexa nos nomes de camada nem invente uma camada `Application` — a divisão em 3 camadas (api / domain / infra) é uma decisão deliberada, documentada em `docs/architecture.md`.
6. Ao final da primeira feature, revise `docs/conventions.md` — se algo tiver ficado ambíguo, é hora de esclarecer o documento, não de decidir caso a caso a cada feature nova.
