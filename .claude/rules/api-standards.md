---
paths:
  - "src/api/**"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# API Standards

> Leia este arquivo antes de criar/alterar: Controller, rota, status code, Response/ResponseMapper, Swagger.

Este documento define padrões para desenvolvimento de APIs REST,
complementando o CLAUDE.md (raiz e `.claude/CLAUDE.md`).

---

## 1. Estrutura de URLs

- Não usar prefixo `/api` nas rotas.
- Recursos no plural:
  - `/usuarios`, `/produtos`, `/pedidos`
- Nomes compostos em **kebab-case**:
  - `/motor-de-regras`, `/tipos-documento`, `/notas-fiscais`
- Sub-recursos quando fizer sentido:
  - `/pedidos/{id}/itens`
- Ações específicas com verbo no final:
  - `/usuarios/{id}/confirmar-email`
  - `/pedidos/{id}/aprovar`

**NUNCA** usar camelCase, PascalCase ou snake_case em URLs.

---

## 2. HTTP Verbs

- `GET` – Leitura, idempotente, sem side effects.
- `POST` – Criação ou ação não idempotente.
- `PUT` – Atualização completa do recurso.
- `PATCH` – Atualização parcial.
- `DELETE` – Exclusão de recurso.

---

## 3. Status Codes Padrão

- `200 OK` – Sucesso em GET/PUT/PATCH com retorno.
- `201 Created` – Sucesso em POST com criação de recurso.
- `204 No Content` – Sucesso sem corpo de resposta (DELETE ou ação).
- `400 Bad Request` – Parâmetros inválidos, JSON malformado.
- `401 Unauthorized` – Não autenticado.
- `403 Forbidden` – Autenticado sem permissão.
- `404 Not Found` – Recurso não encontrado.
- `409 Conflict` – Operação com a mesma chave de idempotência ainda em andamento (`idempotency.md`).
- `422 Unprocessable Entity` – Regra de negócio/validação de domínio falhou.
- `500 Internal Server Error` – Erro técnico inesperado.
- `503 Service Unavailable` – Um sistema de que a rota depende não respondeu (`external-services.md` § 6).

Em geral, **422** deve ser usado em conjunto com NotificationContext.

Repetição de uma operação idempotente (mesma chave, mesmo conteúdo) responde **o mesmo da primeira
vez**, com o mesmo status, em qualquer situação da operação (`idempotency.md` § 3).

---

## 4. Controllers "Thin"

Controllers devem:

- Herdar de `ControllerBase` ou `CustomControllerBase`.
- **USAR construtores primarios** para injecao de dependencias.
- Injetar `IMediator` (para Commands).
- **NAO injetar INotificationContext** - tratamento de notificacoes por filtros globais.
- **USAR Mappers** para converter Entity para Response.
- **USAR `ActionResult<T>`** como retorno.
- Nao conter logica de negocio.
- Focar em:
  - Roteamento
  - Binding (query/route/body)
  - Status code adequado
  - Documentacao (XML comments)

### 4.1. Documentacao Swagger (OBRIGATORIO em Controllers)

- **USAR comentarios XML `/// <summary>`** em Controllers para Swagger — no projeto de API é o
  único lugar que leva. As demais exceções do padrão (Job, teste, propriedade calculada, contrato
  público de pacote) estão em `coding-standards.md`, seção "Comentários"; nenhuma delas é Controller
- **USAR `StatusCodes.StatusXxx`** nos atributos `[ProducesResponseType]`
- **USAR `typeof(ProblemDetails)`** (`Microsoft.AspNetCore.Mvc`) para status codes de erro, e
  `typeof(ValidationProblemDetails)` no 422: é o formato da RFC 9457, que o ASP.NET Core já gera

### 4.2. Exemplo Completo de Controller

