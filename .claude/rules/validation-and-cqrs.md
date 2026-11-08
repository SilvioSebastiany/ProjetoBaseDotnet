---
paths:
  - "src/domain/**"
  - "src/api/**"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Validation and CQRS

> Leia este arquivo antes de criar/alterar: Command, Handler, Validator, Mapper, Domain Event, CommandResult.

Detalha o uso de CQRS, MediatR, FluentValidation e NotificationContext.

---

## 1. CQRS + MediatR

> **IMPORTANTE**: O padrão NÃO usa Query/QueryHandler separado.
> Todas as operações (escrita e leitura) usam Command/CommandHandler.

### 1.1. Commands (Escrita)

**Estrutura de Pastas (OBRIGATÓRIA):**

```
Domain/Commands/{Entidade_Plural}/{Acao}/
├── {Acao}{Entidade}Command.cs
├── {Acao}{Entidade}CommandHandler.cs
└── {Acao}{Entidade}CommandValidator.cs   # ARQUIVO SEPARADO (OBRIGATORIO)
```

**Exemplo:**

```
Commands/Faturas/Adicionar/
├── AdicionarFaturaCommand.cs
├── AdicionarFaturaCommandHandler.cs
└── AdicionarFaturaCommandValidator.cs
```

**Regras Criticas:**

- ⚠️ **NUNCA colocar validacao dentro do Command ou Handler**
- ⚠️ **Validator SEMPRE em arquivo separado** usando FluentValidation
- Handler usa primary constructor para injecao de dependencias
- Handler cria a entidade pelo Mapper do Domain, que chama o construtor ou a fabrica estatica da entidade; handler nao faz `new` de entidade com regra (`domain-modeling.md` § 5)
- Handlers implementam `IRequestHandler<TCommand, TResponse>`

**Exemplo de Command:**

```csharp
using Faturas.Domain.Entities;
using MediatR;
using Faturas.Domain.ValueObjects;

namespace Faturas.Domain.Commands.Faturas.Adicionar
{
    public class AdicionarFaturaCommand : IRequest<Fatura>
    {
        public string Nome { get; set; }
        public Cnpj Cnpj { get; set; }
        public Email Email { get; set; }
        public Telefone Telefone { get; set; }
        public string Filial { get; set; }
        public string Categoria { get; set; }
    }
}
```

**Exemplo de Handler (com primary constructor):**

```csharp
using Faturas.Domain.Entities;
using Faturas.Domain.Interfaces.Repositories;
using Faturas.Domain.Mappers;
using MediatR;
using Faturas.Domain.Notifications;

namespace Faturas.Domain.Commands.Faturas.Adicionar
{
    public class AdicionarFaturaCommandHandler(
        IFaturaRepository faturaRepository,
        INotificationContext notificationContext,
        IUnitOfWork unitOfWork) : IRequestHandler<AdicionarFaturaCommand, Fatura>
    {
        public async Task<Fatura> Handle(
            AdicionarFaturaCommand request,
            CancellationToken cancellationToken)
        {
            // O Mapper do Domain monta o VO e chama o construtor da entidade, onde moram as invariantes de criação
            var fatura = FaturaMapper.Map(request);

            if (fatura.IsInvalid)
            {
                notificationContext.AddNotifications(fatura);
                return default;
            }

            await faturaRepository.AdicionarAsync(fatura, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return fatura;
        }
    }
}
```

### 1.2. Operacoes de Leitura (Mesmo Padrao Command)

**Estrutura de Pastas** (igual a Commands de escrita):

```
Domain/Commands/{Entidade_Plural}/{Acao}/
├── {Acao}{Entidade}Command.cs
├── {Acao}{Entidade}CommandHandler.cs
└── {Acao}{Entidade}CommandValidator.cs   # Se necessario
```

**Exemplo para Listagem:**

```
Commands/Faturas/Listar/
├── ListarFaturasCommand.cs
└── ListarFaturasCommandHandler.cs
```

**Exemplo de Command de Leitura:**

