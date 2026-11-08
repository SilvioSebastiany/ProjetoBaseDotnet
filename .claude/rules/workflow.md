> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Development Workflow

> Leia este arquivo antes de criar/alterar: fluxo de uma feature (spec, codigo, review).

Guia de fluxo de desenvolvimento de features, baseado em specs e planos.

---

## 1. Planejamento de Feature

- Criar spec em `specs/{feature-id}/`.
- Definir:
  - objetivos da feature
  - data model
  - endpoints
  - validações
- Revisar e aprovar `plan.md` antes de codar.

---

## 2. Implementação

1. **Criar Command**
   - `Domain/Commands/{Feature}/{Action}/`
   - Criar Command, Handler e Validator.

2. **Modelar a ação na entidade** (`domain-modeling.md`)
   - Teste de entidade primeiro (`Deve_{Resultado}_Quando_{Condicao}`), depois o método da ação, com todas as pré-condições.
   - Dado de outro agregado entra por parâmetro.

3. **Implementar Handler que orquestra**
   - Busca, chama external, passa por parâmetro, chama a ação, propaga `AddNotifications(entidade)`, salva.
   - Nenhum `if` de regra de negócio no handler.

4. **Mapear Banco (se necessário)**
   - Criar `{Entity}Map.cs` em `Infra.Data/Context/Mappers/{Feature}/`.
   - Configurar tabela, colunas, relacionamentos e auditoria.

5. **Registrar Dependências**
   - Atualizar `*.Infra.CrossCutting.Ioc` com novos serviços/repositórios.

6. **Criar Endpoints**
   - Adicionar action no controller.
   - Delegar ao MediatR para Commands.
   - Documentar com XML comments (OpenAPI).

7. **Criar Migration (se houver mudança no schema)**
   - Executar `dotnet ef migrations add ...`
   - Atualizar banco local.

---

## 3. Validação e Testes

- Validar manualmente pela UI do OpenAPI (Scalar) ou por um `.http`.
- A regra é provada no teste da entidade, sem mock; o teste do handler prova a orquestração.

---

## 4. Code Review

- Garantir que alterações seguem o CLAUDE.md (raiz e `.claude/CLAUDE.md`) e as regras em `.claude/rules/`.
- Utilizar a checklist de review:
  - Arquitetura (camadas, dependências)
  - Modelagem (`domain-modeling.md`): regra na ação da entidade, handler sem `if` de regra, filha só pela raiz, service sem `SaveChanges`
  - Idempotência (`idempotency.md`): operação que cria documento ou move dinheiro com chave, índice único, retomada, e um cenário de repetição no PR
  - Externals (`external-services.md`): cada desfecho tratado, indisponível sem virar falha, credencial do parceiro sem virar 403 ao usuário, corpo cru só no log
  - CQRS + Validators
  - Mapping de banco (se houver)
  - Async/await + CancellationToken
  - Naming
  - IoC
  - Qualidade/SOLID

(Checklist pode ser detalhado em `.claude/rules/testing-guidelines.md` ou em um doc próprio.)

---

## 5. Git, branch e Pull Request

**Branch sempre a partir da branch padrão do remoto atualizada** (`origin/master` ou `origin/main`,
a que o repositório usar), e ela é o alvo de todo PR. Antes de começar qualquer coisa:
`git fetch origin` e crie a branch de `origin/<padrão>`, não da sua cópia local, que pode estar
velha.

**Formato do commit — obrigatório:** [Conventional Commits](https://www.conventionalcommits.org/).

```
<tipo>: <mensagem no imperativo>
```

Tipos: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`. Exemplo:
`feat: adiciona endpoint de listagem de faturas vencidas`. Com issue, ela vai no rodapé
(`Closes #12`), não no assunto. O assunto diz o quê; o corpo diz o porquê.

**Atualizar branch que já tem PR aberto** — o ciclo mais comum, quando um PR anterior da pilha foi
aprovado e mesclado:

```bash
git fetch origin
git rebase origin/master           # rebase, nunca merge
git push --force-with-lease        # nunca --force puro
```

`--force-with-lease` recusa se alguém tiver empurrado algo que você não viu; `--force` sobrescreve
em silêncio. Se o rebase reclamar de *divergent branches*, é `pull.rebase` não configurado — isso se
resolve na máquina, não no repositório.

**Abrir o PR pelo `gh`:**

```bash
gh pr create --base master --title "feat: ..." --body "..."
```

**O que ficou fora do padrão e não foi tocado vai na descrição do PR**, numa seção "Fora do padrão, não
tocado", e numa linha em "não copie" do mapa de moldes do repositório (`domain-modeling.md` § 10).

**Scripts de banco vão para a issue.** Todo script que precisará rodar em produção — migration
manual, `UPDATE` de correção, criação de índice — fica registrado na issue do repositório, para
*todos* os projetos tocados. Sem isso o deploy chega em produção sem o que ele precisa.

## 6. Deploy

- Pipelines automatizados aplicam migrations (quando for o caso).
- Feature marcada como concluída após validação em ambiente alvo.