```csharp
using Faturas.Api.Controllers.Base;
using Faturas.Api.Models.Mappers;
using Faturas.Api.Models.Responses;
using Faturas.Domain.Commands.Faturas.Adicionar;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Faturas.Api.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de faturas
    /// </summary>
    [ApiController]
    [Route("faturas")]
    public class FaturasController(IMediator mediator) : CustomControllerBase
    {
        /// <summary>
        /// Cria uma nova fatura
        /// </summary>
        /// <param name="command">Dados para criacao da fatura</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Fatura criada</returns>
        [HttpPost]
        [Authorize(Roles = PerfisUsuario.Financeiro)]
        [ProducesResponseType(typeof(AdicionarFaturaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AdicionarFaturaResponse>> AdicionarAsync(
            [FromBody] AdicionarFaturaCommand command,
            CancellationToken cancellationToken)
        {
            return Created(AdicionarFaturaResponseMapper.Map(await mediator.Send(command, cancellationToken)));
        }
    }
}
```

### 4.3. Leituras Simples via Repository (GET by ID)

> **IMPORTANTE**: Para operacoes de leitura simples sem logica de negocio,
> pode-se injetar o repositorio diretamente via `[FromServices]`.

**Quando usar Repository direto:**

- GET por ID (ex: GET /entidades/{id})
- Operacoes de leitura sem logica de negocio
- Quando nao ha validacoes alem da existencia

**Quando usar Command/Handler:**

- Operacoes de escrita (POST, PUT, DELETE)
- Listagens com filtros e paginacao complexos
- Operacoes com logica de negocio ou validacoes
- Operacoes que chamam servicos externos

**Exemplo com [FromServices]:**

```csharp
[HttpGet("{id}")]
[ProducesResponseType(typeof(ObterEntidadeResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<ActionResult<ObterEntidadeResponse>> ObterAsync(
    [FromRoute] string id,
    [FromServices] IEntidadeRepository repository,
    CancellationToken cancellationToken)
{
    return Ok(ObterEntidadeResponseMapper.Map(
        await repository.ObterAsync(id, cancellationToken)));
}
```

### 4.4. Endpoints Auxiliares com SDK Externo

Para endpoints auxiliares que consomem SDKs externos diretamente
(ex: consulta de CEP via servico de endereco), pode-se injetar o
servico diretamente via `[FromServices]`, sem necessidade de Command/Handler.

```csharp
[HttpGet("cep/{cep}")]
public async Task<ActionResult<EnderecoResponse>> ConsultarCepAsync(
    [FromRoute] string cep,
    [FromServices] IEnderecosService enderecosService)
{
    return Ok(EnderecoResponseMapper.Map(await enderecosService.BuscarEnderecoPorCEPAsync(cep)));
}
```

---

## 5. Consolidacao de Controllers por Agregado

> **IMPORTANTE**: Endpoints de sub-recursos devem ser consolidados no controller do agregado raiz.

**Padrao Consolidado:**

- Todas as operacoes relacionadas a uma entidade agregado ficam em um unico controller
- Isso inclui: sub-recursos, acoes e operacoes relacionadas
- NAO criar controllers separados para sub-recursos

**Exemplo de rotas consolidadas:**

```
POST   /pedidos                              - Criar pedido
GET    /pedidos                              - Listar pedidos
GET    /pedidos/{id}                         - Obter detalhes
POST   /pedidos/{id}/aprovar                 - Aprovar pedido
POST   /pedidos/{id}/itens                   - Adicionar item
PUT    /pedidos/{id}/itens/{itemId}          - Atualizar item
DELETE /pedidos/{id}/itens/{itemId}          - Remover item
POST   /pedidos/{id}/documentos              - Adicionar documento
GET    /pedidos/{id}/documentos/{documentoId} - Obter documento
```

**Rationale:** Mantem coesao ao agrupar todas operacoes do agregado em um unico controller.

---

## 6. Contratos JSON

- JSON é o formato padrão.
- Propriedades em **camelCase**.
- Usar DTOs para controlar o shape da API (não expor Entities diretamente).
- Paginação via `PagedResponse<T>` do projeto quando aplicável (`validation-and-cqrs.md` § 9).

### 6.1. Toda resposta com corpo é um objeto

Nunca array nem valor solto no topo do JSON. Campo novo entra num objeto sem quebrar quem já
consome; num array, não há onde pôr o total, o aviso ou o próximo cursor.