```csharp
using Faturas.Domain.Pagination;
using MediatR;

namespace Faturas.Domain.Commands.Faturas.Listar
{
    public class ListarFaturasCommand : PagedRequest, IRequest<PagedResponse<FaturaListItemResponse>>
    {
        public string Nome { get; set; }
        public ESituacaoFatura? Situacao { get; set; }  // enum nullable permitido
    }
}
```

Paginacao via `PagedRequest`/`PagedResponse<T>` do projeto quando necessario (§ 9).

---

## 2. FluentValidation para Commands

- Todo Command deve ter um `AbstractValidator<TCommand>`.
- **Validator SEMPRE em arquivo separado** (nunca dentro do Command ou Handler)
- Validators sao registrados via `AddValidatorsFromAssembly`.
- Execucao ocorre em pipeline behavior antes do handler.
- Usar a extensao `.IsValid()` do projeto para ValueObjects (`domain-foundation.md` § 4).

**Exemplo de Validator (arquivo separado):**

```csharp
using FluentValidation;
using Faturas.Domain.Notifications;

namespace Faturas.Domain.Commands.Faturas.Adicionar
{
    public class AdicionarFaturaCommandValidator : AbstractValidator<AdicionarFaturaCommand>
    {
        public AdicionarFaturaCommandValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty();

            RuleFor(x => x.Cnpj)
                .IsValid();

            RuleFor(x => x.Email)
                .IsValid();

            RuleFor(x => x.Filial)
                .NotEmpty();
        }
    }
}
```

**Mensagens:** sempre em português **com acentuação natural (PT-BR)** — são texto exibido ao usuário (ex.: "Documento inválido", "O pedido já foi aprovado"). Apenas os **identificadores de código** (nomes de classe/propriedade/enum) ficam sem acento.

---

## 3. NotificationContext

- Interface centralizada para coletar erros de negocio e tecnicos.
- **Substitui COMPLETAMENTE o uso de excecoes** em todo o codigo.
- Handlers consultam `notificationContext.HasNotifications`.

Principios:

- **NUNCA usar `throw` de excecoes** em nenhuma situacao.
- **NUNCA usar `throw new Exception()`**, `throw new ValidationException()`, etc.
- **TODAS as falhas** (negocio, validacao, tecnicas) devem usar `notificationContext.AddNotification()`.
- Handlers e Services devem adicionar notificacoes e retornar `default` em caso de erro.
- Controllers retornam `422 Unprocessable Entity` quando houver notificacoes. A excecao e a notificacao com chave do `NotificationKeys`, que o filtro converte em 403, 409 ou 503 (`external-services.md` § 6).

Exemplo correto:

```csharp
// CORRETO - falha de negócio vai para o NotificationContext
if (pedido.Situacao is ESituacaoPedido.Cancelado)
{
    notificationContext.AddNotification(nameof(Pedido.Situacao), "Pedido cancelado não pode ser faturado.");
    return default;
}
```

**"Não encontrado" não é notificação:** o handler devolve `default` sem notificar, e o controller
responde 404 (§ 12.1). Com notificação, o filtro global responde com erro (422, ou o status da chave
do `NotificationKeys`) e o `NotFound()` do controller nunca é alcançado.

Exemplo incorreto:

```csharp
// INCORRETO - NUNCA fazer isso
if (entidade is null)
{
    throw new NotFoundException("Entidade nao encontrada");
}

// INCORRETO - NUNCA fazer isso
throw new ValidationException("Campo invalido");
throw new BusinessException("Regra violada");
throw new Exception("Erro generico");
```

### Única exceção: ERRO DE PROGRAMAÇÃO (decisão 2026-07-27)

A regra acima vale para **falha de negócio** — algo que o usuário ou o dado causou, e que o sistema sabe explicar. **Erro de programação é diferente:** um valor de enum sem mapeamento, um caso de `switch` que ninguém implementou, um estado impossível. Aí **estoure exception**, com a mensagem dizendo exatamente o que faltou:

