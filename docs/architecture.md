# Arquitetura

Este projeto não segue um modelo genérico de Clean Architecture com camada `Application` separada. Segue a arquitetura já validada em produção no FluxoDev — enxuta, com 3 camadas conceituais divididas em 5 projetos `.csproj`.

## As camadas

```
src/
├── api/                       # Entrada HTTP
├── domain/                    # Regras de negócio
├── infra.data/                # Persistência
├── infra.crosscutting/        # Utilidades transversais
└── infra.crosscutting.ioc/    # Composition root
```

Conceitualmente são 3 camadas — **api**, **domain**, **infra** — mas `infra` é dividida em 3 projetos por responsabilidade (`infra.data`, `infra.crosscutting`, `infra.crosscutting.ioc`) para manter cada dependência isolada e o composition root em um lugar só.

### `api`
Camada de entrada. Contém:
- `controllers/` — Controllers de escrita, que disparam Commands via `IMediator`.
- `queries/` — QueryControllers, que acessam o repositório diretamente (sem Mediator, sem Handler).
- `mappers/` — conversão Entity ↔ DTO (Response).
- `responses/` — os DTOs de saída.

### `domain`
O coração da regra de negócio. **Não depende de nenhum outro projeto** — nem de `infra.data`, nem de pacotes de ORM. Contém:
- `entities/` — as entidades do domínio (nomenclatura em português sem acento, sempre `class`, nunca `record`).
- `commands/handlers/` — um Command, Handler, Validator e Result por ação de escrita.
- `interfaces/` — os contratos de repositório (`IProdutoRepository`, etc.) que `infra.data` implementa.
- `notifications/` — `NotificationContext`, usado pelos Handlers para acumular erros de negócio.

### `infra.data`
Implementa a persistência. Depende de `domain` (implementa as interfaces de repositório de lá), nunca o contrário.
- `dbcontext/` — o `DbContext`.
- `conventions/` — as `IEntityTypeConfiguration<T>` (Fluent API), separadas da entidade.
- `migrations/` — migrations do EF Core.
- `repositories/` — implementação dos repositórios.

### `infra.crosscutting`
Utilidades sem regra de negócio e sem dependência de domínio: constantes, enums, extensions, settings, providers.

### `infra.crosscutting.ioc`
O composition root. Um único arquivo, `dependencyinjection.cs`, com métodos de extensão que registram DbContext, repositórios, MediatR e validators. Referencia todos os outros projetos — é o único lugar que pode.

## Fluxo de dependências

```
api  →  domain  ←  infra.data
          ↑
infra.crosscutting.ioc (referencia tudo, é o composition root)
```

- `domain` não depende de nada.
- `infra.data` implementa as interfaces que `domain` declara.
- `api` conhece `domain` — via `IMediator` para escrita, e via injeção direta de repositório para leitura (`QueryController`).
- `infra.crosscutting.ioc` é o único projeto que conhece todos os outros; é onde a composição acontece.

## Por que não tem camada `Application`

Nos modelos genéricos de Clean Architecture, a camada `Application` concentra os casos de uso. Aqui, esse papel é dividido:
- Escrita (Command) → o caso de uso é o próprio `CommandHandler`, dentro de `domain/commands/handlers`.
- Leitura (Query) → não há caso de uso separado; o `QueryController` acessa o repositório direto.

Isso elimina uma camada inteira de classes de "orquestração" que, na prática, só repassavam a chamada — ver `docs/patterns/cqrs-assimetrico.md` para o raciocínio completo.

## Nullable reference types

Desabilitado (`<Nullable>disable</Nullable>`) em todos os projetos. É a convenção adotada — não ativar por projeto individualmente.

## Regra de ouro

Se uma decisão de arquitetura não está neste documento ou em `docs/conventions.md`, ela não deve ser tomada "no improviso" dentro de uma feature — deve ser trazida para discussão e, uma vez decidida, documentada aqui.
