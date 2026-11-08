> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Padrões .NET (genérico, reaproveitável entre projetos)

> Origem: `ferramental-engenharia/dados/herval-padrao`, adaptado sem os pacotes da casa (ferramental-dev, decisão 0007).

Padrões para serviços .NET em Clean Architecture + CQRS (MediatR). Este arquivo é genérico:
regras específicas do projeto (comandos, gabaritos, divergências) vivem no `CLAUDE.md` da raiz.
Regras detalhadas por tema em `.claude/rules/` do repo — leia o arquivo do tema ANTES de criar/alterar algo dele.

## Arquitetura (camadas)

- Fluxo de dependências obrigatório: `Services/Api → IoC → Domain ← Infra`. **Domain NUNCA referencia Infra ou Services** — expõe apenas contratos (`IRepository`, `IService`, `IUnitOfWork`); Infra implementa; o IoC conecta.
- Domínio rico, e como fazer (`.claude/rules/domain-modeling.md`): toda escrita é uma **ação nomeada na entidade**, que confere todas as pré-condições, notifica e não muda nada se falhar. O handler busca o que a regra precisa, passa por parâmetro, chama a ação, propaga `AddNotifications(entidade)` e salva — `if` de regra no handler é regra fora do lugar.
- A raiz do agregado orquestra as filhas: filha muda só pela ação da raiz; repositório só da raiz; dado de outro agregado entra por parâmetro.
- O código vizinho não é o padrão: antes de copiar um molde, confira a regra escrita e o mapa de moldes do repositório.
- Princípios de qualidade: SOLID, DRY, KISS e YAGNI — revisões de código validam aderência a este arquivo e às rules.

## Erros (Notification Context)

- NUNCA usar `throw` para **falha de negócio/validação/técnica** — vai para `notificationContext.AddNotification(...)` + `return default`. **Única exceção: erro de programação** (enum sem mapeamento, `switch` não implementado, estado impossível) → `throw` de `NotSupportedException` (ou `UnreachableException`, para estado impossível, do .NET 7 em diante) com a mensagem dizendo o que faltou, sem `// padrao:ok`; notificar ali obrigaria o chamador a *adivinhar* a causa, e cair num `default` silencioso é pior. Trata-se no limite que sabe reportar, logando a mensagem da própria exception: na **API** o `CustomExceptionFilter` global já cobre (não pôr `try/catch` em Controller/Handler); no **Worker**, no `try/catch` por item com `logger.LogError(ex, ...)`. Detalhes: `.claude/rules/validation-and-cqrs.md`.
- NUNCA `return null` nem `return Unit.Value` — sempre `return default`.
- `catch { }` mudo é defeito: toda captura loga a exceção com a mensagem dela, ou relança.
- Falha de regra de negócio na API = 422 (filtro global; NÃO injetar `INotificationContext` em Controllers).

## Tipos e classes

- NUNCA `record` — sempre `class` para DTOs, Requests, Responses, Commands, CommandResults.
- `<Nullable>disable</Nullable>`; `?` só em value types (`DateTime?`, `int?`, enums); nunca em `string`/classes; nunca `= null!`.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` em todo projeto, de `src` e de teste (no `Directory.Build.props` da raiz; com um projeto só, no csproj): aviso do compilador, de analisador e do NuGet (vulnerabilidade inclusive) trava o build e a publicação. A saída é corrigir; supressão só no ponto, com justificativa, nunca `NoWarn` no projeto (`coding-standards.md` § 9).
- Repositório com mais de um projeto tem na raiz `Directory.Build.props` (o comum a todos; o `TargetFramework` fica no csproj), `Directory.Packages.props` (`ManagePackageVersionsCentrally`; `PackageReference` sem versão) e `nuget.config` com o nuget.org como origem única, declarada (`<clear />` antes). No pipeline, o restore usa esse `nuget.config`, sem `--source` (`coding-standards.md` § 10).
- **Comentários — o padrão é NÃO comentar.** XML doc (`/// <summary>`) SEMPRE em Controllers (OpenAPI), nos Jobs do Worker e em todo membro público de pacote NuGet publicado, onde o csproj trava (`coding-standards.md`, "Comentários"). Fora deles, comentário é **exceção** que só se justifica quando o código é genuinamente não óbvio (armadilha real que faria um dev competente errar) — **na dúvida, não escreva**. Não se justifica por "foi decisão nossa", "o contexto está noutro lugar" ou "ajuda quem mexer depois": quem ajuda é **nome melhor e teste** (que não envelhece em silêncio). NUNCA descrever o que a linha faz, reafirmar regra de negócio, citar documento (`US-x`, `§x`, `RN-x`), fase do planejamento ("Fatia B", "Fase 2", "FIX 1"), outro projeto/serviço ou o fluxo de outra classe — quem lê abriu só este arquivo. **Em review, comentário solto é defeito** (mesmo peso de código morto): remove-se, não se reescreve. Nunca usar `#region`. Exemplos calibrados: `.claude/rules/coding-standards.md`.
- Primary constructors SÓ em infraestrutura (Controllers, Repositories, Handlers, DbContext, UoW, Workers, Services); NUNCA em Entities, ValueObjects, DTOs, Commands.
- Um tipo por arquivo (nome do arquivo = nome do tipo); `using` no topo; `var` em variáveis locais.
- Comparação com constante usa pattern (`is null`, `is not null`, `is A or B`, `is >= 0 and <= 60`). `==`, `||` e `&&` ficam entre duas expressões e dentro de consulta do EF, que não aceita pattern (`coding-standards.md` § 1.6).
- Coleções inicializadas com `[]`.
- Data sem hora é `DateOnly`; instante é `DateTime` em UTC (`DateTime.UtcNow`), que o Npgsql exige na coluna `timestamp with time zone` (`database-standards.md` § 3).
- Coleções em Entities: `List<T>` privada + `IReadOnlyCollection<T> => _lista.AsReadOnly()`; a mudança entra por ação da raiz (`coding-standards.md` § 6).

