# Convenções

Regras práticas de nomenclatura e código, válidas em todo o projeto. Onde uma convenção tem um padrão dedicado (ex: CQRS, EF Core), o link para `docs/patterns/` tem o detalhe — aqui fica o resumo de referência rápida.

## Nomenclatura

- Domínio em **português sem acento**: `Tarefa`, `Projeto`, `DataInclusao`, `Revisao` (nunca `Revisão`).
- Interfaces com prefixo `I`: `IProdutoRepository`.
- Enums com prefixo `E`: `EStatusWorkflow`.
- DTOs de entrada de Command: sufixo `Command` (`CriarProdutoCommand`).
- DTOs de saída: sufixo `Response` (`ProdutoResponse`) ou `Result` quando é o retorno direto de um Command (`CriarProdutoCommandResult`).
- Métodos assíncronos: sempre sufixo `Async`, sempre propagando `CancellationToken`.

## Classes

- Sempre `class`, nunca `record` — em nenhuma camada, inclusive DTOs.
- Nullable reference types **desabilitado** em todos os projetos (`<Nullable>disable</Nullable>`).
- Primary constructors em classes de infraestrutura — Controllers, Repositories, Handlers. A Entity em si não usa primary constructor.

## API

- Sem prefixo `/api`.
- Recursos no plural, kebab-case: `/projetos`, `/tarefas/{id}/historico`.
- Ação como verbo no final da rota, quando não é um verbo HTTP padrão: `POST /tarefas/{id}/transicionar-status`.
- Status codes: `201` em criação, `422` para regra de negócio violada (via `NotificationContext`), `404` para recurso inexistente.

## Banco de dados

- PostgreSQL, convenção de nomes **snake_case minúsculo** (idiomático Postgres — não UPPERCASE).
- Mapeamento sempre via Fluent API (`IEntityTypeConfiguration<T>`), nunca Data Annotations na entidade.
- `Configurations`/`conventions` ficam em `infra.data/conventions/`, separadas da entidade — a entidade em `domain` não sabe nada sobre EF Core.

## Commands vs Queries

Ver `docs/patterns/cqrs-assimetrico.md` para a decisão completa. Resumo: escrita ou regra de negócio → Command via MediatR; leitura pura → QueryController direto ao repositório, sem Handler.

## Validação

FluentValidation nos Commands, acumulando erros via `NotificationContext` (nunca lançando exceção para erro de negócio esperado). Ver `docs/patterns/fluentvalidation-notificationcontext.md`.

## Testes

xUnit + Moq + AutoFixture + Shouldly. Ver `docs/patterns/testes-xunit-moq-autofixture-shouldly.md`.

## Git

Ver `docs/git.md`.
