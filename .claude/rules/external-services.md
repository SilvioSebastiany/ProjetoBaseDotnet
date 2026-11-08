---
paths:
  - "src/**/*.cs"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# External Services

> Leia antes de criar ou alterar: integração com outro sistema (`Infra.ExternalServices`, SDK de
> outro serviço), ou o tratamento do que ela devolve num handler ou num robô. Antes de criar um
> external novo, o § 11: o sistema pode já ter SDK.

O external é a **camada anticorrupção**: traduz o que o outro sistema respondeu (status, corpo,
exceção) num desfecho que o domínio entende. O domínio nunca vê HTTP. Quem decide o que o desfecho
significa para o negócio é o handler e a entidade, e não o external.

---

## 1. Os desfechos

| Desfecho | O que chegou | Para quem chama |
|---|---|---|
| **Sucesso** | 2xx com o que se esperava | segue |
| **Não encontrado** | 404, 410 | em consulta, é resposta (§ 3); em comando, é recusa |
| **Recusado** | 400, 422 e os demais 4xx; 2xx com recusa no corpo (§ 7) | o outro sistema disse não: definitivo, não se repete |
| **Indisponível** | timeout, falha de rede, circuito aberto, 408, 409, 425, 429, 5xx, corpo ilegível | pode dar certo depois; se move dinheiro ou cria documento, é **desconhecido** (`idempotency.md` § 4) |
| **Falha técnica** | 401, 403 e 407 do parceiro | a credencial ou a configuração da integração está errada: é nossa, e não do usuário (§ 5) |

O **409** é indisponível porque, entre os nossos serviços, é a mesma operação ainda em andamento
(`idempotency.md` § 3). Parceiro que usa o 409 para outra coisa ("já existe") é sobreposição do
external.

**A classificação mora num lugar só da Infra**, e não se reescreve em cada external: uma classe
`HttpResponseMessageExtensions` em `Infra.ExternalServices/Extensions/`, com
`resposta.ClassificarDesfecho()`, que devolve o `EDesfechoHttp`, e
`excecao.IndicaIndisponibilidade(cancellationToken)`, que diz se a exceção é indisponibilidade. O
cancelamento pedido por quem chamou não é; ele continua propagando. O `LerCorpoParaLogAsync` e o
`LerNotificacoesDoErroAsync` dos exemplos moram na mesma classe.

```csharp
public static EDesfechoHttp ClassificarDesfecho(this HttpResponseMessage resposta) =>
    (int)resposta.StatusCode switch
    {
        >= 200 and < 300 => EDesfechoHttp.Sucesso,
        404 or 410 => EDesfechoHttp.NaoEncontrado,
        401 or 403 or 407 => EDesfechoHttp.FalhaTecnica,
        408 or 409 or 425 or 429 or >= 500 => EDesfechoHttp.Indisponivel,
        _ => EDesfechoHttp.Recusado,
    };

public static bool IndicaIndisponibilidade(this Exception excecao, CancellationToken cancellationToken) =>
    excecao switch
    {
        OperationCanceledException when cancellationToken.IsCancellationRequested => false,
        HttpRequestException or TaskCanceledException or TimeoutException or JsonException => true,
        BrokenCircuitException => true,
        _ => false,
    };
```

O retry, o timeout e o circuito vêm do `Microsoft.Extensions.Http.Resilience`
(`AddStandardResilienceHandler()` no `HttpClient` tipado), e não de laço escrito à mão; o
`BrokenCircuitException` é do Polly, que ele usa por baixo.

O external **só sobrepõe o que é do parceiro**: o 404 que é estado normal, o 2xx com recusa no
corpo, o código de negócio que o parceiro manda num 500.

---

## 2. O que o external devolve: dois formatos

| Quem chama | Formato |
|---|---|
| só segue ou para: consulta de tela, comando síncrono em que qualquer falha é "não deu" | **notifica e devolve `default`** (§ 2.1) |
| age diferente por desfecho: move dinheiro, cria documento, robô que escolhe entre reagendar e encerrar, a mesma chamada servindo API e robô | **desfecho de domínio** (§ 2.2) |

Não se adivinha a causa pela chave da notificação (`"NotFound"`): quem precisa distinguir recebe o
desfecho. SDK que notifica com chave própria: só o external lê essa chave, e devolve o desfecho.

O desfecho que o código não trata é erro de programação: `NotSupportedException` com o desfecho
na mensagem (`validation-and-cqrs.md`, *Única exceção*).

### 2.1. Notifica e devolve `default`

Em consulta, o 404 de "não existe" devolve `default` sem notificar (§ 3). Em comando, o não
encontrado notifica junto com a recusa.