```csharp
_ => throw new NotSupportedException($"Meio de pagamento {meioPagamento} nao implementado na cobranca do Hub de Pagamentos")
```

**O tipo diz que é erro de programação:**

- **valor sem tratamento** (enum, desfecho, chave sem estratégia): `NotSupportedException`;
- **estado impossível**, o ramo que nenhum dado válido alcança: `UnreachableException`
  (`System.Diagnostics`);

Lançar um desses tipos é seguir a regra, e não desviar dela: a linha não leva `// padrao:ok`, e a
revisão não a aponta. Os outros tipos continuam apontados, porque `InvalidOperationException` e
`Exception` são justamente os que se usam para regra de negócio.

**Por que não notificar:** quem chamou não tem como saber a causa e acabaria **adivinhando** a mensagem ("meio de pagamento não suportado"), enquanto a exception carrega o valor exato que faltou. Um `default` silencioso (cair num valor qualquer do enum) é pior ainda: vira bug estranho meses depois.

**Onde tratar** — no limite que sabe reportar, sempre logando **a mensagem da própria exception**, nunca uma inventada:

- **API**: já resolvido pelo `CustomExceptionFilter` global (`Startup`) — não colocar `try/catch` em Controller nem em Handler.
- **Worker**: no `try/catch` por item do laço, com `logger.LogError(ex, ...)` — um item quebrado não derruba a rotina nem contamina o próximo.
- **Reação a evento e integração auxiliar** (§ 16, ferramentas 3 e 5; `external-services.md` § 8): o `try/catch` fica dentro da própria reação, com log, e não relança — o fato já foi gravado, e a falha da reação não muda a resposta nem impede as outras.

Não use exception para regra de negócio, e não use `AddNotification` para erro de programação.

---

## 4. Validacao no Dominio

### 4.1. Value Objects

- Herdam `ValueObject` (`valueobjects.md`).
- **VO com regra** (formato, faixa, obrigatório) se valida a si mesmo, no `Validate()`, e entra no
  validator da entidade com `.IsValid()` (§ 14). Fora dele, a notificação do VO é engolida.
- **VO que só agrupa campos** de uma seção (§ 8) não precisa de validação própria; quando ganhar
  regra, ganha o `Validate()` e a linha no validator da raiz.

### 4.2. Entities

- Toda escrita é uma ação nomeada na entidade, que confere todas as pré-condições, notifica e não
  muda nada se falhar. O como está em `domain-modeling.md`.
- O dado que vale sempre (obrigatório, formato, faixa de negócio, filhas e VOs) fica no validator
  da entidade (§ 14). A pré-condição de estado fica na ação.

---

## 5. Domain Events

Evento é uma das ferramentas para reagir a um fato, e não a única. A triagem e a forma de cada
ferramenta estão no § 16.

---

## 6. Mappers

### 6.1. Domain Mappers

- **Localizacao:** `Domain/Mappers/`
- **Proposito:** Mapear Command -> ValueObject ou Entity
- **Padrao:** Classes `internal static` com metodo `Map()`

```csharp
namespace Faturas.Domain.Mappers
{
    internal static class FornecedorMapper
    {
        internal static Fornecedor Map(AdicionarFaturaCommand command)
        {
            if (command is null)
                return default;

            return new Fornecedor(
                command.Nome,
                command.Cnpj,
                command.Email,
                command.Telefone,
                command.Filial,
                command.Categoria);
        }
    }
}
```

### 6.2. API Response Mappers

- **Localizacao:** `Api/Models/Mappers/`
- **Proposito:** Mapear Entity/Result -> Response DTO
- **Padrao:** Classes `internal static` com metodo `Map()`

```csharp
namespace Faturas.Api.Models.Mappers
{
    internal static class AdicionarFaturaResponseMapper
    {
        internal static AdicionarFaturaResponse Map(Fatura response)
        {
            if (response is null)
                return default;

            return new AdicionarFaturaResponse
            {
                Id = response.Id
            };
        }
    }
}
```