- **Lista sem paginação:** `ListaResponse<T>` do projeto, em `Api/Models/Responses/Base/`, que sai
  como `{ "itens": [...] }`:

  ```csharp
  public class ListaResponse<T>
  {
      public IReadOnlyCollection<T> Itens { get; }

      public ListaResponse(IEnumerable<T> itens)
      {
          Itens = [.. itens];
      }
  }
  ```
- **Lista que precisa de mais campos:** herda dela.
- **Valor único** (um total, um booleano): um response com o campo nomeado.

```csharp
[HttpGet]
[ProducesResponseType(typeof(ListaResponse<CobrancaResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public async Task<ActionResult<ListaResponse<CobrancaResponse>>> ListarAsync(
    [FromQuery] ListarCobrancasCommand command,
    CancellationToken cancellationToken)
{
    var cobrancas = await mediator.Send(command, cancellationToken);

    return Ok(new ListaResponse<CobrancaResponse>(cobrancas.Select(CobrancaResponseMapper.Map)));
}
```

Com campo a mais, o response herda da lista:

```csharp
public class ListarCobrancasResponse : ListaResponse<CobrancaResponse>
{
    public decimal ValorTotal { get; set; }
}
```

**Vale para rota nova.** Rota existente que devolve array continua assim: trocar é mudar o contrato
HTTP, o que a refatoração não faz (`domain-modeling.md` § 10). Se for trocar, é um PR próprio, depois
de avaliar quem consome.

---

## 7. Documentação com Swagger

### 7.1. Configuracao Basica

- Utilizar Swashbuckle.
- O template do .NET 9 em diante **não traz mais** o Swashbuckle: o padrão passou a ser
  `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`/`MapOpenApi`). Swashbuckle continua funcionando,
  mas agora entra como pacote explícito — se o Swagger sumiu num serviço novo, é isto.
- Habilitar XML comments no `.csproj` e no Swagger.
- Anotar endpoints com:
  - `/// <summary>...</summary>`
  - `[ProducesResponseType]`

### 7.2. Configuracao do Swagger com JWT Bearer

```csharp
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "1.0.0",
        Title = "Nome da API",
        Description = "Descricao da API.",
        Contact = new OpenApiContact
        {
            Name = "Equipe de Desenvolvimento",
            Email = "dev@empresa.com.br"
        }
    });

    var bearerTokenSecurityScheme = new OpenApiSecurityScheme
    {
        Description = "Header de autorizacao JWT usando esquema Bearer. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        Scheme = "bearer",       // IMPORTANTE: lowercase
        BearerFormat = "JWT",
        Type = SecuritySchemeType.Http,
        In = ParameterLocation.Header
    };

    options.AddSecurityDefinition("Bearer", bearerTokenSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});
```

---

## 8. Mappers e Responses Organizados por Entidade

Responses e Mappers devem ser organizados em subpastas por entidade:

```
Api/Models/
├── Mappers/
│   ├── Base/
│   ├── Pedidos/
│   │   ├── AdicionarPedidoResponseMapper.cs
│   │   └── ListarPedidosResponseMapper.cs
│   └── Produtos/
│       └── ProdutoResponseMapper.cs
└── Responses/
    ├── Base/
    │   └── ExportacaoResponse.cs
    ├── Pedidos/
    │   ├── AdicionarPedidoResponse.cs
    │   ├── ListarPedidoResponse.cs
    │   └── ObterPedidoResponse.cs
    └── Produtos/
        └── ProdutoResponse.cs
```

### 8.1. Uso de GetDescription() para Enums em Responses

> **IMPORTANTE**: Usar extensao `GetDescription()` para expor
> descricoes amigaveis de enums na API.

```csharp
// No Response, incluir campo {Enum}Descricao junto ao enum
public EStatusPedido Status { get; set; }
public string StatusDescricao { get; set; }

// No Mapper
Status = pedido.Status,
StatusDescricao = pedido.Status.GetDescription(),
```

---

## 9. Boas Práticas

- Manter o número de actions por controller coerente com o agregado de domínio.
- Evitar controllers "god" com muitas responsabilidades.
- Evitar retornar tipos anônimos — usar DTOs estáveis.
- Evitar mudanças quebra-contrato (breaking changes) em APIs públicas.
