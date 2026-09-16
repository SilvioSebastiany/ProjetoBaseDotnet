# Clean Architecture enxuta

## O que é

Uma versão simplificada da Clean Architecture: em vez das 4 camadas clássicas (Domain, Application, Infrastructure, Presentation), este projeto usa 3 camadas conceituais — **api**, **domain** e **infra** — sem uma camada `Application` separada. O `infra` é dividido em 3 projetos (`infra.data`, `infra.crosscutting`, `infra.crosscutting.ioc`) para isolar responsabilidades, mas o conceito continua sendo 3 camadas.

## Por que usamos

A camada `Application`, nos modelos clássicos, normalmente existe para orquestrar casos de uso — mas na prática, com CQRS, o "caso de uso" já É o Handler do Command (ou a própria ação de leitura no Controller, para leitura). Manter uma camada `Application` à parte significava, na prática, uma classe a mais que só repassava a chamada para o domínio, sem agregar nada. Removendo essa camada, o código fica mais direto de navegar: a regra de negócio de uma feature está inteira dentro de `domain/commands/handlers/<feature>/<acao>/`, sem pular entre projetos para entender o fluxo completo.

## Quando usar / quando não usar

Use esta estrutura para qualquer projeto novo com o mesmo perfil deste projeto-base: uma API .NET com regras de negócio de porte pequeno a médio, onde CQRS assimétrico (ver `cqrs-assimetrico.md`) já resolve a separação entre leitura e escrita.

Não force esta estrutura se o projeto tiver múltiplos bounded contexts complexos, integrações externas pesadas (filas, jobs, workers) ou exigir camadas adicionais como `infra.externals` — nesses casos, adapte a estrutura e documente a adaptação (como já é feito aqui: este projeto-base intencionalmente não tem `infra.externals`, `jobs` ou `robo` porque nenhum projeto até agora precisou).

## Exemplo no código

A feature `Cliente` em [`examples/clientes/`](../../examples/clientes/) atravessa as 3 camadas sem uma classe de orquestração `Application`: o Command (`03-Command-CriarClienteCommand.cs`) vai direto ao Handler (`06-Handler-CriarClienteCommandHandler.cs`), que já é o caso de uso.