```csharp
public async Task<TransacaoDto> ObterTransacaoPorPedidoAsync(string pedido, CancellationToken cancellationToken)
{
    try
    {
        var resposta = await httpClient.GetAsync($"transacoes?pedido={pedido}", cancellationToken);

        switch (resposta.ClassificarDesfecho())
        {
            case EDesfechoHttp.Sucesso:
                return await resposta.Content.ReadFromJsonAsync<TransacaoDto>(cancellationToken: cancellationToken);
            case EDesfechoHttp.NaoEncontrado:
                return default;
            default:
                await NotificarFalhaAsync(resposta, "consultar a transação", cancellationToken);
                return default;
        }
    }
    catch (Exception excecao) when (excecao.IndicaIndisponibilidade(cancellationToken))
    {
        logger.LogWarning(excecao, "Pagamentos não respondeu à consulta do pedido {Pedido}", pedido);
        notificationContext.AddNotification(NotificationKeys.SERVICE_UNAVAILABLE, PAGAMENTOS_INDISPONIVEL);
        return default;
    }
}

private async Task NotificarFalhaAsync(HttpResponseMessage resposta, string operacao, CancellationToken cancellationToken)
{
    var desfecho = resposta.ClassificarDesfecho();
    var status = (int)resposta.StatusCode;
    var corpo = await resposta.LerCorpoParaLogAsync(cancellationToken);

    switch (desfecho)
    {
        case EDesfechoHttp.Recusado:
        case EDesfechoHttp.NaoEncontrado:
            logger.LogWarning("Pagamentos recusou {Operacao} com {Status}: {Corpo}", operacao, status, corpo);
            notificationContext.AddNotifications(await resposta.LerNotificacoesDoErroAsync(cancellationToken));
            break;
        case EDesfechoHttp.Indisponivel:
            logger.LogWarning("Pagamentos indisponível ao {Operacao}, com {Status}: {Corpo}", operacao, status, corpo);
            notificationContext.AddNotification(NotificationKeys.SERVICE_UNAVAILABLE, PAGAMENTOS_INDISPONIVEL);
            break;
        case EDesfechoHttp.FalhaTecnica:
            logger.LogError("Pagamentos recusou a credencial da integração ao {Operacao}, com {Status}", operacao, status);
            notificationContext.AddNotification(NotificationKeys.SERVICE_UNAVAILABLE, PAGAMENTOS_INDISPONIVEL);
            break;
        default:
            throw new NotSupportedException($"Desfecho {desfecho} sem tratamento ao {operacao}.");
    }
}
```

### 2.2. Desfecho de domínio

Um enum ou VO **do domínio do serviço**, nomeado pela operação (`EmissaoOrdem`, com
`EResultadoEmissaoOrdem.Emitida | Recusada | Desconhecida`), que leva a referência ou o motivo
traduzido. Nunca `Result<T>` genérico, `HttpStatusCode` ou `HttpResponseMessage`.

A falha técnica, para quem chama, é desconhecida: a retomada reenvia com a mesma chave depois que a
credencial for corrigida.

```csharp
public async Task<EmissaoOrdem> EmitirOrdemAsync(OrdemDto ordem, CancellationToken cancellationToken)
{
    try
    {
        var resposta = await httpClient.PostAsync("ordens", HttpContentFactory.CreateJsonContent(ordem), cancellationToken);
        var desfecho = resposta.ClassificarDesfecho();

        if (desfecho is EDesfechoHttp.FalhaTecnica)
            logger.LogError("Credencial recusada ao emitir a ordem {Chave}, com {Status}", ordem.ChaveIdempotencia, (int)resposta.StatusCode);

        return desfecho switch
        {
            EDesfechoHttp.Sucesso => EmissaoOrdem.Emitida((await resposta.Content.ReadFromJsonAsync<OrdemCriadaDto>(cancellationToken: cancellationToken)).Numero),
            EDesfechoHttp.Recusado or EDesfechoHttp.NaoEncontrado => EmissaoOrdem.Recusada(await TraduzirRecusaAsync(resposta, cancellationToken)),
            EDesfechoHttp.Indisponivel or EDesfechoHttp.FalhaTecnica => EmissaoOrdem.Desconhecida(),
            _ => throw new NotSupportedException($"Desfecho {desfecho} sem tratamento ao emitir a ordem.")
        };
    }
    catch (Exception excecao) when (excecao.IndicaIndisponibilidade(cancellationToken))
    {
        logger.LogWarning(excecao, "Sem resposta ao emitir a ordem {Chave}", ordem.ChaveIdempotencia);
        return EmissaoOrdem.Desconhecida();
    }
}
```