## CQRS (MediatR)

- Leitura E escrita usam Command/CommandHandler — não existe Query/QueryHandler.
- Pasta: `Domain/Commands/{Entidade_Plural}/{Acao}/` com Command + Handler + Validator, cada um em arquivo separado.
- Validator SEMPRE em arquivo separado (FluentValidation); mensagens de validação/notificação em português **COM** acentuação natural (PT-BR) — é texto exibido ao usuário. Só **identificadores de código** (nomes de classe/propriedade) ficam sem acento.
- Mappers `internal static` com método `Map()`: `Domain/Mappers/` (Command → VO/Entity) e `Api/Models/Mappers/` (CommandResult/Entity → Response).
- Retorno do Handler: 1º) a **Entity** (mapeada no Mapper da API — camada anticorrupção); 2º) `{Acao}{Entidade}CommandResult` quando precisa de mais que a entidade; 3º) Result reaproveitado por 2+ commands da mesma área → pasta **`Base/`** do contexto com nome neutro de ação.
- Os tipos de base (`Validatable`, `TrackableEntity`, `ValueObject`, `NotificationContext`, `NotificationKeys`, o `.IsValid()` e os filtros da API) moram no próprio projeto, na forma de `.claude/rules/domain-foundation.md`; projeto que não os tem cria antes da primeira entidade.
- Id é gerado pelo `TrackableEntity` — não atribuir manualmente. Entities usam `AddNotification` para invariantes; o handler propaga com `notificationContext.AddNotifications(entidade)` + `return default`.
- Atualização: `IRequest<bool>` (`default` = não encontrado → 404; `true` → 204). Criação: retorna a Entity (→ 201).
- Parâmetro de rota no Command: método fluente `ComXxxId(id)` ou construtor.
- Reação a fato segue a caixa de ferramentas de eventos (`validation-and-cqrs.md` § 16): o que precisa ser atômico é fluxo explícito no handler, não evento; handler não chama `Publish`.

## API

- Rotas em kebab-case, recursos no plural, SEM prefixo `/api`.
- Controllers thin: herdam `CustomControllerBase`, primary constructor com `IMediator`, retornam `ActionResult<T>`, usam ResponseMapper; zero lógica de negócio.
- `[ProducesResponseType]` com `StatusCodes.StatusXxx` e `typeof(ProblemDetails)` para erros (`ValidationProblemDetails` no 422), no formato da RFC 9457.
- GET simples por id (sem regra de negócio) pode injetar repositório via `[FromServices]`, sem Command.
- Operação que cria documento em outro sistema ou move dinheiro tem chave de idempotência dos dois lados (`.claude/rules/idempotency.md`): `ChaveIdempotencia` com índice único; repetição devolve o resultado da primeira; 409 enquanto a primeira está em andamento; timeout é desconhecido, nunca falha; `POST` só retenta se o destino deduplica pela chave.
- Antes de escrever um external, procure o SDK oficial do sistema no nuget.org (`external-services.md` § 11): com SDK, use o SDK; external próprio só quando não há.
- External traduz a resposta num desfecho (`.claude/rules/external-services.md`): sucesso, não encontrado, recusado, indisponível ou falha técnica, pela tabela do § 1 num método só da Infra (`ClassificarDesfecho`). Consulta devolve `default` (o 404 de "não existe", sem notificar; o erro, notificado); dinheiro, documento e robô recebem desfecho de domínio. Indisponível na API é 503 (`NotificationKeys.SERVICE_UNAVAILABLE`, constante do projeto); 401/403 do parceiro é log de erro e indisponível, nunca 403 ao usuário; corpo cru e status só no log.
- Consolidar sub-recursos no controller do agregado raiz. JSON camelCase; nunca expor Entities (sempre Response).
- Resposta com corpo é sempre objeto, nunca array no topo: lista sem paginação é `ListaResponse<T>` do projeto (`"itens"`, `api-standards.md` § 6), herdada quando precisa de mais campos. Só em rota nova; a existente não muda de contrato por isso.
- Enums em Responses: par `Status` + `StatusDescricao` (via `GetDescription()`).

