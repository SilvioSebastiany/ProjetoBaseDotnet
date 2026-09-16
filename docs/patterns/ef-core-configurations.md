# EF Core Configurations

## O que é

O mapeamento de cada entidade para a tabela do banco é feito via Fluent API, em uma classe `IEntityTypeConfiguration<T>` separada, dentro de `infra.data/conventions/` — nunca com Data Annotations (`[Key]`, `[Column]` etc.) na própria entidade.

## Por que usamos

Separar a configuração da entidade mantém `domain/entities/` limpo — a entidade é só o modelo de negócio, sem nenhum conhecimento de como ela é persistida. Isso também é o que garante que `domain` não precise referenciar o pacote do EF Core. Toda a conversão de nomes (para snake_case, convenção do PostgreSQL) e o mapeamento de tipos ficam concentrados em um único lugar por entidade, fácil de achar e de revisar.

## Quando usar / quando não usar

Toda entidade nova tem uma `Configuration` correspondente em `infra.data/conventions/`, registrada no `DbContext`.

Não coloque lógica de negócio na `Configuration` — ela só descreve mapeamento (nome de coluna, tipo, chave, relacionamento), nunca decide nada.

## Exemplo no código

TODO — preencher quando a feature de exemplo real for portada para `examples/`.