---

## 7. Entidades e TrackableEntity

- **O Id e definido automaticamente** pelo `TrackableEntity` - NAO atribuir manualmente
- **Usar `AddNotification`** para capturar erros de regra de negocio
- Sempre verificar `IsValid` ou `IsInvalid` apos operacoes, e propagar com
  `notificationContext.AddNotifications(entidade)` + `return default` — sem isso o 422 sai sem mensagem

```csharp
public class Fatura : TrackableEntity
{
    public Fornecedor Fornecedor { get; private set; }
    public ESituacaoFatura Situacao { get; private set; }

    protected Fatura() { }

    public Fatura(Fornecedor fornecedor)
    {
        Fornecedor = fornecedor;
        Situacao = ESituacaoFatura.EmAndamento;
    }

    public void Cancelar(string motivo)
    {
        if (Situacao is ESituacaoFatura.Cancelado)
        {
            AddNotification("Situacao", "Fatura ja esta cancelada");
            return;
        }
        Situacao = ESituacaoFatura.Cancelado;
        MotivoCancelamento = motivo;
        DataCancelamento = DateTime.UtcNow;
    }
}
```

---

## 8. ValueObjects para Secoes

- **Usar ValueObjects para agrupar propriedades relacionadas**
- Cada secao logica da entidade deve ter seu proprio ValueObject
- O exemplo abaixo só agrupa: as regras de formato estão nos VOs de dentro (`Cnpj`, `Email`,
  `Telefone`). Se a seção ganhar regra própria, ela se valida como manda o § 4.1

```csharp
public class Fornecedor
{
    public string Nome { get; private set; }
    public Cnpj Cnpj { get; private set; }
    public Email Email { get; private set; }
    public Telefone Telefone { get; private set; }
    public string Filial { get; private set; }
    public string Categoria { get; private set; }

    private Fornecedor() { }

    public Fornecedor(string nome, Cnpj cnpj, Email email, Telefone telefone, string filial, string categoria)
    {
        Nome = nome;
        Cnpj = cnpj;
        Email = email;
        Telefone = telefone;
        Filial = filial;
        Categoria = categoria;
    }
}
```

---

## 9. Paginacao

Os tipos e o `ToPagedAsync` estão em `domain-foundation.md` § 5.

### 9.1. Interface do Repository

```csharp
using Faturas.Domain.Pagination;

Task<PagedResponse<Entidade>> ListarAsync(
    string filtro,
    IPaginationInfo paginationInfo,
    CancellationToken cancellationToken);
```

### 9.2. Implementacao do Repository

```csharp
using Faturas.Domain.Pagination;

public async Task<PagedResponse<Entidade>> ListarAsync(
    string filtro,
    IPaginationInfo paginationInfo,
    CancellationToken cancellationToken)
{
    return await _context.Entidades
        .Where(e => string.IsNullOrWhiteSpace(filtro) || e.Campo.Contains(filtro))
        .OrderByDescending(e => e.DataInclusao)
        .ToPagedAsync(paginationInfo, cancellationToken);
}
```

---

## 10. Commands com Parametros de Rota

Quando um Command recebe parametros que vem da rota (URL) E do corpo da requisicao:

### 10.1. Padrao 1: Parametro via Metodo Fluente

```csharp
public class AdicionarDocumentoCommand : IRequest<DocumentoResult>
{
    internal string EntidadeId { get; private set; }  // internal - nao exposto na API
    public ETipoDocumento TipoDocumento { get; set; }  // corpo da requisicao
    public string NomeArquivo { get; set; }
    public string Base64 { get; set; }

    public AdicionarDocumentoCommand ComEntidadeId(string entidadeId)
    {
        EntidadeId = entidadeId;
        return this;
    }
}

// Controller
[HttpPost]
public async Task<ActionResult<DocumentoResponse>> AdicionarAsync(
    [FromRoute] string entidadeId,
    [FromBody] AdicionarDocumentoCommand command,
    CancellationToken cancellationToken)
{
    return Created(DocumentoResponseMapper.Map(
        await mediator.Send(command.ComEntidadeId(entidadeId), cancellationToken)));
}
```

