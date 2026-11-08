---
paths:
  - "src/infra.data/**"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Database Standards

> Leia este arquivo antes de criar/alterar: Entity Map, migration, coluna/tabela, enum no banco (PostgreSQL/EF Core).

Este documento define convenções e padrões para mapeamento e uso do PostgreSQL com Entity
Framework Core, pelo provider `Npgsql.EntityFrameworkCore.PostgreSQL`.

---

## 1. Mapeamento Explícito (Fluent API)

- Todo mapeamento deve ser feito via **Fluent API** (`IEntityTypeConfiguration<T>`).
- Classes de mapping ficam em: `Infra.Data/Context/Mappers/{Feature}/`.
- `OnModelCreating` deve chamar `modelBuilder.ApplyConfigurationsFromAssembly(...)`.

---

## 2. Convenções de Nomenclatura

Os nomes saem em **snake_case**, pela convenção global do `EFCore.NamingConventions` (§ 7): o
código fica em PascalCase e o banco em minúsculas, sem `HasColumnName` em cada propriedade. No
PostgreSQL, nome com maiúscula exige aspas em todo SQL escrito à mão, e é por isso que o banco não
leva maiúscula.

- **Tabelas:** snake_case plural, explícitas no `ToTable`
  - `produtos`, `clientes`, `itens_pedido`
- **Colunas:** snake_case descritivo, derivado da propriedade
  - `preco`, `usuario_inclusao`, `data_nascimento`
- **Foreign Keys:** `{entidade}_id`
  - `produto_id`, `cliente_id`
- **Índices:** `ix_{tabela}_{colunas}`, o nome que a convenção gera; índice único com `.IsUnique()`
  - `ix_produtos_nome`
- **Ids:** `uuid` gerado na entidade (`domain-foundation.md` § 2), sem sequence

`HasColumnName` só entra quando o nome da coluna precisa ser diferente do da propriedade.

---

## 3. Tipos de Dados

### PostgreSQL (OBRIGATORIO)

- **SEMPRE declarar tamanho e precisão**: `HasMaxLength(n)` em toda string, `HasPrecision(p, s)` em
  todo decimal
- **NAO usar** string sem tamanho (vira `text`) nem decimal sem precisão (vira `numeric` sem
  limite): o provider gera o tipo certo a partir deles, sem `HasColumnType` escrito à mão

| Tipo C# | Tipo PostgreSQL | Como |
|---------|-------------|---|
| string | `character varying(n)` | `HasMaxLength(n)` |
| string longa (texto livre, JSON cru) | `text` | `HasColumnType("text")`, com o motivo no review |
| DateTime | `timestamp with time zone` | sempre em UTC (§ Tipos de Data) |
| DateOnly | `date` | data sem hora |
| int/long | `integer`/`bigint` | convenção |
| decimal | `numeric(p,s)` | `HasPrecision(18, 2)` para dinheiro |
| bool | `boolean` | convenção |
| Guid | `uuid` | convenção |

Exemplo:

```csharp
builder.Property(x => x.Nome)
    .HasMaxLength(200)
    .HasComment("Nome completo do cliente")
    .IsRequired();

builder.Property(x => x.Valor)
    .HasPrecision(18, 2)
    .HasComment("Valor total do pedido")
    .IsRequired();
```

### Comentarios em Colunas (OBRIGATORIO)

- **SEMPRE adicionar `.HasComment()` em todas as colunas** nos mapeamentos (vira `COMMENT ON
  COLUMN` na migration)
- Comentarios devem ser descritivos e em portugues (sem acentos)
- Ajudam na documentacao do banco e facilitam entendimento de quem consulta direto no banco

```csharp
builder.Property(x => x.DataVencimento)
    .HasComment("Data de vencimento da fatura")
    .IsRequired();

builder.Property(x => x.Situacao)
    .HasConversion<string>()
    .HasMaxLength(50)
    .HasComment("Situacao atual da fatura (EmAberto, Paga, Vencida, Cancelada)")
    .IsRequired();
```

### Tipos de Data

- **Data sem hora é `DateOnly`** (`date`): vencimento, competência, data de nascimento. O Npgsql
  mapeia `DateOnly` nativamente, e `DateTime` com hora zerada é o que faz a data andar um dia
  quando alguém converte o fuso.
- **Instante é `DateTime` em UTC** (`timestamp with time zone`): o Npgsql 6+ **recusa** gravar
  `DateTime` com `Kind` diferente de `Utc` nessa coluna. Gere com `DateTime.UtcNow`, e converta para
  o horário local num ponto só, na saída (CLAUDE.md, Async e datas).
- **NAO ligar** `Npgsql.EnableLegacyTimestampBehavior`: ele devolve o comportamento antigo para o
  projeto inteiro e esconde o `DateTime` local que vazou.

