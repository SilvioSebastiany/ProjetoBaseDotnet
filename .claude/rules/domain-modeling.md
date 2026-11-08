---
paths:
  - "src/domain/**"
  - "tests/**"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Domain Modeling

> Leia este arquivo antes de criar/alterar: entidade, ação de negócio, handler de escrita, service de
> domínio ou comportamento que varia por tipo.

Como o domínio se modela na casa. **A regra escrita vale mais que o código vizinho**: se o arquivo ao
lado faz diferente, ele é o desvio. Antes de copiar um molde, confira o mapa de moldes do repositório
(`docs/padrao/moldes.md`), quando existir.

---

## 1. O caminho de uma ação

Toda escrita é uma ação de negócio com nome, e cada peça tem um papel só:

| Peça | Faz | Não faz |
|---|---|---|
| Controller | manda o Command e mapeia o retorno | regra, guarda de `null` (o filtro global converte notificação em 422) |
| Validator do Command | campo obrigatório, formato, tamanho | regra de negócio, faixa de valor do negócio, I/O |
| Handler | busca o que a regra precisa, chama o external, passa por parâmetro, chama **uma** ação, propaga, salva | decidir regra: `if` sobre campo da entidade é regra fora do lugar |
| Ação da entidade | confere **todas** as pré-condições; se uma falha, notifica e não muda nada | contar com o handler ter conferido antes |

```csharp
public async Task<bool> Handle(FinalizarContratoCommand request, CancellationToken cancellationToken)
{
    var contrato = await contratoRepository.ObterPorIdAsync(request.ContratoId, cancellationToken);

    if (contrato is null)
        return default;

    var possuiDevolucaoEmAberto = await devolucaoRepository.ExisteEmAbertoAsync(contrato.Id, cancellationToken);

    contrato.Finalizar(request.Motivo, possuiDevolucaoEmAberto);

    if (contrato.IsInvalid)
    {
        notificationContext.AddNotifications(contrato);
        return default;
    }

    return await unitOfWork.SaveChangesAsync(cancellationToken);
}
```

- A injeção das dependências segue a forma da sua versão (`coding-standards.md` § 3).
- "Não encontrado" fica no handler: `return default` sem notificar dá 404. Com notificação, o filtro
  responde 422.
- Sem `AddNotifications(contrato)`, o 422 sai sem mensagem.
- Antes de uma chamada externa que cria documento, o handler pode pré-checar pela consulta da
  entidade (`contrato.PodeFinalizar`). A ação confere tudo de novo.

---

## 2. A entidade: só ação e consulta são públicas

```csharp
public void Finalizar(string motivo, bool possuiDevolucaoEmAberto)
{
    if (!ValidarFinalizacao(possuiDevolucaoEmAberto))
        return;

    Situacao = ESituacaoContrato.Finalizado;
    MotivoFinalizacao = motivo;
    RegistrarHistorico(ETipoHistoricoContrato.Finalizado);
}

public bool PodeFinalizar => Situacao is ESituacaoContrato.Ativo;

private bool ValidarFinalizacao(bool possuiDevolucaoEmAberto)
{
    if (!PodeFinalizar)
    {
        AddNotification(nameof(Situacao), "Só um contrato ativo pode ser finalizado.");
        return false;
    }

    if (possuiDevolucaoEmAberto)
    {
        AddNotification(nameof(Situacao), "O contrato tem uma devolução em aberto.");
        return false;
    }

    return true;
}
```

- **Público:**
  - a **ação**, nomeada pelo fato de negócio. Retorno de integração também é ação
    (`RegistrarAprovacaoFiscal(retorno)`), e não `AtualizarSituacao`;
  - a **consulta pura** (`PodeX`, `MotivoXIndisponivel`, calculadas), que **não notifica**. O
    `Pode{Acao}` é a exceção escrita da regra de booleano (`naming-conventions.md` § 3.1).
- **Privado:** a guarda que notifica, o setter, e o auxiliar reaproveitado entre ações.
- Propriedade é `{ get; private set; }`. Setter público, `init` e `Atualizar*` genérico não existem:
  o `init` deixa um object initializer de fora pular o construtor e as invariantes de criação (§ 5).
- Mudança de situação é um método só: `AlterarSituacao(bool ativo)` chama `Ativar`/`Desativar`
  privados. O handler não escolhe qual chamar.

---

## 3. O agregado: a raiz orquestra

- **A filha muda só pela ação da raiz.** A raiz roda as guardas dela e delega ao método `internal`
  da filha. O handler nunca pega a filha da coleção para mudá-la, porque assim as guardas da raiz
  não rodam.
- **Repositório só da raiz.** Filha com CRUD próprio é outra raiz, e aí tem repositório.
- **Dado de filha:** a raiz resolve pela navegação, com o `Include` partindo dela.
- **Dado de outro agregado** (existência, contagem, unicidade): o handler busca e passa por
  parâmetro à ação ou à fábrica, que decide. Navegação entre agregados não existe, para que dois
  agregados não se misturem no mesmo `SaveChanges`.
- Referência entre agregados é por id explícito. FK de filha é shadow property
  (`database-standards.md`, *Shadow property*).
