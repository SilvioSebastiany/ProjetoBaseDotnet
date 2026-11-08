---
paths:
  - "**/*.cs"
---

> Copiado de `ferramental-dev/padrao/backend` em 2026-11-08. Para atualizar, copie de novo e confira o `git diff`.

# Extensões .NET

> Leia este arquivo antes de criar/alterar: método utilitário ou extensão.

> **⚠️ VERIFICAÇÃO OBRIGATÓRIA:** Antes de implementar qualquer método utilitário, conversão, validação ou manipulação de tipos, **VERIFIQUE SE JÁ NÃO EXISTE**, nesta ordem: na BCL do .NET, num pacote que o projeto já referencia, e na pasta de extensões do próprio projeto. Evite reinventar a roda!

## Onde mora

Extensão genérica (que não conhece o domínio) mora num lugar só por solução: o projeto
`CrossCutting`, na pasta `Extensions/`, uma classe estática por tipo estendido
(`StringExtensions`, `EnumerableExtensions`, `DateOnlyExtensions`). Extensão que conhece o domínio
não é extensão: é método da entidade ou do VO.

Antes de criar, procure o nome e o verbo nas duas línguas:

```bash
grep -rn "static .*this " src --include=*.cs     # as extensões que o projeto já tem
```

---

## 🔴 IMPORTANTE: IsNullOrEmpty() - Padrão Obrigatório

A BCL tem `string.IsNullOrEmpty`, mas não tem o equivalente para coleção. O projeto tem **um**,
em `EnumerableExtensions`, e toda verificação de lista nula ou vazia passa por ele:

```csharp
public static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> source) =>
        source switch
        {
            null => true,
            ICollection<T> c => c.Count == 0,
            IReadOnlyCollection<T> c => c.Count == 0,
            _ => !source.Any(),
        };
}
```

### ❌ NÃO FAÇA ISSO:

```csharp
// ❌ ERRADO - Não usar .Any() para verificar se lista tem itens
if (lista != null && lista.Any())

// ❌ ERRADO - Não usar verificação manual de nulo + Count
if (lista == null || lista.Count == 0)

// ❌ ERRADO - Não usar is null separado
if (lista is null || !lista.Any())

// ❌ ERRADO - Pattern matching sem o extension method
if (lista is { Count: > 0 })
```

### ✅ FAÇA ISSO:

```csharp
// ✅ CORRETO - Usar IsNullOrEmpty() para TODAS as verificações de listas/coleções
if (lista.IsNullOrEmpty())
    return; // lista é nula OU vazia

if (!lista.IsNullOrEmpty())
    ProcessarItens(lista); // lista tem itens
```

---

## O que a BCL já tem, e se escreve à mão sem precisar

| Precisa de | Use |
|------|----------|
| Dividir lista em lotes | `Enumerable.Chunk(tamanho)` |
| Filtro condicional em query | `if (filtro) query = query.Where(...)`; uma extensão `WhereIf` só se o padrão se repetir em mais de três queries |
| Primeiro/último dia do mês | `new DateOnly(d.Year, d.Month, 1)` e `.AddMonths(1).AddDays(-1)` |
| Remover acentos | `string.Normalize(NormalizationForm.FormD)` e filtrar `UnicodeCategory.NonSpacingMark` |
| Só dígitos | `string.Concat(s.Where(char.IsAsciiDigit))` |
| Descrição de enum | `[Display(Name = "...")]` lido por reflexão numa extensão `GetDisplayName()`, criada uma vez |
| Formatar moeda | `valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))` |

---

## 📋 Checklist de Revisão

Antes de criar qualquer método utilitário, verifique:

- [ ] Precisa verificar se lista/coleção é nula ou vazia? → Use `IsNullOrEmpty()`
- [ ] A BCL já faz? → Use a BCL (tabela acima).
- [ ] O projeto já tem uma extensão parecida? → Use ou estenda a que existe, em vez de criar outra.
- [ ] A extensão conhece o domínio? → Não é extensão: é método da entidade ou do VO.
