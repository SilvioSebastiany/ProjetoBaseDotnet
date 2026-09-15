# Git

Convenção de commits para este projeto e para os projetos criados a partir dele, baseada no padrão já usado no FluxoDev.

## Formato

Segue [Conventional Commits](https://www.conventionalcommits.org/), em português, no imperativo:

```
<tipo>: <descrição curta em português, no imperativo>

[corpo opcional — só se precisar explicar o "porquê", não o "o quê"]
```

Em projetos que trabalham com tarefas com identificador (como o FluxoDev, que usa `T-XX`), o ID da tarefa entra como escopo obrigatório: `<tipo>(<T-XX>): <descrição>`.

## Tipos permitidos

| Tipo | Quando usar |
|---|---|
| `feat` | Nova funcionalidade (nova entidade, Command, Query, endpoint, componente) |
| `fix` | Correção de algo que estava errado |
| `chore` | Tarefa de infraestrutura sem lógica de negócio (estrutura de pastas, configuração de projeto, dependências) |
| `test` | Adição ou ajuste de testes, sem mudar código de produção |
| `docs` | Mudança só em documentação (`docs/`) |
| `refactor` | Reorganização de código sem mudar comportamento |

## Quando commitar

- Um commit ao finalizar o desenvolvimento de uma tarefa/feature e entregar para revisão — não antes, não em pedaços soltos no meio do trabalho.
- Um novo commit a cada correção subsequente na mesma tarefa, se ela for retornada pela revisão. Sempre na mesma branch, nunca reescrevendo (`--amend`, `rebase`, `force-push`) o commit anterior.
- Nunca commitar código que não builda.

## O que não fazer

- Não usar `git commit -m "wip"`, `"ajustes"`, `"update"` ou qualquer mensagem sem contexto.
- Não commitar `bin/`, `obj/`, `node_modules/`.
- Não squashar tentativas anteriores de uma tarefa retornada.
