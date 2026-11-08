---
paths:
  - "**/*.cs"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Naming Conventions

> Leia este arquivo antes de criar/alterar: nome de classe, rota, coluna, constante ou enum.

Convenções de nomes para manter consistência entre projetos .NET
seguindo Clean Architecture.

---

## 1. Linguagem do Domínio

- Conceitos de domínio (Entities, Commands, Properties) em **português sem acentos**.
  - `Pedido`, `Cliente`, `AdicionarProdutoCommand`, `DataInclusao`
- Sem `ç` ou acentos no código:
  - `Descricao` (não `Descrição`)
  - `Preco` (não `Preço`)
- Termos técnicos de framework permanecem em inglês:
  - `Handler`, `Controller`, `Repository`, `Request`, `Response`.
- **Nome de padrão de projeto também é termo técnico, e fica em inglês:** `Strategy`, `Provider`,
  `Factory`, `Builder`, `Policy`, `Chain`, `Link`. Traduzido (`Estrategia`, `Provedor`), ele some da busca de quem
  procura o padrão pelo nome, e destoa do resto do código .NET. O nome do domínio continua em português:
  - `IQuitacaoStrategy`, `QuitacaoBoletoStrategy`, `QuitacaoStrategyProvider`,
    `StrategyInjection.AddStrategies`;
  - nunca `IQuitacaoEstrategia` nem `ProvedorQuitacao`.
- **Chain of Responsibility** (`domain-modeling.md` § 9.1) segue a mesma regra:
  - a corrente é `I{Contexto}Chain` / `{Contexto}Chain`, e cada elo é `I{Contexto}Link` /
    `{Regra}Link`: `IElegibilidadeChain`, `ElegibilidadeChain`, `IElegibilidadeLink`,
    `ImeiAnatelLink`;
  - o registro é `ChainInjection.AddChains`, chamado de dentro do `AddServices` do Ioc;
  - as pastas são `Chains/{Contexto}`, `Chains/{Contexto}/Links` e `Interfaces/Chains/{Contexto}`;
  - o elo **nunca** se chama `Handler`: na casa, `Handler` é do Mediator, e um
    `ImeiAnatelHandler` seria lido como handler de command.
- Texto corrido (comentário, mensagem, documentação) segue em português: "a estratégia da origem".

---

## 2. Padrões Gerais

- **Classes / Structs:** PascalCase  
  - `Produto`, `PedidoService`, `CalculoPrecoService`
  - **NUNCA usar `record`** - sempre preferir `class` para DTOs, Responses, Requests e Commands
- **Métodos:** PascalCase  
  - `CalcularDesconto`, `AprovarPedido`
- **Propriedades:** PascalCase  
  - `DataInclusao`, `UsuarioInclusao`
- **Variáveis Locais / Parâmetros:** camelCase  
  - `pedido`, `clienteId`, `valorTotal`
- **Campos privados:** `_camelCase`  
  - `_pedidoRepository`, `_uow`
- **Constantes:** UPPER_SNAKE_CASE  
  - `VALOR_MINIMO_PEDIDO`, `PRAZO_PADRAO_ENTREGA_DIAS`

---

## 3. Interfaces e Enums

- **Interfaces:** prefixo `I`  
  - `IProdutoRepository`, `IPedidoService`, `IUnitOfWork`
- **Enums:** prefixo `E`  
  - `EStatusPedido`, `ETipoPagamento`

---

## 3.1 Booleano não leva prefixo (OBRIGATÓRIO)

O nome do booleano **já é** a afirmação — `ClienteAtivo` **é** o cliente ativo, e quem lê espera
`true`. O prefixo não acrescenta informação, só alonga: quem quer o caso falso nega
(`!ClienteAtivo`), e é a negação que carrega a intenção.

Vale para propriedade, campo, variável local, parâmetro e método que devolve `bool`. Nenhum prefixo
é aceito — nem os em inglês (`Is`, `Has`, `Can`, `Should`), nem os em português (`Eh`), nem `Flag`.

