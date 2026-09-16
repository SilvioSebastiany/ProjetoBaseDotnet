# Repository

## O que é

O padrão Repository isola o acesso a dados atrás de uma interface (`IProdutoRepository`, por exemplo) declarada em `domain/interfaces/` e implementada em `infra.data/repositories/`. Tanto o `CommandHandler` (escrita) quanto o `QueryController` (leitura) dependem só da interface, nunca da implementação concreta nem do EF Core diretamente.

## Por que usamos

O `domain` não pode depender de EF Core (ou de qualquer ORM) — é uma regra de arquitetura, não só de estilo, porque é o que garante que `domain` continue sem depender de nada externo (ver `docs/architecture.md`). O Repository é a peça que resolve isso: o domínio declara o que precisa (`ObterPorIdAsync`, `AdicionarAsync`...), e `infra.data` implementa usando o `DbContext`.

## Quando usar / quando não usar

Toda entidade que precisa ser persistida tem seu repositório correspondente, com uma interface em `domain/interfaces/` e implementação em `infra.data/repositories/`.

Não crie um repositório genérico (`IRepository<T>`) como abstração antecipada — cada repositório expõe só os métodos que as features realmente usam (`ObterPorProjetoAsync`, `ExisteAsync`...), não um CRUD genérico completo "para o caso de precisar depois".

## Exemplo no código

TODO — preencher quando a feature de exemplo real for portada para `examples/`.
