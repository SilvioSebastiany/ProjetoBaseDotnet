---
paths:
  - "src/domain/**"
  - "src/infra.data/**"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Value Objects

> Leia este arquivo antes de criar/alterar: Value Object (verifique os VOs que o projeto já tem, em `Domain/ValueObjects/`, ANTES de criar).

> **⚠️ VERIFICAÇÃO OBRIGATÓRIA:** Antes de criar qualquer Value Object para documentos, contatos, dinheiro ou período, **VERIFIQUE SE JÁ NÃO EXISTE** no domínio do projeto. Evite duplicação de código!

## 🎯 Conceitos Básicos

### Classe Base `ValueObject`

Todos os Value Objects herdam da classe `ValueObject`, que herda `Validatable`
(`domain-foundation.md` §§ 2 e 3).

**Características principais:**
- ✅ Propriedade `IsValid` - indica se o objeto é válido
- ✅ Propriedade `IsInvalid` - indica se o objeto é inválido
- ✅ Propriedade `Notifications` - contém os erros de validação
- ✅ Método `Validate()` - executa a validação do objeto, com um validator do FluentValidation
- ✅ Suporte a **conversão implícita** de string
- ✅ Comparação pelo valor (`ComponentesDeIgualdade`), sem `Equals` e `GetHashCode` escritos à mão

### Forma padrão

```csharp
public sealed class Cnpj : ValueObject
{
    public string Numero { get; private set; }

    private Cnpj() { }

    public Cnpj(string valor)
    {
        Numero = string.Concat((valor ?? string.Empty).Where(char.IsAsciiDigit));
    }

    public string NumeroFormatado =>
        IsValid ? Convert.ToUInt64(Numero).ToString(@"00\.000\.000\/0000\-00") : Numero;

    public static implicit operator Cnpj(string valor) => new(valor);

    public override bool Validate() => UseValidator(this, new CnpjValidator());

    protected override IEnumerable<object> ComponentesDeIgualdade()
    {
        yield return Numero;
    }
}
```

```csharp
public class CnpjValidator : AbstractValidator<Cnpj>
{
    public CnpjValidator()
    {
        RuleFor(x => x.Numero)
            .Length(14).WithMessage("O CNPJ deve ter 14 dígitos.")
            .Must(DigitoVerificadorValido).WithMessage("O CNPJ é inválido.");
    }

    private static bool DigitoVerificadorValido(string numero) { /* ... */ }
}
```

- Guarda o valor **normalizado** (só dígitos, minúsculas, sem espaço) e formata na saída.
- O construtor sem parâmetro é privado e existe para o EF.
- Sem primary constructor (CLAUDE.md, Tipos e classes).

### Padrão de Uso

```csharp
// Conversão implícita
Cnpj cnpj = "11.222.333/0001-81";

// Instanciação direta
Cnpj cnpj = new Cnpj("11.222.333/0001-81");

// Verificação de validade
if (cnpj.IsValid)
{
    Console.WriteLine(cnpj.NumeroFormatado);
}
```

---

## Quando criar um VO

- **Dado com formato e validação conhecidos** (documento, e-mail, telefone, CEP, valor em dinheiro,
  período de competência) é VO na entidade, e nunca `string` com a validação repetida no validator.
- **O VO entra no validator da raiz** com `.IsValid()` (`domain-foundation.md` § 4). Fora dele, a
  notificação do VO é engolida e a raiz inválida é salva (`domain-modeling.md` § 4).
- **Persistência:** VO de um campo vira `HasConversion`; VO de mais de um campo vira `OwnsOne`, com
  prefixo nas colunas (`database-standards.md`), e não tabela própria.

---

## 🛠️ Tratamento de Erros

```csharp
var email = new Email("email-invalido");

if (email.IsInvalid)
{
    // Notificar e retornar default (NUNCA lançar exceção — ver Notification Context no CLAUDE.md)
    notificationContext.AddNotifications(email);
    return default;
}
```

---

## 📋 Checklist de Revisão

Antes de criar qualquer Value Object, verifique:

- [ ] O dado tem formato e validação conhecidos? → Procure em `Domain/ValueObjects/` antes de criar.
- [ ] Já existe? → Use o que existe, na entidade, e ligue no validator da raiz com `.IsValid()`.
- [ ] Não existe? → `sealed class` que herda `ValueObject`, com `Validate()` por um validator próprio e conversão implícita de string.
