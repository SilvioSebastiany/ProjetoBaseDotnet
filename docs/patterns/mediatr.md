# MediatR

## O que é

MediatR é a biblioteca usada para desacoplar o Controller do Handler que efetivamente executa a escrita. O Controller monta o Command e chama `mediator.Send(command)`; o MediatR encontra e executa o `IRequestHandler` correspondente.

## Por que usamos

Sem o Mediator, o Controller precisaria conhecer diretamente cada Handler (ou uma fábrica de Handlers), o que acopla a camada HTTP à implementação da regra de negócio. Com o MediatR, o Controller só conhece o Command (um objeto simples) e o contrato `IRequest<TResult>` — quem implementa a lógica pode mudar sem que o Controller precise ser alterado.

Importante: MediatR aqui é usado **somente para escrita** (Commands). Leitura não passa por ele — ver `cqrs-assimetrico.md`.

## Quando usar / quando não usar

Use para toda operação de escrita/alteração com regra de negócio — é o único canal permitido para isso.

Não use para leitura (isso reintroduz a simetria que o CQRS assimétrico deste projeto deliberadamente evita) e não use para orquestração entre múltiplos Handlers dentro do próprio domínio — se um Handler precisa de outro passo, chame o serviço/repositório diretamente, não encadeie `Send` dentro de um Handler.

## Exemplo no código

[`ClientesController.CriarAsync`](../../examples/clientes/11-Controller-ClientesController.cs) chama `mediator.Send(command, cancellationToken)`, que o MediatR roteia para [`CriarClienteCommandHandler`](../../examples/clientes/06-Handler-CriarClienteCommandHandler.cs). A ação `ListarAsync`, no mesmo Controller, não usa `IMediator` — vai direto ao repositório.