O handler passa o desfecho a **uma** ação da entidade, que decide o efeito: grava o número quando
foi emitida, falha quando foi recusada, e deixa para a retomada quando é desconhecida
(`idempotency.md` § 4).

```csharp
var emissao = await ordemService.EmitirOrdemAsync(OrdemMapper.Map(cobranca), cancellationToken);
cobranca.RegistrarEmissao(emissao);
```

---

## 3. Consulta: "não encontrado" é resposta

- O 404 de "não existe" devolve `default`, ou lista vazia, **sem notificar**. Quem chama decide se,
  ali, não existir é erro.
- **A mesma pergunta tem uma resposta só:** dois métodos que consultam a mesma coisa não tratam o
  "não achei" de jeitos diferentes.
- Parceiro que devolve 404 também para rota inexistente: o external confere o corpo antes de
  responder "não existe" (§ 7, o corpo se lê uma vez).
- Consulta com erro **nunca responde o que é cômodo** ("existe", "pendente", lista vazia). Erro é
  indisponível ou falha técnica, e quem chama fica sabendo.

---

## 4. Mensagem ao usuário e log

- **Ao usuário vai a mensagem traduzida:** o que não deu certo, em termos do negócio, e o que fazer.
  A recusa de outro serviço nosso (422, `ValidationProblemDetails`) já vem em termos do negócio, e
  passa por `LerNotificacoesDoErroAsync`, que nunca estoura nem devolve vazio: HTML de proxy, corpo
  vazio e 422 sem lista viram "o sistema chamado respondeu de forma inesperada".
- **A recusa de um parceiro de fora** chega ao usuário só quando é legível e diz o motivo (cartão
  negado, material bloqueado), com o que se tentava fazer na frente. Código, JSON e HTML, nunca.
- **O corpo cru, o status e a exceção vão para o log**, nunca para a notificação.
  `LerCorpoParaLogAsync` trunca o corpo. Na mensagem não entram `StatusCode`, `ReasonPhrase`,
  `ex.Message` nem `resposta.ToString()`.
- **A chave da notificação** é o campo ou a operação, e nunca o nome ou o código do status.
  - As únicas chaves de transporte são as do `NotificationKeys` (constantes do projeto, em
    `Domain/Notifications/`), que o filtro de notificação da API converte em status.
  - Elas moram no Domain, que a Infra já referencia, e por isso nunca se escreve o literal.
  - Uma chave `"403"` escrita por engano faz o filtro responder "Acesso negado" ao usuário.

---

## 5. Falha técnica

- **401, 403 e 407 do parceiro são a credencial ou a configuração da integração.** O external loga
  como **erro**, porque é esse log que leva alguém a agir. Para quem chama, é **indisponível**:
  - 503 na API;
  - desconhecido em dinheiro e documento;
  - próximo ciclo no robô.

  Nunca chega ao usuário como 403 ou 422.
- **Não é `throw`.** Falha técnica vai por notificação (`CLAUDE.md`, Notification Context), e o
  filtro de exceção da API não loga: lançar trocaria o log de erro por um 500 sem rastro.
- **A exceção é a chamada a outro serviço nosso com o token do usuário** repassado (um
  `DelegatingHandler` que copia o `Authorization` da requisição). Ali, o 403 é do usuário, e o external o propaga com a chave
  `NotificationKeys.FORBIDDEN`. O 401 continua sendo falha técnica.

---

## 6. Indisponível: por onde sai

- **API, com o usuário esperando:** 503, pela notificação com a chave
  `NotificationKeys.SERVICE_UNAVAILABLE`.
  - O filtro de notificação da API converte a chave em 503. Sem ela, a notificação sai como 422, e
    quem chama a lê como recusa definitiva.
  - Quando há notificações de vários tipos, a ordem é 403 > 409 > 503 > 422.
  - A rota que chama um external de forma síncrona declara
    `[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]`.
- **Dinheiro ou documento:** desconhecido, nunca falha. A retomada segue `idempotency.md` § 4.
- **Robô:** o item fica para o próximo ciclo. Não vira falha definitiva e não derruba o lote:
  - o `catch` é por item;
  - o `catch (OperationCanceledException) { throw; }` leva
    `when (cancellationToken.IsCancellationRequested)`. Sem o `when`, o timeout do `HttpClient`,
    que também é `OperationCanceledException`, aborta o ciclo inteiro.
- **Retry automático:** `idempotency.md` § 5.

---

## 7. 2xx com erro no corpo

Quando o parceiro devolve 2xx com o resultado no corpo, quem classifica é o corpo:
- recusa no corpo é **Recusado**;
- "em processamento" ou "pendente" **não é recusa**: é desconhecido, e segue a retomada;
- corpo que não desserializa é **Indisponível**. Por isso a leitura do corpo fica dentro do `try`
  que trata a indisponibilidade.