## Banco (PostgreSQL / EF Core)

- Fluent API (`IEntityTypeConfiguration<T>`); SEMPRE `HasMaxLength(n)` em string e `HasPrecision(p, s)` em decimal — nunca string sem tamanho nem decimal sem precisão; `text` só com motivo.
- `.HasComment()` obrigatório em TODAS as colunas (português sem acentos).
- Enums como string: `HasConversion<string>()` + `HasMaxLength(50)`; nunca `integer` nem enum nativo do PostgreSQL.
- Nomes em snake_case pela convenção global (`UseSnakeCaseNamingConvention()`): tabelas plural no `ToTable`, colunas derivadas da propriedade, FK `{entidade}_id`, índices `ix_{tabela}_{colunas}`; Id `uuid` gerado na entidade, sem sequence.
- ValueObjects: colunas com prefixo obrigatório; VO simples via `HasConversion`, complexo via `OwnsOne`.
- Auditoria obrigatória: `usuario_inclusao`, `data_inclusao`, `usuario_alteracao`, `data_alteracao`.
- Migrations: nome descritivo em português, uma mudança conceitual por migration.

## Async e datas

- Todo I/O é assíncrono; sufixo `Async`; `CancellationToken` recebido e propagado SEM valor default (exceção: a API pública de pacote NuGet publicado, com `= default` no último parâmetro; em método já publicado, a assinatura antiga fica como sobrecarga sem o token, `async-guidelines.md` § 4); nunca `CancellationToken.None`; evitar `.Result`/`.Wait()`.
- `.editorconfig` na raiz com `dotnet_diagnostic.CA2016.severity = warning`: com a trava de avisos, quem tem o token e não repassa não compila (`async-guidelines.md` § 4.2).
- Timezone: o banco guarda UTC; regra de calendário do negócio em horário local, com a conversão entre fusos explícita e num ponto só.

## Nomenclatura

- Domínio em português sem acentos (`Descricao`, `Preco`); termos de framework em inglês (Handler, Repository, Response).
- Interfaces com prefixo `I`; enums com prefixo `E`; constantes `UPPER_SNAKE_CASE`; campos privados `_camelCase`.
- Sufixos: `Command`, `Request`, `Response`, `CommandResult`.

## Reaproveitamento e IoC

- NUNCA criar utilitário/extensão/VO sem antes checar, nesta ordem, a BCL do .NET, os pacotes que o projeto já referencia e o que o próprio projeto já tem (`.claude/rules/extensions.md`, `.claude/rules/valueobjects.md`). Exemplos: `IsNullOrEmpty()` para coleções (uma extensão no CrossCutting), `Chunk` da BCL para lotes, `ToPagedAsync` do projeto para paginação (`validation-and-cqrs.md` § 9), VOs do domínio (Cnpj, Email, Dinheiro...).
- Toda dependência registrada no composition root `*.Infra.CrossCutting.Ioc` (Scoped para repos/DbContext/UoW/NotificationContext). Operação usada em 2+ handlers → Service nomeado pelo contexto (`PedidoService.GerarX`), que pode fazer I/O e **nunca salva**.
- Cache de estado em memória: **interface no Domain, implementação na Infra**, Singleton, premissa single-instance documentada; hidratação via agregação no banco (SUM/COUNT), nunca materializando eventos em massa.

## Testes

- xUnit + Moq; nome `Deve_{Resultado}_Quando_{Condicao}`; AAA com os marcadores `// Arrange`, `// Act` e `// Assert` (`// Act e Assert` quando o assert é a própria chamada), que não contam como comentário para a regra de comentários.
- Assertions com os `Assert` do próprio xUnit, sem biblioteca à parte; projeto que migra com Shouldly ou FluentAssertions troca no PR da migração (`testing-guidelines.md` § 1.1).
- FluentAssertions só em projeto que já o usa, e abaixo da 8 (da 8 em diante a licença é paga para uso comercial); migrar é o recomendado (`testing-guidelines.md` § 1.2).
- Testes seguem as MESMAS regras de produção: Nullable disable, sem `#region`, sem `?`/`null!`.
- Reflection para montar agregados (coleções privadas carregadas pelo EF) SOMENTE dentro de um builder central em `tests/.../Base/` — nunca no corpo do teste.
- Nunca depender do relógio/dia real: expectativas de períodos em andamento devem capar em `DateTime.UtcNow` ou derivar horários do instante atual.