### 10.2. Padrao 2: Parametros via Construtor

```csharp
public class ObterDocumentoCommand : IRequest<DocumentoResult>
{
    public string EntidadeId { get; private set; }
    public string DocumentoId { get; private set; }

    public ObterDocumentoCommand(string entidadeId, string documentoId)
    {
        EntidadeId = entidadeId;
        DocumentoId = documentoId;
    }
}

// Controller - criar instancia diretamente
[HttpGet("{id}")]
public async Task<ActionResult<DocumentoResponse>> ObterAsync(
    [FromRoute] string entidadeId,
    [FromRoute] string id,
    CancellationToken cancellationToken)
{
    return Ok(DocumentoResponseMapper.Map(
        await mediator.Send(new ObterDocumentoCommand(entidadeId, id), cancellationToken)));
}
```

---

## 11. CommandResult para Operacoes Complexas

Quando o Handler precisa retornar dados que NAO sao a entidade diretamente
(ex: URL de servico externo, dados calculados):

```csharp
// Arquivo: Commands/Documentos/Obter/ObterDocumentoCommandResult.cs
public class ObterDocumentoCommandResult
{
    public string Id { get; set; }
    public string NomeArquivo { get; set; }
    public string Url { get; set; }  // URL temporaria de servico externo
}

// Handler retorna o Result em vez da Entity
public class ObterDocumentoCommandHandler : IRequestHandler<ObterDocumentoCommand, ObterDocumentoCommandResult>
{
    public async Task<ObterDocumentoCommandResult> Handle(...)
    {
        var documento = await repository.ObterAsync(...);
        var url = await servicoExterno.ObterUrlAsync(documento.Id);

        return new ObterDocumentoCommandResult
        {
            Id = documento.Id,
            NomeArquivo = documento.NomeArquivo,
            Url = url
        };
    }
}
```

---

## 12. Padroes de Retorno em Handlers

### 12.1. Operacoes de Atualizacao (bool)

Operacoes de atualizacao que podem falhar por entidade nao encontrada
devem usar `IRequest<bool>` para indicar sucesso/falha:

**Command:**

```csharp
public class AtualizarEntidadeCommand : IRequest<bool> { }
```

**Handler:**

```csharp
public class AtualizarEntidadeCommandHandler : IRequestHandler<AtualizarEntidadeCommand, bool>
{
    public async Task<bool> Handle(AtualizarEntidadeCommand request, CancellationToken cancellationToken)
    {
        var entidade = await repository.ObterAsync(request.EntidadeId, cancellationToken);
        if (entidade is null)
            return default;  // false - entidade nao encontrada

        entidade.AlterarPrazo(request.NovoPrazo);  // a ação nomeada pelo fato, com as regras dentro

        if (entidade.IsInvalid)
        {
            notificationContext.AddNotifications(entidade);
            return default;  // 422: a notificação vence o NotFound do controller
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);  // true se salvou
    }
}
```

**Controller:**

```csharp
[HttpPut("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<ActionResult> AtualizarAsync(
    [FromRoute] string id,
    [FromBody] AtualizarEntidadeCommand command,
    CancellationToken cancellationToken)
{
    return await mediator.Send(command.ComEntidadeId(id), cancellationToken)
        ? NoContent()      // 204 - sucesso
        : NotFound();      // 404 - entidade nao encontrada
}
```

### 12.2. Operacoes de Criacao (Entity)

Operacoes de criacao devem retornar a entidade criada para permitir
extracao do ID gerado:

