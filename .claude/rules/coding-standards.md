---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.props"
  - "nuget.config"
  - ".editorconfig"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Coding Standards

> Leia este arquivo antes de criar/alterar: qualquer classe C# (records, nullable, comentarios, var, colecoes, returns, datas).

Padrões gerais de código: construtores, nullable reference types, comentários, coleções,
`return` e datas.

Regra que depende da versão de .NET chega aqui já filtrada para o `TargetFramework` deste
repositório — o que você lê é o que compila aqui.

---

## 1. Organização de Arquivos

### 1.1. Uma Definição por Arquivo (OBRIGATÓRIO)

- **SEMPRE criar um arquivo separado** para cada classe, interface, enum ou struct
- **NUNCA colocar múltiplas definições de tipos no mesmo arquivo**
- **NUNCA usar `record`** - sempre preferir `class` (ver seção 3.3)
- O nome do arquivo deve corresponder exatamente ao nome do tipo definido

Correto:

```
Domain/Entities/
├── Pedido.cs           # Contém apenas a classe Pedido
├── ItemPedido.cs       # Contém apenas a classe ItemPedido
└── Cliente.cs          # Contém apenas a classe Cliente

Domain/Enums/
├── EStatusPedido.cs    # Contém apenas o enum EStatusPedido
└── ETipoPagamento.cs   # Contém apenas o enum ETipoPagamento

Domain/Interfaces/
├── IPedidoRepository.cs    # Contém apenas a interface IPedidoRepository
└── IPedidoService.cs       # Contém apenas a interface IPedidoService
```

Incorreto:

```csharp
// NAO fazer isso - multiplas classes no mesmo arquivo
public class Pedido { }
public class ItemPedido { }
public enum EStatusPedido { }
```

### 1.2. Using Directives no Topo (OBRIGATÓRIO)

- **SEMPRE usar `using` directives no topo do arquivo** para importar namespaces
- **NUNCA usar namespaces completos (fully qualified names) inline no código**
- Isso melhora a legibilidade e mantém o código mais limpo

Correto:

```csharp
using Domain.Entities.Ativos;

public class ProcessoService
{
    public void ProcessarAtivosOtimizados(Processo processo) { }
    
    public void ProcessarAtivosOtimizados(Ativo ativo) { }
}
```

Incorreto:

```csharp
// NAO fazer isso - usando namespace completo inline
public class ProcessoService
{
    public void ProcessarAtivosOtimizados(Processo processo) { }
    
    public void ProcessarAtivosOtimizados(Domain.Entities.Ativos.Ativo ativo) { }
}
```

### 1.2.0. Estilo de `namespace`: o padrão NÃO governa

