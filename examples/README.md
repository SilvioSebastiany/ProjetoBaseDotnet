# Examples

## clientes/

Primeira feature real portada para cá como exemplo canônico de implementação: `Cliente`, cobrindo os dois lados do CQRS assimétrico.

- **Escrita (Command):** `CriarCliente` — Command, Validator, Result, Handler.
- **Leitura (Query):** `ListarClientes` — ação de leitura no mesmo Controller, sem Mediator, sem Handler.
- **Persistência:** EF Core Configuration e Repository.
- **API:** Controller único (`ClientesController`) com as duas ações, Response e Mapper.
- **Testes:** unitário do Handler (xUnit + Moq + AutoFixture + Shouldly) e E2E dos dois endpoints (`WebApplicationFactory`).
- **Composition root:** trecho do `dependencyinjection.cs` com o registro de `IClienteRepository`.

Os arquivos em `examples/clientes/` são cópias de leitura do código real — cada um tem um comentário no topo apontando o arquivo fonte em `src/` ou `tests/`. Não edite os arquivos aqui: edite o original e re-copie.

Os "TODO — preencher quando a feature de exemplo real for portada para `examples/`" em cada `docs/patterns/*.md` foram substituídos pelo trecho de código correspondente, com link para os arquivos aqui.

Nenhuma feature fictícia (tipo "Produto") foi criada só para preencher este espaço — `clientes/` é código real, exercitado pelos testes da solução.
