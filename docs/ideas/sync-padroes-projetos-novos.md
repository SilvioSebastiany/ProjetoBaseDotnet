# Sincronizar padrões do ProjetoBaseDotnet para projetos novos

**Status: MVP implementado** em `~/.claude/skills/dotnet-init-solucao/` (`SKILL.md` + `assets/claude-md-secao-base.md`). Ver "MVP Scope" abaixo para o que ficou dentro/fora.

## Problem Statement
Como garantir que, ao criar (ou reabrir) um projeto .NET novo, o Claude sempre conheça e siga os padrões arquiteturais decididos em `ProjetoBaseDotnet` — sem depender de o usuário lembrar de referenciá-los manualmente a cada projeto, e sem cada projeto reescrever/divergir sua própria versão das mesmas regras?

## Evidência do problema (não é hipotético)

`FluxoDev` (`C:\SilvioArquivos\dev\FluxoDev\CLAUDE.md`) já reescreveu à mão toda a arquitetura de `ProjetoBaseDotnet` — fluxo de dependências, distinção Command/QueryController, convenções de nomenclatura — como texto solto dentro do próprio `CLAUDE.md`, sem nenhuma referência ao repositório-base. O skill global `dotnet-init-solucao` (`~/.claude/skills/dotnet-init-solucao/SKILL.md`) tem o mesmo problema: hardcoda a arquitetura fixa no próprio texto da skill, já desatualizado em relação à decisão mais recente deste projeto (Command e Query no mesmo Controller, não `QueryController` separado). Os dois casos mostram o mesmo padrão de falha: a fonte da verdade devia ser `ProjetoBaseDotnet`, mas cada lugar tem sua própria cópia, e elas já divergiram.

## Recomendação: skill de init copia e sincroniza docs/examples

`dotnet-init-solucao` (skill global em `~/.claude/skills/dotnet-init-solucao/`) ganha um passo novo: ao criar o esqueleto do projeto, copia `docs/` e `examples/` inteiros de `ProjetoBaseDotnet` para dentro do projeto novo (ex: `docs/base/` e `examples/base/`, para não colidir com docs próprios do projeto), e gera/atualiza a seção correspondente do `CLAUDE.md` apontando para essa cópia local.

Se a skill for rodada de novo num projeto que já tem essa cópia, ela **sobrescreve sempre, sem perguntar** — a cópia local nunca é o lugar de customizar. Isso é uma escolha deliberada de simplicidade: em vez de suportar merge ou detecção de customização (que exigiria diff, backup, confirmação), a regra é simples e previsível — "essa pasta é sempre um espelho exato do ProjetoBaseDotnet no momento do sync". Qualquer adaptação específica de um projeto (como o `FluxoDev` fez, adaptando enums de `infra.crosscutting` para `domain`) precisa viver em outro lugar do `CLAUDE.md`, fora da área sincronizada — nunca dentro de `docs/base/`.

Isso também resolve o problema simetricamente na origem: o próprio `~/.claude/skills/dotnet-init-solucao/SKILL.md` deixa de hardcodar a arquitetura no texto da skill — ele passa a apontar para `ProjetoBaseDotnet` como fonte única, e delega a explicação detalhada para a cópia sincronizada dentro de cada projeto novo.

## Key Assumptions to Validate
- [ ] `docs/` + `examples/` cabe sem ficar poluído dentro de um projeto novo pequeno — validar visualmente no próximo projeto criado.
- [ ] "Sobrescrever sempre sem perguntar" não causa perda de trabalho real — só vale se a disciplina de "nunca customizar a pasta sincronizada" for seguida; testar rodando o sync duas vezes seguidas num projeto de teste.
- [ ] O nome de pasta (`docs/base/`, `examples/base/`) não colide com convenções que o projeto de destino já usa — confirmar ao implementar.

## MVP Scope
**Dentro (implementado):**
- Passo novo em `dotnet-init-solucao`: copia `docs/` e `examples/` de `ProjetoBaseDotnet` para `docs/base/` e `examples/base/` no projeto novo.
- Seção fixa no `CLAUDE.md` do projeto novo, delimitada por marcadores (`<!-- INÍCIO/FIM SEÇÃO SINCRONIZADA: dotnet-init-solucao -->`) para permitir re-sync sem duplicar — texto exato em `assets/claude-md-secao-base.md`.
- Regra de sync documentada no passo a passo: sobrescreve sempre, sem perguntar; se `ProjetoBaseDotnet` não existir na máquina, pula e avisa em vez de falhar.
- A própria skill parou de hardcodar a arquitetura em texto: agora referencia `ProjetoBaseDotnet` como fonte única (seção "Fonte única da arquitetura").

**Fora (não implementado, decisão explícita):**
- Merge inteligente ou detecção de customização local.
- Sincronização automática/agendada (ex: hook, watcher) — só roda quando a skill é invocada manualmente.
- Sync como parte das outras skills (`dotnet-init-testes`, `dotnet-init-docker-postgres`, `dotnet-init-frontend-vite`) — ficou só em `dotnet-init-solucao` por ora (ver Open Questions).
- Script de sync isolado (sem recriar a solução) — não criado; rodar a skill inteira de novo já cobre o caso de uso por enquanto.

## Not Doing (and Why)
- **MCP server** — complexidade desproporcional: exigiria um processo rodando, protocolo próprio, manutenção separada, para resolver algo que uma cópia de arquivo resolve.
- **Skill dedicado de "consulta"** (`consultar-padrao-arquitetura`) — adicionaria uma segunda skill para sincronizar com uma terceira fonte (o próprio skill), sem necessidade: o mecanismo de cópia já resolve o problema de forma mais direta.
- **Memória global do Claude Code** — descartada porque o usuário decidiu explicitamente por uma referência por-projeto, não uma regra pessoal válida em qualquer sessão/máquina.
- **Merge/confirmação ao sobrescrever** — descartado a favor de simplicidade; se isso causar perda de customização real no futuro, reavaliar.

## Open Questions
- O sync deve também rodar como parte de `dotnet-init-testes` / `dotnet-init-docker-postgres` / `dotnet-init-frontend-vite`, ou só uma vez, na skill de solução base?
- Vale um pequeno script (`sync-docs-base.sh`/`.ps1`) dentro do skill, separado do fluxo de criação, para permitir rodar só o sync sem recriar a solução inteira?