- Coleção: `coding-standards.md` § 6.

---

## 4. Validação em três lugares

| Onde | O quê |
|---|---|
| Validator do Command | a entrada: obrigatório, formato, tamanho do campo |
| `{Entidade}Validator`, ligado em `Validate()` | o dado que vale sempre: obrigatório, formato, faixa de negócio, e **toda filha ou VO que notifica** |
| Ação | a pré-condição de estado |

```csharp
public override bool Validate() => UseValidator(this, new ContratoValidator());
```

```csharp
RuleForEach(contrato => contrato.Parcelas).IsValid();
RuleFor(contrato => contrato.Endereco).IsValid();
```

- **VO ou filha que notifica e não está no validator da raiz tem a notificação engolida:** a raiz
  segue válida e é salva.
- Faixa de valor do negócio (desconto de 0 a 60) é da entidade, e não do validator do Command.
- Validator por ação (`FinalizarContratoValidator`, chamado dentro da ação) só quando a checklist
  da ação é longa.
- A falha não é removida: depois que a entidade fica inválida, ela continua inválida no escopo.
- `IsValid` roda `Validate()` a cada leitura. Não o consulte dentro de um laço.

---

## 5. Criação

- **Um jeito de criar:** construtor público com as invariantes de criação, chamado pelo Mapper do
  Domain. O estado inicial (histórico, primeira atividade) nasce nele.
- **Mais de um jeito:** fábrica estática na entidade, nomeada pelo fato (`Multa.ParaAvaria(...)`).
  É o *Static Creation Method* do Refactoring Guru.
- Construtor protegido, sem parâmetro, para o EF.
- Handler não faz `new` de entidade com regra.

---

## 6. Permissão

- Acesso à rota por perfil: `[Authorize]` ou policy.
- Regra que depende de quem faz (só o administrador troca a loja): a ação recebe o usuário e
  confere. O handler não repete.

---

## 7. Service, external e encadeamento

- **Service** existe para o trecho reaproveitado por 2 ou mais handlers.
  - Pode fazer I/O, mas **nunca chama `SaveChanges`**: quem salva é o handler.
  - Não decide regra que é de uma entidade ou de um VO.
  - É nomeado pelo **contexto** (`PedidoService.GerarPedidoCompraAvulsoAsync`), e nunca pelo
    método (`GerarPedidoCompraAvulsoService`).
- **External:** a interface fica no Domain e a implementação na Infra. Detalhe de transporte
  (`HttpStatusCode`, `System.Net`) não entra no Domain; a Infra traduz para um valor de domínio.
  O que cada resposta significa e o que o external devolve estão em `external-services.md`. Chamada
  que cria documento ou move dinheiro segue `idempotency.md`: chave gravada antes, timeout como
  desconhecido, e retomada.
- **`mediator.Send`** de Command só no Controller, no Worker e na reação de entrada
  (`validation-and-cqrs.md` § 16.1). Handler não chama handler.
- **Reação a fato:** `validation-and-cqrs.md` § 16. O que precisa ser atômico não é evento: é fluxo
  explícito no handler, num `SaveChanges` só.

---

## 8. Persistência

- A entidade que vai mudar é carregada **rastreada**, com o nome normal (`ObterPorIdAsync`). Leitura
  sem rastreio leva o nome `...AsNoTrackingAsync`.
- **Na quase totalidade dos casos, `SaveChangesAsync` basta:** a entidade carregada rastreada e
  mudada pela ação é gravada sozinha, inclusive a filha que veio pelo `Include` da raiz.
- **Exceção única:** a entidade lida sem rastreio por alguma restrição, e que precisa ser salva.
  Prefira recarregá-la rastreada. Não dando, o repositório expõe `void Atualizar(entidade)` — sem
  `Async`, porque `Update` só mexe no change tracker, em memória.
- **Auditoria de alteração que não grava não se conserta com `Atualizar`.** O defeito está em quem
  preenche a auditoria (o interceptor do `SaveChanges`), e se corrige lá.
- Laço do Worker: a lista sem rastreio serve só para percorrer. Cada item é recarregado rastreado e
  salvo, com `ClearTracking` no `finally`.

---

## 9. Comportamento que varia por tipo: Strategy + Provider

**Antes, triar o que varia:**

| Varia | Use |
|---|---|
| o comportamento, e cada braço é uma regra inteira | Strategy + Provider |
| só valor (percentual, prazo, limite) | parâmetro em tabela ou configuração |
| só credencial ou endpoint, com o mesmo comportamento | Factory que monta o cliente com o dado |
| o ciclo de vida | entidade + enum + método guardado (§ 2) |
| uma linha entre duas variantes | `if` na entidade |
| só qual ação da entidade aplicar, sem dependência | um delegate (`Action<T>`) |
| não se escolhe uma: todas rodam em ordem, e qualquer uma pode parar as seguintes | Chain of Responsibility (§ 9.1) |

- **A chave é um tipo de comportamento** (enum do domínio), e nunca um cliente, um programa ou um id
  de cadastro. O cadastro aponta para o tipo.