Bloco (`namespace X { ... }`) e de arquivo (`namespace X;`, C# 10) valem os dois. **Siga o
arquivo que você está editando**; num arquivo novo, siga o que domina no projeto.

Isto está escrito porque o silêncio aqui não era neutro: todos os exemplos deste acervo usam
namespace em bloco, e gabarito sem regra ao lado vira regra de facto — quem lê conclui que o
bloco é o padrão e "corrige" arquivos que não estavam errados.

E não há o que descobrir: projeto antigo costuma ser misto, inclusive **dentro do mesmo
repositório**. Uniformizar seria um diff em centenas de arquivos, sem nada em troca.

### 1.2.1. Limpar e ordenar os `using` ao tocar no arquivo (OBRIGATÓRIO)

**Todo arquivo que você alterar sai com os `using` limpos e ordenados** — não só o bloco que você
mexeu. São duas coisas:

1. **Remover os que não estão em uso.**
2. **Ordenar alfabeticamente, de forma literal**, sem privilegiar `System`.

Ordem literal significa comparar a string inteira, então os `System` **não** vão para o topo:
ficam depois de `Faturas.*`, `MediatR` e `Microsoft.*`, na posição que a letra dá:

```csharp
using Faturas.Domain.Notifications;
using MediatR;
using MeuProjeto.Domain.Interfaces.Repositories.Pedidos;
using System;
using System.Text.Json;
using System.Threading.Tasks;
```

É exatamente o que o **Ctrl+R+G** do Visual Studio faz com
`dotnet_sort_system_directives_first = false`, e o `.editorconfig` da raiz deve fixar essa opção — então
IDE e linha de comando produzem o mesmo resultado, sem diff de reordenação entre quem editou onde.

Na linha de comando:

```bash
dotnet format <solucao>.sln \
  --diagnostics IDE0005 --severity warn \
  --include <arquivo ou pasta>
```

Sempre com `--include`: sem ele o comando reformata a solução inteira e o diff fica ilegível.

### 1.3. Uso de `var` para Variáveis Locais (OBRIGATÓRIO)

- **SEMPRE usar `var`** para declaração de variáveis locais
- **NUNCA usar tipo explícito** na declaração de variáveis locais
- O tipo é inferido pelo compilador e melhora a legibilidade do código

Correto:

```csharp
var fatura = await faturaRepository.ObterAsync(id, ct);
var itens = new List<ItemPedido>();
var resultado = await mediator.Send(command, ct);
var nome = "João";
var quantidade = 10;
```

Incorreto:

```csharp
// NAO fazer isso - tipo explicito em variaveis locais
Fatura fatura = await faturaRepository.ObterAsync(id, ct);
List<ItemPedido> itens = new List<ItemPedido>();
string nome = "João";
int quantidade = 10;
```

### 1.4. Sufixo `Async` (OBRIGATÓRIO)

- **Todo método assincrono** (`Task`/`Task<T>`) **DEVE** ter o sufixo `Async` — inclusive **actions de Controller** e handlers proprios. Deixa explicito que o metodo e assincrono.
- Exceção: metodos que implementam interface de terceiros com nome fixo (ex.: `IRequestHandler.Handle` do MediatR) — mantem o nome da interface.
- **Armadilha na action:** o ASP.NET Core tira o `Async` do nome da action por padrão
  (`SuppressAsyncSuffixInActionNames`), então `CreatedAtAction(nameof(ObterAsync), ...)` não acha a
  rota e estoura em execução. Use `Created($"/recurso/{id}", response)`, que é a forma padrão, ou
  um nome de rota explícito.

### 1.5. Indentação de Argumentos (>3) (OBRIGATÓRIO)

- Quando uma **chamada** ou **declaração** de método/construtor tiver **mais de 3 parâmetros**, quebrar **um argumento por linha** (indent all arguments):

```csharp
return await assinaturaIPlaceOneRepository.ListarPaginadoAsync(
    request,
    request.Tier,
    request.Status,
    cancellationToken);
```

- Ate 3 parametros pode ficar em linha unica.

### 1.6. `is`, `or`, `and` e `not` no lugar de `==`, `||` e `&&`

Quando a expressão é comparada com uma **constante** (`null`, enum, literal, faixa), use pattern
matching: a expressão aparece uma vez só, e `is null` não passa por um `==` sobrecarregado.

```csharp
// certo
if (cliente is null)
if (cliente is not null)
if (situacao is ESituacao.Ativo or ESituacao.Suspenso)
if (situacao is not (ESituacao.Cancelado or ESituacao.Encerrado))
if (percentual is >= 0 and <= 60)

// errado, quando dá para usar pattern
if (cliente == null)
if (situacao == ESituacao.Ativo || situacao == ESituacao.Suspenso)
if (percentual >= 0 && percentual <= 60)
```

**Quando não dá, fica o operador — sem contorno para encaixar no padrão:**

- **Entre duas expressões** (`pedido.ClienteId == cliente.Id`): pattern só compara com constante.
- **Dentro de consulta do EF** (`IQueryable`, o `Where` do repositório): árvore de expressão não
  aceita pattern (erro CS8122), então `==`, `||` e `&&` continuam ali.
- **Condições sobre expressões diferentes** (`ativo && saldo > 0`): `&&` e `||` continuam. Não monte
  um property pattern só para trocar o operador.
- **Texto com regra de caixa ou cultura:** `string.Equals(a, b, StringComparison...)`.

### 1.7. Argumento `default` ou `null` leva o nome do parâmetro (OBRIGATÓRIO)

Na chamada, `default` e `null` soltos não dizem **o que** está ficando vazio: quem lê tem de abrir a
assinatura para descobrir. Passe o argumento pelo nome.

```csharp
// certo
return new ObterDocumentosAceiteCommandResult(termoAceite: default, relatorioAssinatura: default);

// errado
return new ObterDocumentosAceiteCommandResult(default, default);
```

- Vale para chamada de método e de construtor, em produção e em teste, e para o estilo de um
  argumento por linha (§ 1.5): a linha do `default` leva o nome.
- **Fica de fora o que já diz o tipo:** `return default;`, `default(T)` e o `(T)null` que desambigua
  sobrecarga, como no `ReturnsAsync((Pedido)null)` do Moq. Também o valor padrão de parâmetro na
  declaração (`Pedido padrao = null`) e o argumento de atributo (`[InlineData(null, 1)]`).

---

## 2. Comentários e Documentação

### 2.1. Regras Gerais

- **NÃO adicionar comentários XML de summary** (`/// <summary>`) no código gerado
- **EXCEÇÃO:** Controllers podem ter comentários XML para documentação do Swagger
- **EXCEÇÃO:** Job leva summary na classe dizendo o que o processo faz e de onde puxa — não há
  chamador nem Swagger para ler
- **EXCEÇÃO:** em projeto de teste, `[Fact]`/`[Theory]` e helper de fixture podem ter summary
  dizendo **qual regra de negócio o teste protege** — não o que ele executa
- **EXCEÇÃO:** propriedade calculada pode ter summary **quando ela encapsula regra ou restrição que
  o nome não consegue carregar** — o summary aparece no IntelliSense no ponto de uso, onde comentário
  inline não chega. Derivação óbvia (concatenar, somar, negar um booleano) não leva
- **OBRIGATÓRIO em pacote NuGet publicado** (SDK, pacote de suporte): todo tipo e membro público ou
  protegido tem summary, e o csproj do pacote trava isso — quem consome está em **outro repositório**
  e não tem o código para ler (seção "Comentários", *Pacote NuGet publicado*)
- **NÃO usar `#region` e `#endregion`** para organização de código
- Código deve ser autoexplicativo através de bons nomes de classes, métodos e variáveis
- Comentários inline só devem ser usados quando absolutamente necessários para explicar lógica complexa
- Organização do código deve ser feita através de boa estrutura de classes e arquivos separados

---

## 3. Construtores Primários

### 3.1. Quando USAR

**USAR construtores primários** em classes de infraestrutura:

- Controllers
- Repositories
- DbContext
- UnitOfWork
- Workers/BackgroundServices
- Handlers (CommandHandler)
- Services de infraestrutura

Exemplo:

```csharp
public class FaturaRepository(FaturasDbContext context) : IFaturaRepository
{
    public async Task<Fatura> ObterAsync(string id, CancellationToken ct)
    {
        return await context.Faturas.FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
```

```csharp
public class AdicionarFaturaCommandHandler(
    IFaturaRepository faturaRepository,
    INotificationContext notificationContext,
    IUnitOfWork unitOfWork) : IRequestHandler<AdicionarFaturaCommand, Fatura>
{
    public async Task<Fatura> Handle(
        AdicionarFaturaCommand request,
        CancellationToken cancellationToken)
    {
        // implementacao
    }
}
```

### 3.2. Quando NÃO USAR

**NÃO usar construtores primários** em:

- Entidades (Domain)
- ValueObjects
- DTOs/Responses
- Commands

Essas classes devem manter construtores tradicionais para:

- Permitir construtores privados/protegidos para EF Core
- Manter encapsulamento de invariantes de domínio
- Evitar exposição de dependências em objetos de dados

---

## 3.3. Records vs Classes (OBRIGATÓRIO)

- **NUNCA usar `record`** para DTOs, Responses, Requests, Commands, CommandResults ou qualquer tipo de dados
- **SEMPRE usar `class`** como padrão para todos esses tipos
- Records podem parecer mais concisos, mas causam problemas de compatibilidade com serialização, binding de model e algumas bibliotecas
- A consistência com `class` facilita manutenção e evita comportamentos inesperados

Correto:

```csharp
public class AdicionarFaturaCommand : IRequest<Fatura>
{
    public string Nome { get; set; }
    public Cpf Cpf { get; set; }
}

public class AdicionarFaturaResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
}
```

Incorreto:

```csharp
// NAO usar record para Commands
public record AdicionarFaturaCommand(string Nome, Cpf Cpf) : IRequest<Fatura>;

// NAO usar record para Responses
public record AdicionarFaturaResponse(string Id, string Nome);
```

---

## 4. Nullable Reference Types

### 4.1. Configuração do Projeto

**SEMPRE usar `<Nullable>disable</Nullable>`** nos arquivos .csproj

Isto vai **contra o padrão do SDK**: todo projeto criado do .NET 6 em diante nasce com
`enable`, e desligar é uma alteração deliberada, não um descuido de quem copiou o `.csproj`.

A razão é o custo de ligar depois. Nullable num código que não nasceu com ele acende centenas
de avisos de uma vez, e aviso que ninguém consegue zerar é aviso que se aprende a ignorar —
junto com os que importavam. É a mesma razão pela qual as checagens mecânicas de um padrão rodam
sobre as linhas **adicionadas** num diff, e não sobre o repositório inteiro.

### 4.2. Regras de Uso

- **NÃO usar `?` em tipos de referência** (string, classes, interfaces, etc.)
- **NÃO usar `= null!`** para inicialização de propriedades

### 4.3. Quando USAR Nullable

**USAR `?` APENAS em tipos primitivos/value types** que podem ser nulos:

- `DateTime?`, `TimeOnly?`
- `int?`, `long?`, `decimal?`, `double?`, `float?`
- `bool?`, `char?`, `byte?`
- `Guid?`
- Enums nullable (ex: `EStatusPedido?`)

### 4.4. Exemplos

Correto:

```csharp
public string Nome { get; set; }                 // string sem ?
public DateTime? DataCancelamento { get; set; }  // DateTime nullable
public int? NumeroCalcado { get; set; }          // int nullable
public Cpf Cpf { get; set; }                     // classe sem ? e sem = null!
public ESituacaoFatura? Situacao { get; set; } // enum nullable
```

Incorreto:

```csharp
public string? Nome { get; set; }                // NAO usar ? em string
public Cpf Cpf { get; set; } = null!;            // NAO usar = null!
public Email? Email { get; set; }                // NAO usar ? em classes
```

---

## 5. Tipos de Data

### 5.1. DateTime vs DateOnly

- **Data sem hora é `DateOnly`**: vencimento, competência, nascimento. Um `DateTime` com a hora
  zerada carrega um fuso que ninguém pediu, e é na conversão de fuso que a data anda um dia
- **Instante é `DateTime` em UTC**, gerado com `DateTime.UtcNow`: o Npgsql recusa gravar
  `DateTime` local na coluna `timestamp with time zone` (`database-standards.md` § 3). A
  conversão para o horário local é uma só, na saída
- `DateTimeOffset` só quando o fuso de origem é dado do negócio (o horário que o parceiro mandou)

Correto:

```csharp
public DateOnly DataVencimento { get; private set; }
public DateTime? DataCancelamento { get; private set; }   // gravado com DateTime.UtcNow
```

Incorreto:

```csharp
public DateTime DataVencimento { get; set; }   // data sem hora em DateTime
DataCancelamento = DateTime.Now;               // instante local: o Npgsql recusa
```

---

## 6. Coleções

O tipo depende do **cenário**, não do gosto. São três, e trocar um pelo outro tem consequência
concreta:

| Cenário | Tipo | Por quê |
|---|---|---|
| **Entidade** (com ou sem alteração) | `List<T>` privada + `IReadOnlyCollection<T>` | nunca nula, encapsulamento real, e o Add é onde a invariante mora (§6.1) |
| **Contrato serializado** (DTO de importação, Request, Response) | `List<T>` com `{ get; set; }` **e inicializador** | ver §6.3 |
| **Parâmetro** de método | `IEnumerable<T>` | aceita qualquer coleção sem forçar conversão no chamador; materialize com `ToList()` se for percorrer mais de uma vez |

### 6.1. Coleções em Entidades: sempre encapsuladas (OBRIGATÓRIO)

O padrão é `List<T>` privada exposta como `IReadOnlyCollection<T>` — **mesmo quando a coleção só é
lida**, e mesmo quando o único método seria um `Adicionar`. O item da coleção muda só pela ação da
raiz, e o método de mudança da filha é `internal` (`domain-modeling.md` § 3).

```csharp
private readonly List<FluxoStatusTransicao> _transicoes = new List<FluxoStatusTransicao>();

public IReadOnlyCollection<FluxoStatusTransicao> Transicoes => _transicoes.AsReadOnly();
```

**Por que não `IEnumerable<T>` com setter privado**, que parece mais simples:

1. **Fica nula.** Sem inicializador, a navegação que o `Include` não trouxe vem `null`, e o
   `foreach` estoura. Todo consumidor passa a precisar de guarda; com o campo inicializado, viria
   vazia.
2. **Não encapsula nada.** `((List<T>)pedido.Itens).Add(...)` compila e funciona. Já o
   `ReadOnlyCollection<T>` devolvido por `AsReadOnly()` não aceita esse cast.
3. **Tira o lugar da regra.** É no `Adicionar` que a invariante entra — ex.: `Pedido.AdicionarItem`
   garante que o mesmo produto não entra duas vezes, e sem esse ponto de entrada a regra não teria
   onde morar.

O EF popula o campo privado por convenção (`_transicoes` ↔ `Transicoes`), sem configuração no Map.

> **Lazy geralmente não é o problema.** Sem `UseLazyLoadingProxies` e sem navegação `virtual`,
> acessar a propriedade não dispara consulta — confira no `DbContext` do projeto. O risco de
> `IEnumerable<T>` é o nulo, não o lazy.

> **Cuidado com `AsReadOnly()` em caminho quente:** ele aloca um wrapper novo **a cada acesso à
> propriedade**. Num laço sobre a coleção dentro de outro laço, são alocações repetidas. Se isso
> pesar, guarde o wrapper num campo em vez de criá-lo no getter — mas não troque por
> `=> _lista`, que devolve a `List` e reabre o cast.

`IEnumerable<T>` com setter privado sobrevive no que já existe; não usar em coisa nova.

### 6.2. Coleções Alteráveis

Se **HOUVER necessidade de alteração** (Add, Remove), usar padrão de encapsulamento:

```csharp
private readonly List<Dependente> _dependentes = [];

public IReadOnlyCollection<Dependente> Dependentes => _dependentes.AsReadOnly();

public void AdicionarDependente(Dependente dependente)
{
    _dependentes.Add(dependente);
}

public void RemoverDependente(Dependente dependente)
{
    _dependentes.Remove(dependente);
}
```

### 6.3. Coleções em contrato serializado (OBRIGATÓRIO)

Em tipo que vai e volta por JSON — arquivo de importação/exportação, Request, Response — a coleção é
`List<T>` com **`{ get; set; }` público** e **inicializador**:

```csharp
public List<ItemPedido> Perguntas { get; set; } = new List<ItemPedido>();
```

**O setter público não é descuido, é requisito.** Propriedade só com `get` é ignorada em silêncio
pelo `System.Text.Json`: o JSON traz os itens, a propriedade fica **vazia**, e nenhuma
exceção é lançada. Medido no net7.0 e no net9.0.

```csharp
// ERRADO - os itens do arquivo desaparecem sem erro nenhum
public IReadOnlyCollection<string> Documentos { get; } = new List<string>();
```

**Não use `IEnumerable<T>` aqui**, ainda que a coleção seja só lida: ela aceita sequência lazy,
`Count()` re-enumera, e quem consome o contrato percorre a mesma coleção várias vezes (indexar por
chave, resolver referências, comparar com o destino). `IReadOnlyList<T>` desserializa e comunica
melhor a intenção, mas com setter público a imutabilidade é fachada — `((List<T>)x).Add(...)`
funciona —, então não paga a complexidade.

**O inicializador** garante coleção vazia quando o campo não vem no JSON, e é o que dispensa
checagem de nulo em quem lê o contrato.

---

## 7. Inicialização de Coleções

Usar sintaxe de inicialização de coleção:

```csharp
private readonly List<Dependente> _dependentes = [];
```

Em vez de:

```csharp
private readonly List<Dependente> _dependentes = new List<Dependente>();
private readonly List<Dependente> _dependentes = new();
```

---

## 8. Return Statements

### 8.1. Regra Geral

- **NUNCA usar `return null`** em nenhuma situação
- **SEMPRE usar `return default`** quando precisar retornar um valor nulo/padrão

### 8.2. CommandHandlers (MediatR)

- **NUNCA usar `return Unit.Value`** em CommandHandlers
- **SEMPRE usar `return default`** para handlers que retornam `Unit`

Correto:

```csharp
public async Task<Unit> Handle(AtualizarFaturaCommand request, CancellationToken cancellationToken)
{
    // implementação
    return default;
}
```

Incorreto:

```csharp
public async Task<Unit> Handle(AtualizarFaturaCommand request, CancellationToken cancellationToken)
{
    // implementação
    return Unit.Value;  // NAO usar Unit.Value
}
```

### 8.3. Métodos com Retorno de Objeto

Correto:

```csharp
public async Task<Fatura> ObterAsync(string id, CancellationToken ct)
{
    var fatura = await context.Faturas.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (fatura is null)
        return default;
    
    return fatura;
}
```

Incorreto:

```csharp
public async Task<Fatura> ObterAsync(string id, CancellationToken ct)
{
    var fatura = await context.Faturas.FirstOrDefaultAsync(c => c.Id == id, ct);
    if (fatura is null)
        return null;  // NAO usar return null
    
    return fatura;
}
```

---

## 9. Aviso trava o build (OBRIGATÓRIO)

Projeto em .NET 10 ou acima liga a trava, e todo aviso do build passa a ser erro:

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

- **Onde:** num repositório de vários projetos, uma vez no `Directory.Build.props` da raiz, que
  cobre todos, inclusive os de teste (§ 10). Com um projeto só, no `.csproj` dele.
- **O que trava:** todo aviso que o build emite. Do compilador (`CS`), dos analisadores que o
  projeto liga (`CA`, `IDE` com severidade de aviso, `xUnit`), e do NuGet no restore, inclusive
  vulnerabilidade conhecida num pacote, direto ou transitivo (`NU1901` a `NU1904`), e pacote que o
  framework já traz (`NU1510`).
- **Por quê:** o projeto chegou ao LTS sem aviso, e a trava é o que o mantém assim. Sem ela, o
  aviso sai no log de um pipeline verde, ninguém lê, e o pacote ou o serviço é publicado com ele.
  Com ela, o build falha, e a publicação não acontece até o aviso ser corrigido. Vulnerabilidade
  anunciada numa dependência trava a publicação seguinte até a versão subir: é de propósito.
- **A saída é corrigir.** Supressão é exceção: no ponto exato
  (`[SuppressMessage(..., Justification = "...")]`), com o motivo escrito, e citada na descrição
  do PR. `<NoWarn>`, `<WarningsNotAsErrors>` ou a trava desligada no projeto inteiro desfazem a
  regra sem ninguém ver, e não são saída.
- **Ao migrar para o .NET 10:** zere os avisos e ligue a trava no mesmo PR. Ligar antes deixa o
  build vermelho; deixar para depois costuma ser não ligar.

Vale do .NET 10 em diante, o LTS de destino da migração de 2026. Projeto em versão anterior não
recebe esta seção: ali ainda há migração a fazer, e travar aviso em código que vai mudar de alvo
só atrasa a migração.

---

## 10. Configuração comum na raiz do repositório (OBRIGATÓRIO)

Repositório com mais de um projeto (`.csproj`) em .NET 10 ou acima mantém na raiz três arquivos:
`Directory.Build.props`, `Directory.Packages.props` e `nuget.config`. O que é igual em todos os
projetos fica num lugar só, e o projeto novo herda sem que alguém se lembre de copiar.

Repositório com um projeto só não precisa de nenhum dos três: o `.csproj` basta.

### 10.1. `Directory.Build.props`

O que vale para todos os projetos do repositório:

```xml
<Project>
  <PropertyGroup>
    <Nullable>disable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- **O `TargetFramework` fica no `.csproj` de cada projeto.** É lá que as ferramentas e
  quem abre o projeto procuram o alvo, e o projeto com mais de um alvo varia.
- **O que é de um projeto só fica no `.csproj` dele:** `OutputType`, `IsPackable`, o
  `AnalysisLevel` recomendado do `src` (no projeto de teste ele liga o CA1707 contra o nome
  `Deve_..._Quando_...`), os metadados e a trava de documentação de pacote NuGet.
- **Vale para todo projeto abaixo da pasta**, inclusive os de teste. O MSBuild usa o primeiro
  `Directory.Build.props` que acha subindo do projeto; um mais acima só entra se este o importar.
  O da raiz do repositório é o único que vai junto para o pipeline.

### 10.2. `Directory.Packages.props`: uma versão por pacote

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>
</Project>
```

E no `.csproj`, a referência **sem** versão: `<PackageReference Include="Newtonsoft.Json" />`. Com
versão no `.csproj`, o NuGet recusa o restore (`NU1008`).

- Um pacote tem uma versão no repositório inteiro, e subir é uma linha.
- **Dependência transitiva vulnerável:** num serviço, ligue
  `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` e suba
  pela `PackageVersion`, em vez de uma `PackageReference` solta só para forçar a versão.
- **Em repositório de pacote NuGet, o transitive pinning fica desligado.** Com ele, o pacote
  publicado passa a declarar como dependência o transitivo fixado, que o `.csproj` não declara.
- `VersionOverride` num `.csproj` é exceção, com o motivo na descrição do PR.

### 10.3. `nuget.config`: de onde vem cada pacote

O `nuget.config` na raiz declara a origem, com `<clear />` antes: sem ele, o restore soma as
origens configuradas na máquina de quem roda, e o build muda de máquina para máquina. Com uma
origem só, o mapeamento de origem é opcional; ele passa a ser obrigatório (`NU1507`, que a trava do
§ 9 transforma em erro) no dia em que entrar uma segunda.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

- **Origem nova, sempre em HTTPS.** Em HTTP, o .NET 10 recusa a origem sem
  `allowInsecureConnections` (`NU1302`).
- **Com duas origens, mapeie por prefixo** (`<packageSourceMapping>`): é a proteção contra um
  pacote com o mesmo nome publicado com versão maior na outra origem.
- **No pipeline, o restore usa este arquivo**, sem `--source` na linha de comando: com o
  mapeamento ligado, `--source` só funciona se a URL for idêntica à do `nuget.config` (`NU1100`).

---

## Comentários (regra endurecida em 24/07/2026; exceções de teste e de propriedade calculada em 11/08/2026; contrato público de pacote em 15/09/2026)

- **Controllers**: XML doc (`/// <summary>`) SEMPRE — alimenta o Swagger.
- **Job**: XML doc na classe, dizendo **o que o processo faz e de onde ele puxa** — e o que a justifica é a
  falta de leitor: job não tem chamador para se ler, não tem Swagger, e o nome só cabe a operação.
  Quem abre o arquivo costuma estar de plantão atrás do que aquilo mexe.
- **Teste**: `[Fact]`/`[Theory]` e helper de fixture podem ter XML doc, e é o único lugar onde
  "registrar o porquê" É o valor — o summary diz por que aquele caso existe, o assert diz o que
  acontece. Mesma barra de precisão: summary que só reescreve o nome do método é ruído e sai. As
  proibições de citar chamado, documento, fase ou narrar outro serviço valem igual (ver "O leitor
  não tem o nosso contexto").
- **Marcadores AAA**: `// Arrange`, `// Act` e `// Assert` não contam como comentário. São a
  estrutura que o `testing-guidelines.md` pede em todo teste, e ficam mesmo quando o nome do teste
  já diz tudo. Quando o assert é a própria chamada (`Assert.Throws`), o marcador é `// Act e Assert`.
- **Propriedade calculada**: pode ter XML doc quando encapsula regra ou restrição de negócio que o
  nome não carrega. O que justifica aqui é **mecânico, não estético**: quem lê
  `if (cliente.ExigeNomeCompleto)` numa outra classe vê o summary no IntelliSense e não veria um
  comentário inline. Uma a três linhas, dizendo a regra e a quem ela **não** se aplica quando isso
  não é óbvio. Não vale para derivação óbvia (`NomeCompleto => $"{Nome} {Sobrenome}"`,
  `EstaVazio => !Itens.Any()`): aí o nome já diz tudo e o summary é ruído.
- **Pacote NuGet publicado** (SDK, pacote de suporte) — **OBRIGATÓRIO, em tudo** (decisão de
  08/10/2026). Pacote de suporte precisa estar muito bem documentado: quem consome está em **outro
  repositório** e não tem o código para ler. O summary no IntelliSense é o único canal, e sem ele a
  pergunta vira mensagem para quem escreveu.
  - **Todo tipo e todo membro público ou protegido** tem summary: classe, interface, enum e seus
    valores, constante, propriedade, construtor, método e extensão. O interno do pacote se lê como
    qualquer classe, e não precisa.
  - **O csproj do pacote trava** (só o de `src/`; o de teste fica fora):

    ```xml
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <WarningsAsErrors>$(WarningsAsErrors);CS1591;CS1572;CS1573;CS1574</WarningsAsErrors>
    ```

    `CS1591` é membro sem summary; `CS1572` e `CS1573`, `<param>` que não existe ou que falta;
    `CS1574`, `cref` que não resolve. Assim o summary que ficou para trás de uma mudança de
    assinatura também não compila. O XML vai no `.nupkg`, e é ele que chega ao IntelliSense.
  - **O summary diz o contrato:** o que faz, o que devolve, quando devolve `default` ou vazio, o que
    lança e quando, o efeito colateral (inclusive global, como fixar uma cultura), a unidade, o
    formato e a consequência de escolher errado. `<param>` e `<returns>` quando o nome não basta, e
    `<exception cref>` quando o método lança de propósito. Preferir o que é **verificável** a adjetivo.
    Summary que só reescreve o nome (`/// <summary>O código da empresa.</summary>` em `Codigo`) não
    conta como documentado.
  - **O README do pacote acompanha:** para que serve, instalação, a tabela de versão e alvo, o uso
    de cada recurso com exemplo, as armadilhas, e o que é legado com o que usar no lugar.
  - **Pacote existente ganha a trava no primeiro PR que mexer nele**, que documenta o que faltar e
    atualiza o README. Não se abre PR só para isso.
- **Demais classes**: comentário SOMENTE quando o código não está claro o suficiente (regra de negócio não óbvia, restrição, premissa — ex.: convenção de timezone, semântica de um campo que o nome não expressa).
- **Código autoexplicativo NÃO leva comentário** — se o nome do método/propriedade já diz o que faz, comentário é ruído.
- XML docs antigos no Domain: avaliar caso a caso ao tocar no arquivo (manter os que documentam regra não óbvia; remover os redundantes); não fazer varredura em massa.

### O padrão é NÃO comentar (OBRIGATÓRIO)

**Não escreva comentário.** É essa a regra. Comentário é exceção que precisa se justificar, e a barra é alta: só entra quando o código é **genuinamente não óbvio** — uma armadilha real, uma restrição contra-intuitiva que faria um dev competente errar. **Na dúvida, não escreva.**

Não basta a informação ser verdadeira ou útil: se ela não impede um erro concreto naquela linha, é ruído.

**Comentário NÃO se justifica por:**

- "foi uma decisão nossa" / "vale registrar o porquê" — decisão vai no doc, não no código;
- "o contexto está em outro lugar" — se o leitor precisa do contexto, ele não deveria estar preso a uma linha de C#;
- "ajuda quem for mexer depois" — quem ajuda é **nome melhor e teste**;
- "é só uma linha".

**Quando sentir vontade de explicar, o movimento certo é quase sempre outro:** renomear o método, a variável ou o teste. Um teste `Deve_{Resultado}_Quando_{Condicao}` documenta comportamento melhor que qualquer comentário — e, diferente do comentário, **não envelhece em silêncio**: se o comportamento muda, ele quebra.

**Em review, comentário solto ou impreciso é DEFEITO** — mesmo peso de código morto. Remove-se, não se reescreve.

### O leitor não tem o nosso contexto (OBRIGATÓRIO)

Se o comentário passou pela barra acima, ele ainda precisa passar por esta: escreva para um dev que abriu **só este arquivo**, não acompanhou o projeto e não vai ler documento nenhum.

**PROIBIDO em comentário de código:**

- Referência a documento, seção ou história: `US-x`, `§x`, `RN-x`, "conforme o doc de negócio".
- Referência a fase/etapa/tarefa do nosso planejamento: "Fatia B", "Fase 2", "FIX 1", "Marco 3", "TODO do ticket X".
- Referência a **outro projeto/serviço** ou ao comportamento interno dele: "o serviço X captura", "igual o sistema Y faz".
- Narrar o fluxo de **outras classes**: "o `XCommandHandler` retorna antes do `SaveChanges`, então…".
- Histórico/decisão de sessão: "antes era X, mudamos porque…", "não reintroduzir isso".
- **Reafirmar regra de negócio** que já está no código ou no teste: "pedido cancelado não volta a pendente", "o total já inclui o frete".
- **Descrever o que a linha seguinte faz**, mesmo em outras palavras.

**O que um bom comentário tem (quando existir):**

- Fala **daquela linha ou daquele bloco**, não do sistema.
- Explica **por que**, quando o "o quê" já está no código.
- **Uma linha.** Se precisou de duas, revise; se precisou de parágrafo, o problema é o código.

**Exemplos**

```csharp
// RUIM - contexto que o leitor não tem, e narra outra classe
// US-E1/US-E4: a pausa CONGELA o ciclo inteiro (Renovar() recusa cobrar enquanto Pausado). A reativacao
// "ajusta a data-aniversario" deslocando o inicio e o pago-ate; o ConfirmarPagamentoCobrancaCommandHandler
// retorna antes do SaveChangesAsync, o que travaria o pagamento na reconciliacao de 30 min.
```

```csharp
// RUIM - repete o que o código já diz
// Verifica se o tier e Freemium e adiciona notificacao
if (Tier is ETierIPlaceOne.Freemium)
```

```csharp
// RUIM - verdadeiro, preciso, uma linha... e ainda assim ruído:
// o comportamento já está fixado por um teste, que não envelhece em silêncio
// Desloca inicio e fim juntos: a renovacao reancora no dia do inicio, entao mover so o fim
// devolveria o periodo compensado no ciclo seguinte.
Vigencia = new Vigencia(Vigencia.DataInicio.Value.AddDays(dias), Vigencia.DataFim.Value.AddDays(dias));

// BOM - nenhum comentário; quem documenta é o teste:
// Deve_DeslocarInicioEFimPelosMesmosDias_Quando_ReativadaDaPausa
```

O único caso que costuma passar a barra é a **armadilha externa** — algo que um dev competente "consertaria" e quebraria:

```csharp
// BOM - sem isso alguem alinha a string ao C# e gera migration a mais
.HasComment("Meio de pagamento (EMeioPagamentoSAP)")
```

Em **testes** vale o mesmo: o nome `Deve_{Resultado}_Quando_{Condicao}` já é a documentação. Comentário só quando o *arranjo* tem uma sutileza não óbvia (ex.: por que uma data específica foi escolhida) — e sem citar "FIX N" nem número de bug. Os marcadores AAA não entram nessa conta: são estrutura, e não comentário (ver "Marcadores AAA" acima).

Vale também para `.HasComment()` de coluna: descreva o dado, não o histórico do modelo nem onde ele *era* calculado antes.
