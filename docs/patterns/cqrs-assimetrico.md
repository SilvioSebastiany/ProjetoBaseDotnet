# CQRS assimétrico

## O que é

CQRS (Command Query Responsibility Segregation) aqui é aplicado de forma **assimétrica**: só a escrita passa pelo padrão completo (Command → MediatR → Handler). A leitura não usa Mediator nem Handler — o Controller de leitura (`QueryController`) chama o repositório diretamente.

## Por que usamos

Em várias implementações de CQRS, toda operação — inclusive um simples "buscar por id" — vira um Query object com seu próprio Handler. Na prática, isso costuma adicionar uma classe e um arquivo a mais para operações que não têm nenhuma regra de negócio: só buscam e devolvem o dado. O CQRS assimétrico existe para esse ganho: manter o rigor (validação, acúmulo de notificação, transação) onde ele importa — a escrita — sem burocratizar a leitura, que é só busca.

## Quando usar / quando não usar

**É Command quando:**
- A operação altera o estado do sistema (criar, atualizar, excluir, transicionar status).
- Existe qualquer regra de negócio a validar antes de agir.

**É Query (QueryController) quando:**
- A operação só busca e devolve dado — nenhuma regra de negócio, nenhuma escrita.
- A única validação é de parâmetro simples (ex: "esse id existe?" → `404`), nunca de regra de negócio.

**Regra prática:** se você consegue responder "o que essa operação decide?" com "nada, só busca" → Query. Se a resposta envolve uma regra de negócio → Command.

Não crie um Command para uma leitura só para manter "consistência de padrão" — isso é exatamente o excesso de abstração que este padrão existe para evitar.

## Exemplo no código

TODO — preencher quando a feature de exemplo real for portada para `examples/`.