- **Política** é a variação pura, sem I/O. Pode ir como parâmetro da ação da entidade
  (`devolucao.Restituir(politica)`). **Estratégia** é a variação que faz I/O: fica no handler ou no
  service, e a entidade recebe o resultado.
- **Estratégia:**
  - declara a chave que atende;
  - é sem estado e scoped;
  - não salva, e muda agregado só pela ação dele.
- **Provider:**
  - é o único lugar que escolhe;
  - monta o mapa uma vez, e **chave duplicada estoura na montagem**;
  - não tem cache nem regra.
- **Chave composta:** tupla de enums. Para **escolher** uma estratégia, nunca uma corrente de
  `Atende(...)`: ela fica com a primeira que responder sim, e a ordem de registro no DI decide calada
  qual ganha. Quando não se escolhe uma, e todas rodam numa ordem declarada, é a Chain do § 9.1.

```csharp
public class QuitacaoStrategyProvider : IQuitacaoStrategyProvider
{
    private readonly IReadOnlyDictionary<EModalidade, IQuitacaoStrategy> _estrategias;

    public QuitacaoStrategyProvider(IEnumerable<IQuitacaoStrategy> estrategias)
    {
        _estrategias = estrategias.ToDictionary(estrategia => estrategia.Modalidade);
    }

    public IQuitacaoStrategy Obter(EModalidade modalidade)
    {
        return _estrategias.TryGetValue(modalidade, out var estrategia)
            ? estrategia
            : throw new NotSupportedException($"Quitação sem estratégia para {modalidade}");
    }
}
```

- Cada estratégia é registrada com `AddScoped<IQuitacaoStrategy, ...>()`, e o Provider também é
  scoped.
- **Chave que veio do código sem estratégia** é erro de programação, e lança a exceção acima.
- **Chave que veio de dado** (cadastro apontando para um tipo desativado) é falha de negócio: o
  Provider oferece um `TentarObter`, e quem chama notifica. Nunca devolva `default` calado.
- **Teste de cobertura:** monte o Provider pelo registro real do projeto (o mesmo `Add...` do Ioc)
  e confira que todo valor do enum tem uma estratégia. É ele que pega a estratégia esquecida no
  registro, antes da produção.

### 9.1. Etapas em ordem, e qualquer uma pode parar: Chain of Responsibility

A Strategy escolhe **uma** implementação. Quando todas as etapas de uma lista rodam, na ordem, e
qualquer uma pode parar as seguintes, é Chain. O caso típico são as verificações antes de uma
compra, em que algumas são cobradas e não devem rodar depois de outra ter barrado.

- **A ordem é declarada**, e nunca a do registro no DI. Ela vem da configuração por ponto de
  chamada (`"Elegibilidade:Pontos": { "AvaliacaoCD": ["ImeiAnatel"] }`) ou de uma lista no código.
  Ponto sem lista não roda etapa nenhuma. A etapa gratuita vem antes da cobrada.
- **Link:**
  - declara a chave que atende (enum do domínio);
  - é sem estado e scoped;
  - faz o I/O da própria verificação e devolve um VO de parecer;
  - não conhece o próximo, não salva e não muda agregado.
- **Chain:**
  - é o Client do GoF: recebe `IEnumerable<I{Contexto}Link>`, monta o mapa uma vez e percorre a
    lista. **Sem Provider separado**: ela é o único lugar que resolve os links;
  - chave duplicada estoura na montagem, e chave configurada sem link estoura com o nome
    (`NotSupportedException`): é erro de programação;
  - **para no primeiro que não libera**, e devolve um VO de análise do contexto
    (`AnaliseElegibilidade`: o que rodou, qual etapa barrou e por quê), e não um `Result` genérico;
  - pode fazer I/O e não salva, como o Service do § 7.
- **A entidade decide o destino:** o handler passa a análise como parâmetro da ação (§ 1), e é a
  ação que transforma "barrou" em divergência, devolução ou notificação.
- **Encadeada com `SetNext`**, em que cada elo transforma e repassa ao próximo, só quando cada elo
  de fato muda o que o seguinte recebe. Para verificar e parar, a lista basta.
- **Teste:**
  - roda na ordem da configuração;
  - curto-circuito: a etapa depois da que barrou não é chamada, e é esse o teste que protege a
    consulta cobrada;
  - etapa não aplicável passa adiante;
  - chave duplicada e chave sem link estouram.

---

## 10. Refatoração: o que se aplica e o que se propõe

- **Na rota que a tarefa já altera** (controller, command, validator, handler, ação, mapper): o
  que estiver fora deste arquivo é corrigido junto, sem mudar o contrato HTTP nem o comportamento,
  e com teste.
- **Fora dela:** vira proposta. Vai na descrição do PR, em "Fora do padrão, não tocado", e numa
  linha em "não copie" do mapa de moldes.
- Regra movida do handler para a entidade ganha o teste de entidade **antes** de sair do handler.

---

## 11. Teste

- A regra é provada no **teste da entidade**, sem mock: `Deve_{Resultado}_Quando_{Condicao}`.
- O teste do handler prova a orquestração: com a entidade inválida, `AddNotifications` é chamado e
  `SaveChangesAsync` não é.