---

## 4. Auditoria Obrigatória

Entidades persistidas devem conter:

- `usuario_inclusao` – usuário que criou o registro.
- `data_inclusao` – data/hora da criação.
- `usuario_alteracao` – usuário da última alteração.
- `data_alteracao` – data/hora da última alteração.

Essas colunas devem ser mapeadas explicitamente nos `Maps`, com `HasComment`, e são preenchidas
pelo interceptor do `SaveChanges` (`domain-foundation.md` § 2).

---

## 5. Value Objects

- VO simples (ex: `Cnpj`, `Email`) podem usar `HasConversion`.
- VO complexos (ex: `Endereco`, `Dimensoes`) devem usar `OwnsOne`.
- **Nome das colunas de VO deve ter prefixo obrigatorio** identificando o ValueObject:
  - Formato: `{prefixo_valueobject}_{nome_propriedade}`
  - `endereco_rua`, `endereco_numero`, `endereco_cep`
  - `dados_pessoais_nome_completo`
  - `dados_bancarios_banco`
  - No `OwnsOne`, a convenção já gera o prefixo pelo nome da navegação. No `HasConversion` de VO
    simples, a coluna leva o nome da propriedade da entidade, que já é o prefixo.

### 5.1. Enums (OBRIGATORIO)

- **SEMPRE armazenar enums como string** usando `HasConversion<string>()`, e nunca como `integer`
  nem como enum nativo do PostgreSQL (que exige migration para cada valor novo)

```csharp
builder.Property(x => x.Situacao)
    .HasConversion<string>()
    .HasMaxLength(50)
    .IsRequired();
```

### 5.2. Exemplo Completo com OwnsOne

```csharp
builder.OwnsOne(x => x.Fornecedor, fornecedor =>
{
    fornecedor.Property(c => c.Nome)
        .HasMaxLength(200)
        .HasComment("Nome completo do fornecedor")
        .IsRequired();

    fornecedor.Property(c => c.Cnpj)
        .HasConversion(v => v.Numero, v => new Cnpj(v))
        .HasMaxLength(14)
        .HasComment("CNPJ do fornecedor (somente numeros)")
        .IsRequired();

    fornecedor.Property(c => c.Email)
        .HasConversion(v => v.Endereco, v => new Email(v))
        .HasMaxLength(255)
        .HasComment("Email de contato do fornecedor")
        .IsRequired();
});
```

As colunas saem como `fornecedor_nome`, `fornecedor_cnpj` e `fornecedor_email`.

---

## 6. Shadow property e mapeamento pai/filha

A FK da filha de um agregado **não aparece na classe**: o id que só existe para amarrar o
relacionamento no EF é shadow property. A filha não recebe o id da raiz no construtor; o EF preenche
ao adicionar na coleção da raiz.

```csharp
// RaizMap
builder.HasMany(raiz => raiz.Itens)
    .WithOne()
    .HasForeignKey("RaizId")
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(raiz => raiz.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

// ItemMap
builder.Property<Guid>("RaizId")
    .IsRequired()
    .HasComment("Raiz a que o item pertence");
```

Vale só quando as duas condições valem:

1. **Há relacionamento de verdade no mesmo DbContext.** Id de registro de outro serviço não é FK —
   é referência, e fica como propriedade normal.
2. **O campo não é filtro do repositório.** Se a consulta faz `Where` por ele, a shadow obriga
   `EF.Property<T>(x, "RaizId")` na leitura e piora os dois lados.

Referência entre **agregados** é por id explícito, e não por navegação.

---

## 7. Convenção Global de Nomes

A convenção de snake_case é ligada uma vez, no registro do DbContext no IoC:

```csharp
services.AddDbContext<FaturasDbContext>(options => options
    .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());
```

Ela vale para tabela, coluna, chave e índice. Ligar ou desligar depois renomeia tudo numa migration
só, e por isso ela entra antes da primeira.

---

## 8. Migrations

- Sempre criar migrations com nomes descritivos em português.
- Sempre revisar a migration antes de aplicar.
- Nunca editar migration já aplicada em produção.
- Preferir migrations **atômicas** (uma mudança conceitual por migration).

Comandos comuns:

```bash
# Criar migration
dotnet ef migrations add AdicionarTabelaProdutos --startup-project ../{ProjectName}.Api

# Aplicar migrations
dotnet ef database update --startup-project ../{ProjectName}.Api

# Remover última migration (não aplicada)
dotnet ef migrations remove --startup-project ../{ProjectName}.Api
```

---

## 9. Scripts SQL

- Gerar scripts quando necessário, idempotentes, para rodar em produção sem saber em que migration o
  banco está:

```bash
dotnet ef migrations script --idempotent --startup-project ../{ProjectName}.Api --output migration.sql
```
