# Dependency Injection (composition root)

## O que é

Todo o registro de dependências (DbContext, repositórios, MediatR, validators) acontece em um único arquivo: `infra.crosscutting.ioc/dependencyinjection.cs`. É o composition root do projeto — o único lugar que referencia todos os outros projetos.

## Por que usamos

Concentrar a composição em um arquivo (dividido em métodos de extensão conforme cresce, mas ainda um arquivo) evita que o registro de dependências fique espalhado entre `Program.cs` e vários outros pontos do código, o que dificultaria saber, num relance, tudo que está registrado no container de DI.

## Quando usar / quando não usar

Toda nova interface/implementação (repositório, serviço, provider) é registrada em `dependencyinjection.cs`, nunca diretamente em `Program.cs` da API.

Se o arquivo crescer demais, divida em métodos de extensão dentro do próprio arquivo (`AddRepositories()`, `AddMediatR()`, etc.) — evite, por enquanto, dividir em múltiplos arquivos por responsabilidade; isso só deve ser reconsiderado se o único arquivo se tornar realmente difícil de navegar.

## Exemplo no código

TODO — preencher quando a feature de exemplo real for portada para `examples/`.
