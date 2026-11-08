---
paths:
  - "src/**/*.cs"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Idempotency

> Leia este arquivo antes de criar/alterar: rota que cria documento ou move dinheiro, chamada a outro
> sistema que cria ou move algo, retry HTTP, ou robô e consumidor que reprocessam.

A mesma operação vai chegar mais de uma vez: duplo clique, retry HTTP, robô na rodada seguinte,
webhook e robô ao mesmo tempo, operador reprocessando. Idempotência é a resposta escrita a isso — e
ela é **da operação de negócio**, não da requisição HTTP.

---

## 1. Quando é obrigatória

| Operação | Estratégia |
|---|---|
| cria documento em outro sistema (ordem, devolução, boleto, link, ticket, envelope) ou move dinheiro (cobrança, captura, estorno, devolução, vale) | **obrigatória, dos dois lados** (§§ 3 e 4) |
| transição de estado (aprovar, cancelar, confirmar pagamento) | a própria entidade: a ação só sai do estado de origem, e a repetição no estado de destino não faz nada e não é erro |
| cadastro que pode legitimamente repetir | não precisa |
| `GET`, `PUT`, `DELETE` | já são, desde que o `PUT` não crie e o `DELETE` do que já foi apagado não seja erro |

---

## 2. A chave da operação

- **Identifica a operação, não o recurso pai:** `isencao-multa-{multaId}`,
  `renovacao-{assinaturaId}-c{ciclo}`. O código do pedido sozinho, quando o pedido tem várias
  operações, faz duas operações diferentes parecerem a mesma.
- **Gerada por quem inicia a operação, gravada antes da primeira chamada, e a mesma em toda
  retentativa.** `Guid.NewGuid()` na hora de chamar não é chave: cada tentativa vira outra operação.
- **Determinística** quando a operação é única por entidade. **Guid gravado** quando a mesma
  entidade pode ter tentativas legítimas diferentes (`-t{n}`).
- No contrato e na entidade, o nome é `ChaveIdempotencia`.

---

## 3. Quem recebe: a nossa API

A chave vem **no corpo do contrato**, é gravada na entidade com **índice único**, e junto vai a
**impressão digital** da operação: o hash dos campos que a definem.

| Chega | Responde |
|---|---|
| chave nova | processa |
| mesma chave e mesma impressão, operação concluída — **em qualquer situação** (paga, cancelada, recusada) | o resultado da primeira vez, com o mesmo status HTTP (201 na criação) |
| mesma chave, impressão diferente | 422 |
| mesma chave, a primeira ainda em andamento (reservada, sem desfecho) | 409 |

- **Conferir antes de inserir não basta.** Duas requisições ao mesmo tempo passam pela conferência;
  quem segura é o índice único. A violação dele na corrida não vira 500, e o handler não ganha
  `try/catch` por isso:
  - **a captura mora na Infra.** Um método do UnitOfWork ou do repositório traduz a violação do
    índice único num resultado ("a chave já existia");
  - **ele também tira do rastreio a entidade que falhou.** Ela continua `Added`, e o próximo
    `SaveChanges` do escopo tentaria o insert de novo;
  - **com esse resultado, o handler relê pela chave** e responde como repetição.
- **O 409** é a notificação com a chave `NotificationKeys.CONFLICT` (constante do projeto, em
  `Domain/Notifications/`), que o filtro de notificação da API converte em 409. Sem essa chave, a
  notificação sairia como 422, e quem chama a leria como recusa definitiva de uma operação que
  ainda pode concluir.
- **Por que a chave de negócio, e não só o cabeçalho `Idempotency-Key`:** o índice não expira, e
  impede a duplicata que nasce por dois caminhos diferentes (duplo clique e robô), que o cabeçalho
  — uma chave por requisição, guardada por um tempo — não impede.
- **API pública cujo chamador não tem um id natural** pode aceitar o cabeçalho `Idempotency-Key`,
  gravado no mesmo `ChaveIdempotencia`. Nunca um mecanismo paralelo.

---

## 4. Quem chama: nós chamando outro sistema

1. **Reserva:** grava a tentativa com a chave e a situação "aguardando envio", antes de chamar.
2. **Chama** mandando a chave, ou a referência que o parceiro usa para deduplicar ou consultar.
3. **Desfecho**, que o external traduz (`external-services.md` § 2.2):
   - o outro lado disse sim: grava a referência externa num `SaveChanges`, e os efeitos depois,
     noutro;
   - disse não: falha;
   - timeout, 409, 5xx, resposta sem desfecho: **desconhecido**, e nunca falha.
4. **Retomada** do desconhecido, pelo robô ou pela próxima chamada:
   - o parceiro **consulta pela chave**: consulta antes de reenviar;
   - o parceiro **deduplica pela chave**: reenvia com a mesma chave;
   - **nenhum dos dois:** não reenvia sozinho. Quando o efeito é dinheiro ou documento, para para
     conferência.

**O guarda de duplicidade distingue reserva sem desfecho de operação concluída.** Tratar a reserva
como "já existe" trava a retentativa para sempre — é por isso que gravar antes de chamar, sem esse
cuidado, piora o caso que queria consertar.

A entidade recebe o desfecho numa ação só, que confere a guarda e decide o efeito:

```csharp
public bool AguardaRetomada => Situacao is ESituacaoTentativa.Reservada or ESituacaoTentativa.Desconhecida;

public void RegistrarEmissao(EmissaoOrdem emissao)
{
    if (!AguardaRetomada)
    {
        AddNotification(nameof(Situacao), "A emissão já tem desfecho registrado.");
        return;
    }

    switch (emissao.Resultado)
    {
        case EResultadoEmissaoOrdem.Emitida:
            Emitir(emissao.NumeroOrdem);
            break;
        case EResultadoEmissaoOrdem.Recusada:
            Recusar(emissao.Motivo);
            break;
        case EResultadoEmissaoOrdem.Desconhecida:
            Situacao = ESituacaoTentativa.Desconhecida;
            break;
        default:
            throw new NotSupportedException($"Emissão sem tratamento para {emissao.Resultado}");
    }
}
```

---

## 5. Retry HTTP automático

- `GET`, `PUT` e `DELETE` retentam em falha transitória.
- **`POST` só retenta quando o destino deduplica pela chave que mandamos.** Para os outros, a falha
  transitória vira desconhecido e segue pela retomada (§ 4) — reenviar às cegas cria o documento duas
  vezes.
- O registro do `HttpClient` diz qual é o caso:

```csharp
services.AddHttpClient<IParceiroService, ParceiroService>(cliente => cliente.BaseAddress = endereco)
    .AddPolicyHandler(requisicao => requisicao.Method == HttpMethod.Post
        ? Policy.NoOpAsync<HttpResponseMessage>()
        : retryTransitorio);
```

---

## 6. Robô, mensagem e corrida

- **Robô** reprocessa por estado: pega só o que aguarda retomada, nunca tudo de novo.
- **Duas instâncias** do mesmo robô: reivindicação atômica do item, ou índice único na emissão.
- **Consumidor de mensagem:** idempotente pelo `Id` da mensagem (`validation-and-cqrs.md` § 16.2).
- **Webhook e robô** agindo na mesma operação: a transição de estado na entidade é a primeira
  linha de defesa; o índice único na emissão, a segunda.

---

## 7. Teste

Cada forma de repetir tem o seu teste:

- a mesma requisição duas vezes;
- a repetição depois de concluída, e depois de cancelada;
- a mesma chave com outro conteúdo;
- duas ao mesmo tempo (o índice);
- a retomada depois de timeout: o desconhecido não vira falha, e não duplica.