```csharp
// Command
public class AdicionarEntidadeCommand : IRequest<Entidade> { }

// Handler retorna a entidade criada
public async Task<Entidade> Handle(...)
{
    var entidade = new Entidade(...);
    await repository.AdicionarAsync(entidade, ct);
    await unitOfWork.SaveChangesAsync(ct);
    return entidade;
}

// Controller
[HttpPost]
[ProducesResponseType(typeof(AdicionarEntidadeResponse), StatusCodes.Status201Created)]
public async Task<ActionResult<Response>> AdicionarAsync(...)
{
    return Created(ResponseMapper.Map(await mediator.Send(command, ct)));
}
```

---

## 13. Commands Base para Atualizacao

Para operacoes de atualizacao com multiplas secoes de dados, criar Commands base reutilizaveis:

**Estrutura de Pastas:**

```
Domain/Commands/{Entidade}/
├── Base/                              # Commands base reutilizaveis
│   ├── DadosPessoaisCommand.cs
│   ├── EnderecoCommand.cs
│   └── DadosBancariosCommand.cs
├── Atualizar/
│   ├── AtualizarEntidadeCommand.cs    # Usa os commands base
│   ├── AtualizarEntidadeCommandHandler.cs
│   └── AtualizarEntidadeCommandValidator.cs
```

**Padrao de Command Base:**

```csharp
public class DadosPessoaisCommand
{
    public string NomeCompleto { get; set; }
    public DateTime? DataNascimento { get; set; }
    public ESexo? Sexo { get; set; }           // Enum nullable
    public Email Email { get; set; }           // ValueObject
    public Telefone TelefonePrincipal { get; set; }
}
```

**Command Principal que Compoe os Base:**

```csharp
public class AtualizarEntidadeCommand : IRequest<bool>
{
    public string EntidadeId { get; private set; }

    public DadosPessoaisCommand DadosPessoais { get; set; }
    public EnderecoCommand Endereco { get; set; }
    public DadosBancariosCommand DadosBancarios { get; set; }

    public AtualizarEntidadeCommand ComEntidadeId(string entidadeId)
    {
        EntidadeId = entidadeId;
        return this;
    }
}
```

---

## 14. Validators Centralizados para Entidades

Validators de entidade ficam em `Domain/Validators/`. Cobrem o dado que vale sempre (obrigatório,
formato, faixa de negócio); a pré-condição de estado fica na ação (`domain-modeling.md` § 4).

**Quem chama:** a própria entidade, no `Validate()`, que roda a cada leitura de `IsValid`/`IsInvalid`:

```csharp
public override bool Validate() => UseValidator(this, new EntidadeValidator());
```

**Filha ou VO que notifica entra aqui**, por `.IsValid()` (`domain-foundation.md` § 4).
Fora do validator da raiz, a notificação da filha é engolida, e a raiz é salva como válida.
`SetValidator` serve para a seção que não se valida sozinha; filha ou VO que herda `Validatable`
entra por `.IsValid()`, que usa o `Validate()` dela.

**Estrutura:**

```
Domain/Validators/
├── EntidadeValidator.cs       # Validator principal (compoe os demais)
├── DadosPessoaisValidator.cs
├── EnderecoValidator.cs
└── DadosBancariosValidator.cs
```

**Validator Principal (Composicao):**

```csharp
using FluentValidation;

public class EntidadeValidator : AbstractValidator<Entidade>
{
    public EntidadeValidator()
    {
        RuleFor(x => x.DadosPessoais)
            .NotNull()
            .WithMessage("Dados pessoais são obrigatórios")
            .SetValidator(new DadosPessoaisValidator());

        RuleFor(x => x.Endereco)
            .NotNull()
            .WithMessage("Endereço é obrigatório")
            .IsValid();

        RuleForEach(x => x.Dependentes)
            .IsValid();
    }
}
```

---

## 15. Validacao de ValueObjects Opcionais em Commands

Para validar ValueObjects (Email, Telefone, Cnpj, Cep) que podem ser nulos no formulario,
usar `.Must(x => x is null || x.IsValid)`:

```csharp
public class AtualizarEntidadeCommandValidator : AbstractValidator<AtualizarEntidadeCommand>
{
    public AtualizarEntidadeCommandValidator()
    {
        // Enums nullable - validar apenas se preenchido
        RuleFor(x => x.DadosPessoais.Sexo)
            .IsInEnum()
            .When(x => x.DadosPessoais.Sexo.HasValue);

        // ValueObjects - validar IsValid apenas se nao for null
        RuleFor(x => x.DadosPessoais.Email)
            .Must(x => x is null || x.IsValid)
            .WithMessage("E-mail invalido");

        RuleFor(x => x.DadosPessoais.TelefonePrincipal)
            .Must(x => x is null || x.IsValid)
            .WithMessage("Telefone principal invalido");

        RuleFor(x => x.Endereco.Cep)
            .Must(x => x is null || x.IsValid)
            .WithMessage("CEP invalido");

        // Validacao de CPF em colecoes
        RuleForEach(x => x.Dependentes).ChildRules(dep =>
        {
            dep.RuleFor(d => d.Cpf)
                .Must(x => x is null || x.IsValid)
                .WithMessage("CPF do dependente invalido");
        });
    }
}
```

### 15.1. Validacao de Unicidade em Colecoes

```csharp
RuleFor(x => x.Dependentes)
    .Must(HaveUniqueCpfs)
    .WithMessage("CPFs dos dependentes devem ser unicos");

private static bool HaveUniqueCpfs(IEnumerable<DependenteCommand> dependentes)
{
    var cpfs = dependentes?.Select(d => d.Cpf?.ToString()).Where(c => !string.IsNullOrEmpty(c));
    return cpfs?.Count() == cpfs?.Distinct().Count();
}
```

---

## 16. Reação a fato: a caixa de ferramentas

Evento é uma ferramenta entre oito. Antes de escrever um, faça a triagem, nesta ordem:

1. **O fluxo só termina se B acontecer, ou precisa do resultado de B?** Não é evento: ferramenta 1
   ou 2.
2. **Aconteceu algo, e outras partes deste serviço reagem sem ser condição do fluxo?** Ferramenta 3.
3. **Outro sistema precisa saber, e a perda não é aceitável?** Ferramenta 4.
4. **Pode se perder sem ninguém sentir falta?** Ferramenta 5.

| # | Ferramenta | Quando | Como | Se falhar |
|---|---|---|---|---|
| 0 | A própria ação | muda o mesmo agregado: situação, filha, histórico | método da entidade, no mesmo `SaveChanges` | nada grava |
| 1 | Fluxo explícito | o fluxo só termina se B acontecer, no mesmo serviço e banco | o handler chama as ações (ou um Service por contexto; Strategy quando varia por tipo) e salva uma vez | nada grava: 422 na API; no Worker, o próximo ciclo |
| 2 | Chamada externa síncrona | a ação precisa do resultado para decidir | o handler chama o external e passa por parâmetro | a ação não acontece |
| 3 | Evento de domínio | fato com reações internas que não são condição do fluxo e que o produtor não precisa conhecer | a ação registra o fato; as reações rodam depois do `SaveChanges`, cada uma isolada | a reação vira log de erro e **não** muda a resposta nem desfaz o fato |
| 4 | Evento de integração | outro sistema precisa saber, e a perda não é aceitável | contrato próprio gravado numa tabela de saída **no mesmo commit do fato**; um processo em segundo plano envia, com retentativa e o `Id` da linha como chave de idempotência no destino | retenta; no máximo, erro visível |
| 5 | Descartável | métrica, telemetria, e-mail ou ticket informativo | ferramenta 3, e pode chamar sistema externo (`external-services.md` § 8) | log de aviso |
| 6 | Entrada | webhook, MQTT, broker | o adaptador traduz para Command (`Send`); deduplica pelo id do emissor; confirma depois de processar | erro visível, sem travar a fila |
| 7 | Rotina periódica | reconciliação, varredura | `BackgroundService` com prazo por ciclo, ou CronJob sem sobreposição; laço isolado por item | o próximo ciclo |