**O corpo se lê uma vez.** Ler o JSON duas vezes da mesma resposta estoura (`Stream was not
readable`), e a exceção não é de indisponibilidade. Quando o corpo serve para classificar e para
usar, leia como texto e desserialize do texto. `LerCorpoParaLogAsync` e `LerNotificacoesDoErroAsync`
leem como texto, e por isso funcionam juntos, em qualquer ordem.

---

## 8. Integração auxiliar (best-effort)

E-mail, métrica, ticket, enriquecimento: a falha não derruba o fluxo e não muda a resposta.

- Roda **depois do commit**, como reação descartável (ferramenta 5 da `validation-and-cqrs.md`
  § 16), que pode chamar sistema externo. Se a perda não é aceitável, não é auxiliar: é evento de
  integração (ferramenta 4).
- A falha vira log de aviso com o que se perdeu. Nunca `catch { }` calado.
- **SDK que notifica em vez de lançar:** guarde as notificações antes da chamada e, depois dela,
  descarte as novas, logando cada uma. Sem isso, a notificação do SDK vira 422 de um fato que já
  foi gravado.

```csharp
var anteriores = notificationContext.Notifications.ToList();

try
{
    await emailService.EnviarAsync(mensagem, cancellationToken);
}
catch (Exception excecao) when (!cancellationToken.IsCancellationRequested)
{
    logger.LogWarning(excecao, "E-mail {Assunto} não enviado", mensagem.Assunto);
}
finally
{
    DescartarNotificacoesNovas(anteriores);
}

private void DescartarNotificacoesNovas(IReadOnlyCollection<Notification> anteriores)
{
    foreach (var notificacao in notificationContext.Notifications.Skip(anteriores.Count))
        logger.LogWarning("Notificação do envio de e-mail descartada: {Mensagem}", notificacao.Message);

    notificationContext.ClearNotifications();
    notificationContext.AddNotifications(anteriores);
}
```

---

## 9. API e robô

- **O external é o mesmo e devolve o mesmo desfecho nos dois.** Quem decide o efeito é o handler:
  na API, o status da resposta; no robô, reagendar ou encerrar. O mesmo 5xx nunca é "tente de novo"
  num e "falha definitiva" no outro.
- **No robô, notificação não é resposta.** O que vale é o que o handler grava. A notificação do item
  é logada e limpa a cada item.

---

## 10. Teste

O external tem um teste por desfecho, com um `HttpMessageHandler` falso:
- 2xx;
- 404;
- 409, que é indisponível;
- 422;
- 500;
- corpo HTML num 502 e num 200, os dois indisponíveis;
- timeout (`TaskCanceledException` sem o token cancelado);
- cancelamento de quem chamou, que propaga;
- 401, com log de erro e indisponível para quem chama.

O handler tem os seus: indisponível não vira falha, e falha técnica não vira 403 nem 422.

---

## 11. Antes de escrever o external: SDK

O external de um sistema que outros também chamam é um SDK: escrito uma vez, com as regras
deste arquivo, e usado por todos. Cada sistema com o seu external para o mesmo destino é a mesma
classificação de erro escrita N vezes, e cada uma diverge de um jeito.

Antes de escrever, procure no nuget.org o SDK oficial do sistema (publicado pelo dono dele), e nos
seus outros repositórios um external para o mesmo destino.

| Situação | O que fazer |
|---|---|
| o sistema tem SDK | **usa o SDK, e não escreve external.** Falta a operação? Ela entra no SDK |
| sem SDK, e só o seu sistema o chama | external no projeto |
| sem SDK, e outro projeto seu já o chama por external próprio | **é hora do SDK:** proponha o SDK em vez de escrever mais um external |

- **Usando o SDK,** a interface dele é injetada direto no handler ou no service do Domain, sem
  external de embrulho na Infra: o SDK já é a camada anticorrupção. Quando o retorno vira
  entidade, o mapeamento fica num mapper do Domain. É por isso que o SDK nunca devolve detalhe de
  HTTP: o que ele devolve entra no Domain.
- Vale para sistema seu e para parceiro de fora.
- O SDK segue este arquivo inteiro: ele é o external de todos. Classifica o desfecho, traduz a
  mensagem, e não deixa HTTP passar para fora dele.
- Quem já chama o sistema por external próprio migra para o SDK quando for tocado, e não num PR
  só para isso.
- Criar o repositório do SDK é decisão de quem conduz. Trabalho autônomo registra a proposta na
  descrição do PR e traz.