**Uma exceção, e só uma:** a consulta de capacidade da entidade, que fica ao lado da ação e responde
se ela pode acontecer agora, se chama `Pode{Acao}` (`PodeFinalizar`, `PodePausar`;
`domain-modeling.md` § 2). Ela não é estado: é a pergunta que a ação vai fazer, e o nome de um
estado (`Finalizavel`) não diz de qual ação se trata. Estado continua sem prefixo (`Ativo`, nunca
`EhAtivo`).

```csharp
// certo
public bool DevolucaoQuitacao { get; set; }
public bool ClienteAtivo { get; set; }
public bool PerguntaInicial { get; set; }
public bool Terminal { get; set; }

if (!status.Terminal)
    AvancarProcesso();

// errado
public bool EhDevolucaoQuitacao { get; set; }
public bool IsPerguntaInicial { get; set; }
public bool HasDocumentos { get; set; }
public bool CanAprovar { get; set; }
public bool FlagAtivo { get; set; }
```

> **Dívida existente:** há propriedades antigas com `Is*`. Renomear em Response **quebra o contrato com o
> front**, então não saia corrigindo: código novo nasce sem prefixo, e o antigo se ajusta quando
> houver PR que já mexa naquele contrato.

---

## 3.2 `Id` vai no fim do nome (OBRIGATÓRIO)

Propriedade que carrega identificador termina em `Id` — nunca começa. `IntegracaoId`, não
`IdIntegracao`.

Não é só estética: o nome fica ordenável e agrupável (todos os `*Id` juntos numa listagem), casa com
a convenção da coluna no banco (`integracao_id`, `{entidade}_id`) e é o que o EF espera por convenção
para relacionar navegação com chave estrangeira — `Pergunta` + `PerguntaId` o EF liga sozinho,
`IdPergunta` não.

```csharp
// certo
public long PerguntaId { get; private set; }
public long? IntegracaoId { get; private set; }
public string DocumentoGedId { get; set; }

// errado
public long IdPergunta { get; private set; }
public long? IdIntegracao { get; private set; }
public string IdDocumentoGed { get; set; }
```

Vale para propriedade, campo, variável local e parâmetro.

> **Exceção — contrato de terceiro:** DTO que espelha resposta de sistema externo mantém o nome que o
> terceiro usa (ex.: `IdRevision` na resposta de um GED de terceiro). Renomear ali quebra a desserialização, e
> `[JsonPropertyName]` só para satisfazer a convenção esconde de quem lê qual é o campo real do
> parceiro.

> **Dívida existente:** há propriedades antigas com `Id` na frente, algumas em Response e Command que
> o front consome — renomear quebra o contrato. Código novo nasce certo; o antigo se ajusta quando
> houver PR que já mexa naquele contrato.

---

## 4. DTOs e Commands

- DTOs de entrada: sufixo `Request`  
  - `CriarPedidoRequest`, `AtualizarClienteRequest`
- DTOs de saída: sufixo `Response`  
  - `PedidoResponse`, `ClienteResponse`
- Commands: sufixo `Command`  
  - `AdicionarProdutoCommand`, `AprovarPedidoCommand`

---

## 5. URLs de API

- Sempre em **kebab-case**.
- Sempre no plural para recursos:
  - `/produtos`, `/clientes`, `/pedidos-abertos`
- Ações específicas:
  - `/pedidos/{id}/aprovar`
  - `/usuarios/{id}/confirmar-email`

(Mais detalhes em `.claude/rules/api-standards.md`.)

---

## 6. Constantes de Domínio

- Constantes globais por contexto:
  - `PedidoConstants`, `StatusConstants`
- Uso:

```csharp
public static class PedidoConstants
{
    public const int PRAZO_PADRAO_ENTREGA_DIAS = 7;
    public const decimal VALOR_MINIMO_PEDIDO = 50.00m;
}
```
