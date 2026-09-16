# Testes E2E: WebApplicationFactory

## O que é

Testes que sobem a API inteira em memória (`WebApplicationFactory<Program>`) e batem nela via `HttpClient` real — passando por roteamento, model binding, pipeline do MediatR, FluentValidation, Handler e repositório de verdade. Diferente do teste unitário (`docs/patterns/testes-xunit-moq-autofixture-shouldly.md`), aqui nada é mockado: o teste usa o mesmo Postgres configurado em `appsettings.json`.

## Por que usamos

Teste unitário de Handler com repositório mockado garante que a lógica de negócio está certa, mas não garante que a rota, o binding do JSON, o pipeline de validação do MediatR e o mapeamento do EF Core (Fluent API, snake_case) realmente funcionam encaixados. O E2E cobre exatamente essa junção — é o teste que pega erro de rota (`CreatedAtAction` apontando pra ação errada), de binding, ou de mapeamento que nenhum teste unitário isolado detectaria.

## Por que aponta para o Postgres já configurado (não um banco descartável)

Decisão deliberada deste projeto-base: manter simples e sem dependência de Docker para rodar `dotnet test`. O custo é real — os testes escrevem no mesmo banco de desenvolvimento — por isso todo teste que grava dado é responsável por limpar o que criou (ver exemplo abaixo). Se o projeto crescer e isso virar um problema (testes cada vez mais lentos, conflito de dados em CI paralelo), a evolução natural é trocar por Testcontainers — mas não adiante essa complexidade sem a dor que a justifique.

## Quando usar / quando não usar

Use E2E para cobrir o caminho HTTP completo de uma feature nova (pelo menos o caminho feliz e o de validação/`422`) — não para testar variações de regra de negócio, que já são cobertas nos testes unitários do Handler.

Não duplique aqui toda a matriz de casos que já está no teste unitário do Handler — o E2E existe para garantir que a "cola" funciona, não para re-testar lógica de negócio.

Sempre limpe o dado criado (via `DbContext` direto, não via HTTP) ao final do teste — nunca deixe teste E2E sujar o banco de desenvolvimento.

## Exemplo no código

- Factory: `tests/BaseDotnet.Api.Tests/Infrastructure/ApiWebApplicationFactory.cs`
- Teste: `tests/BaseDotnet.Api.Tests/Clientes/CriarClienteEndpointTests.cs` — cobre `POST /clientes` no caminho feliz (`201` + persistência real) e no caminho de validação (`422` via `NotificationContext`), com limpeza do registro criado no `finally`.
