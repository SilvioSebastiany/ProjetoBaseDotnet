# FluentValidation + NotificationContext

## O que é

Duas peças que trabalham juntas na validação de Commands:
- **FluentValidation**: define, de forma declarativa, as regras de validação de um Command (campo obrigatório, tamanho máximo, valor mínimo etc.) em uma classe `Validator` própria.
- **NotificationContext**: um objeto injetado no Handler que acumula erros de negócio (`AddNotification`) em vez de lançar exceção. No final do Handler, se houver notificações, a API responde `422` com a lista de erros.

## Por que usamos

Erro de negócio esperado (ex: "Projeto informado não existe", "título é obrigatório") não é uma situação excepcional — é parte do fluxo normal da aplicação. Usar `throw` para isso tornaria o controle de fluxo mais caro e a resposta ao cliente menos previsível. Com `NotificationContext`, o Handler pode acumular **múltiplos** erros de uma vez (em vez de parar no primeiro) e a API sempre responde de forma consistente.

## Quando usar / quando não usar

Use FluentValidation para validação de forma/estrutura do Command (campo vazio, formato, tamanho, faixa de valor) — isso roda antes do Handler, via pipeline behavior do MediatR.

Use `NotificationContext.AddNotification` dentro do Handler para regras que dependem de estado (ex: "esse id existe no banco?", "essa transição de status é permitida?") — coisas que o Validator sozinho não consegue checar sem acessar o repositório.

Não use exceções para erro de negócio esperado — reserve `throw` para falhas realmente excepcionais (infraestrutura indisponível, bug).

## Exemplo no código

TODO — preencher quando a feature de exemplo real for portada do FluxoDev.