**O que precisa ser atômico não é evento.** Se a reação falhando tem de desfazer o fato, ela é
fluxo (ferramenta 1). E se a perda da reação não é aceitável, ela também não é ferramenta 3.

### 16.1. Evento de domínio (ferramenta 3)

- **É um fato nomeado no passado** (`ContratoFinalizadoEvent`), registrado **pela ação** da
  entidade. Não é registrado num setter, nem é um "status mudou" genérico: este obriga cada reação a
  refazer a máquina de estados da entidade.
- Carrega ids e valores, e não a entidade.
- **O handler não chama `Publish`.** Os eventos registrados pelas ações são publicados depois do
  `SaveChanges` bem-sucedido, pelo despacho do pipeline. Enquanto o repositório não tiver esse
  despacho, ele é publicado no fim do handler, a partir dos eventos da entidade, e nunca antes de
  salvar, com `// padrao:ok` na linha do `Publish`. Nesse caso, cada reação captura e loga a própria
  falha: o `Publish` padrão do MediatR (`ForeachAwaitPublisher`) é um laço sem `catch`, e a primeira reação que falha
  subiria até a resposta, depois do fato gravado, sem deixar as outras rodarem.
- **A reação:**
  - roda isolada: a falha é capturada e logada com o nome do evento e da reação;
  - não notifica o contexto do comando, porque o fato já foi gravado;
  - muda outro agregado só pela ação dele, e grava o que mudou: com o despacho do pipeline, ela roda
    no próprio escopo, e o despacho não salva;
  - não chama sistema externo: o que é para fora é a ferramenta 4, ou a 5 quando a perda é aceitável.
- **A reação de entrada** é a única exceção à regra de reação não chamar handler. É a reação que faz
  com uma observação de fora o que o webhook faria (ferramenta 6): o status consultado num sistema de
  fora vira o command de confirmar, expirar ou falhar. Vale só quando as três condições valem:
  1. o fato é uma observação de um sistema de fora, que a própria rotina volta a observar no ciclo
     seguinte: perder a reação custa só atraso. Não é um "status mudou" de dentro do serviço;
  2. ela é a única reação desse evento;
  3. depois do `Send`, que passa pelo pipeline, ela só trata o desfecho do mesmo agregado (contar a
     recusa, por exemplo).

  A linha do `Send` leva `// padrao:ok entrada: <o que ela traduz>`, porque o verificador não
  distingue a reação de entrada das outras.

```
Domain/Events/{Contexto}/
├── {Entidade}{Fato}Event.cs
└── Reacoes/{Reacao}EventHandler.cs
```

### 16.2. Evento de integração (ferramenta 4)

- **Contrato próprio, versionado**, e não o evento de domínio: quem está de fora não pode quebrar
  quando o domínio muda.
- **A linha de saída é gravada no mesmo `SaveChanges` do fato.** Gravar depois do commit, ou num
  `SaveChanges` à parte, reabre a janela "salvou e caiu antes de avisar".
- **O envio:**
  - é feito por um processo em segundo plano;
  - reivindica cada linha de forma atômica, porque a API roda em mais de uma réplica;
  - retenta com espera crescente e tem um máximo;
  - no máximo, vira erro visível;
  - manda o `Id` da linha como chave de idempotência, porque a entrega é "pelo menos uma vez".

---

## 17. Retorno de Commands e CommandResults compartilhados (pasta Base)

1. **Padrão primário**: o Handler retorna a **Entity**, e o **Mapper da API** (camada anticorrupção) converte para Response. Nunca expor a Entity na API.
2. **Quando o retorno precisa de mais informações além da entidade em foco** (dados calculados, agregações, composições), usar um **`{Acao}{Entidade}CommandResult`**.
3. **Quando o mesmo Result é reaproveitado por mais de um command da mesma área**, movê-lo para uma pasta **`Base/`** dentro do contexto (ex.: `Domain/Commands/Pedidos/Base/`) com **nome neutro de ação** (ex.: `ResumoPedidoCommandResult`, não `ConsultarResumoPedidoItemCommandResult`).
