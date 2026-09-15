# Testes: xUnit + Moq + AutoFixture + Shouldly

## O que é

A combinação de bibliotecas usada em todos os testes automatizados do projeto:
- **xUnit** — framework de testes.
- **Moq** — criação de mocks para as dependências (ex: `IProdutoRepository`) dos Handlers e Controllers sob teste.
- **AutoFixture** — geração automática de dados de teste (evita montar objetos de teste campo a campo à mão).
- **Shouldly** — asserções mais legíveis (`resultado.ShouldBe(...)`) no lugar do `Assert.Equal` padrão do xUnit.

## Por que usamos

Handlers e Repositories têm dependências (outros repositórios, `NotificationContext`) que precisam ser isoladas no teste unitário — é para isso que serve o Moq. AutoFixture reduz o boilerplate de montar Commands e entidades de teste, principalmente quando eles têm muitos campos (como `Tarefa`, com uma dezena de propriedades). Shouldly deixa a asserção mais próxima da leitura em português do que se está verificando, o que ajuda porque o domínio inteiro também é nomeado em português.

## Quando usar / quando não usar

Todo `CommandHandler` tem teste unitário cobrindo o caminho feliz e os principais casos de notificação de erro (ex: "projeto não existe" → `NotificationContext` recebe a notificação e o Handler não persiste nada).

`QueryController`s simples (sem regra de negócio) podem ter cobertura mais leve — o valor do teste ali é menor, já que não há lógica a validar, só passagem de dado.

Não use AutoFixture para gerar valores que o teste precisa controlar explicitamente (ex: um id específico usado na asserção) — nesses casos, defina o valor manualmente e use AutoFixture só para o resto do objeto.

## Exemplo no código

TODO — preencher quando a feature de exemplo real for portada do FluxoDev.
