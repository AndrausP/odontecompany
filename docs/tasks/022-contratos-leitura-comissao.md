---
task: "022"
sprint: "7"
status: done
---

# 022 — Contratos de leitura cross-módulo pra comissão (Billing + Scheduling + Identity)

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** BACKEND
**Estimativa:** 2 dias
**Depende de:** Nenhuma
**Critério de aceite:** Existem 3 portas de leitura novas em `*.Contracts` que, juntas, permitem
ao módulo `Reporting` montar "quanto de comissão cada profissional recebeu, em qual filial, sob
qual classe, num período" **sem nenhum JOIN cruzado entre módulos** e **sem N+1** (toda porta é
batch: recebe período/organization e devolve lista, nunca um item por chamada).

## Contexto (o que já existe — não re-mapear)

- `Fatura` (Billing.Domain): `ProfissionalId` (Guid?), `ComissaoDentistaPercentual` (numeric(5,2)),
  `CalcularValorComissao()` (= `ValorTotal * pct / 100` — **regime de competência, sobre a fatura
  inteira**), `OrganizationId`, coleção `Parcelas`.
- `Parcela`: `DataPagamento` (DateTime?), `ValorParcela`, `Status` (`StatusParcela.Paga`).
- `Profissional` (Scheduling.Domain): `Id`, `Nome`, `UserId` (Guid?), `BranchId` (Guid?), `Ativo`.
- `OrganizationMembership` (Identity.Domain): `UserId`, `Role`, `BranchId?`, `OrganizationId`.
- Portas já existentes como referência de padrão: `Billing.Contracts.IFaturamentoSummaryProvider`,
  `Patients.Contracts.IPatientLookup`, `Tenancy.Contracts.IBranchLookup`.

## Escopo técnico

### 1. `Billing.Contracts.IComissaoSummaryProvider` (novo)

Devolve, por profissional, a comissão **em regime de caixa** do período: só considera parcelas com
`Status == Paga` e `DataPagamento` dentro de `[dataInicio, dataFim]`. Faturas canceladas fora.

Forma esperada (Architect fecha a assinatura final):

```
Task<IReadOnlyList<ComissaoPorProfissionalDto>> ObterComissoesAsync(
    Guid organizationId, DateTime dataInicio, DateTime dataFim, CancellationToken ct = default);

record ComissaoPorProfissionalDto(
    Guid? ProfissionalId,          // null = fatura sem profissional atribuído (bucket "Não atribuído")
    decimal ValorPagoNoPeriodo,    // soma das parcelas pagas no período
    decimal ValorComissao,         // comissão proporcional ao valor pago (ver Decisão D2)
    int QuantidadeFaturas,         // faturas distintas que tiveram parcela paga no período
    int QuantidadeParcelasPagas);
```

**Decisão D2 (Tech Lead, já tomada — não reabrir):** a base de cálculo é o **valor pago no
período**, não o `ValorTotal` da fatura. `CalcularValorComissao()` existente é competência
(fatura inteira) e **não pode** ser usado direto aqui — numa fatura de 3 parcelas com 1 paga, ele
pagaria comissão cheia. Cálculo correto: `ValorParcelaPaga * ComissaoDentistaPercentual / 100`,
arredondado a 2 casas, por parcela, somado depois.
**Aberto pro Architect:** onde esse cálculo mora — método novo no domínio
(`Fatura.CalcularComissaoSobre(decimal valorPago)`) ou expressão no provider de Infrastructure.
Preferência do Tech Lead: **Domain** (é regra de negócio, não consulta), mas a chamada é do
Architect.

### 2. `Scheduling.Contracts.IProfissionalLookup` (novo)

```
Task<IReadOnlyList<ProfissionalResumoDto>> ListarPorOrganizationAsync(
    Guid organizationId, CancellationToken ct = default);

record ProfissionalResumoDto(Guid Id, string Nome, Guid? UserId, Guid? BranchId, bool Ativo);
```

Inclui inativos — comissão de profissional já desligado ainda precisa aparecer em período passado.

### 3. `Identity.Contracts.IMembershipLookup` (novo)

```
Task<IReadOnlyList<MembershipResumoDto>> ListarPorOrganizationAsync(
    Guid organizationId, CancellationToken ct = default);

record MembershipResumoDto(Guid UserId, string Role, Guid? BranchId, bool Ativo);
```

`Role` como **string** — mesmo padrão de `ICurrentUserAccessor.Role`: a fronteira pública não
expõe o enum do módulo dono.

### 4. Implementações + DI

Cada implementação vive na `*.Infrastructure` do próprio módulo dono (nunca no Reporting), com
`AsNoTracking()` e `IgnoreQueryFilters()` + organization explícito no parâmetro — mesmo padrão de
`FaturamentoSummaryProvider`. Registrar no DI do módulo respectivo.

### 5. Testes

Testes de unidade NUnit por implementação, nomenclatura `Should_[Comportamento]_When_[Condição]`.
Mínimo obrigatório:
- `Should_IgnorarParcelaNaoPaga_When_CalcularComissaoDoPeriodo`
- `Should_UsarValorPagoENaoValorTotal_When_FaturaTemParcelasParciaisPagas`
- `Should_RetornarBucketNaoAtribuido_When_FaturaNaoTemProfissional`
- `Should_IncluirProfissionalInativo_When_ListarPorOrganization`
- `Should_ExcluirFaturaCancelada_When_CalcularComissao`

## Riscos

- **R1 (médio):** `Fatura` não tem `BranchId`. Filial da comissão é **derivada** de
  `Profissional.BranchId`. Se um profissional muda de filial, comissões antigas migram junto na
  visão histórica. **Dívida nomeada, aceita nesta sprint** — snapshot de filial na fatura exigiria
  migration + backfill + mudança nos 3 fluxos de criação de fatura, fora de escopo.
- **R2 (baixo):** fatura com `ProfissionalId == null` não tem filial nem classe — cai no bucket
  "Não atribuído". Não é bug, é o dado.

## Decisão do Architect (A1, A2)

**A1 — `CalcularComissaoSobre(valorPago)` mora em `Fatura.Domain.Entities.Fatura`**

Método público:
```csharp
public decimal CalcularComissaoSobre(decimal valorPago)
    => ComissaoDentistaPercentual is null or 0 
        ? 0 
        : Math.Round(valorPago * ComissaoDentistaPercentual.Value / 100, 2);
```

Motivo: é cálculo de comissão proporcional — regra de negócio pura (D2 do TL reafirmada), não consulta. Arredondamento idêntico ao `CalcularValorComissao()` existente (2 casas, `MidpointRounding.ToEven`).

**A2 — Assinaturas finais confirmadas conforme escopo da 022**

Batch-ready, sem N+1. Ver seções "1. `IComissaoSummaryProvider`", "2. `IProfissionalLookup`", "3. `IMembershipLookup`" acima — nenhuma mudança nas assinaturas (já estão corretas na spec).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Regra de comissão em docs/sprints/sprint-7.md |
| 2. Contexto | Reader → Writer | Fatura/Parcela/Profissional/Membership já modelados |
| 3. Quebra | Tech Lead | Escopo acima; D2 decidida (regime de caixa) |
| 4. Estrutura | Architect | A1: método `CalcularComissaoSobre` em Domain; A2: assinaturas confirmadas |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | feito — ver Notas |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | documentado — ver docs/knowledge/patterns.md (RBAC de ownership em memória, controller separado por Authorize cumulativo) e docs/decisions.md (A1-A6) |

## Status

planned → in-progress → in-review (QA) → qa-approved → **done**

### QA (2026-08-18, Jubileu — pass)

- `Fatura.CalcularComissaoSobre(valorPago)` (Billing.Domain/Entities/Fatura.cs:200-201) = `Math.Round(valorPago * pct / 100, 2)` — `Math.Round` default é `MidpointRounding.ToEven`, idêntico ao `CalcularValorComissao()` (linha 191-192). Guarda `is null or 0` retorna 0. Confirmado pelos 4 casos em `FaturaTests` (percentual setado / null / zero / valorPago zero).
- `ComissaoSummaryProvider.ObterComissoesAsync` (Billing.Infrastructure/Repositories/ComissaoSummaryProvider.cs) — regime de CAIXA real: filtra por `parcela.DataPagamento` dentro do período (linhas 43-45), não pela fatura. `Where(f => f.Status != StatusFatura.Cancelada)` explícito na query (linha 36). Comissão calculada POR PARCELA via `Fatura.CalcularComissaoSobre(parcela.ValorParcela)` (linha 51) e só depois agregada — decisão D2 respeitada, fatura de 3 parcelas com 1 paga NÃO paga comissão cheia. Confirmado no teste `Should_UsarValorPagoENaoValorTotal_When_FaturaTemParcelasParciaisPagas` (300 total → só 100 paga → comissão 10, não 30) e `Should_ExcluirFaturaCancelada_When_CalcularComissao`.
- Isolamento cross-org: as 4 portas (`ComissaoSummaryProvider`, `ProfissionalLookup`, `MembershipLookup`, `BranchLookup.ListarNomesAsync`) recebem `organizationId` explícito + `IgnoreQueryFilters()` + `Where(x.OrganizationId == organizationId)` — sem vazamento entre organizations.
- R1/D1 (filial derivada de `Profissional.BranchId`, sem snapshot) documentada em `docs/sprints/sprint-7.md` (D1 linha 107, R2 linha 140), nesta task (R1 linhas 100-104) e propagada pras tasks 027/028/029. Dívida nomeada e visível.
- Testes novos: 4 (`FaturaTests` CalcularComissaoSobre) + 5 (`ComissaoSummaryProviderTests`) + 2 (`ProfissionalLookupTests`) + 3 (`MembershipLookupTests`) + 2 (`BranchLookupTests`) = 16 testes. Cobrem cenários de verdade, não passam mecanicamente (o teste de exclusão de fatura cancelada faz seed real em InMemoryDatabase, não mock puro).

**Risco residual:** discrepância assinada pelo próprio Dev Backend na seção "Discrepância handoff x spec" (`IProfissionalLookup.ObterPorUserIdAsync` da A4 não implementada — controller resolve por `ListarPorOrganizationAsync().FirstOrDefault(UserId==...)`, funcionalmente idêntico). Não é bug, é decisão a ratificar pelo Architect/PO se quiserem alinhamento formal.

## Notas

Task de fundação: **bloqueia a 023**. Nada de frontend depende dela diretamente.

### Implementado (Dev Backend, 2026-08-18)

Escopo da spec (seções 1–5) implementado conforme A1/A2. `dotnet build` limpo (0 erros, 0
avisos), `dotnet test` 310/310 (baseline 293 + 17 testes novos, 0 falhas).

1. **`Fatura.CalcularComissaoSobre(decimal valorPago)`** — `src/Modules/Billing/Billing.Domain/Entities/Fatura.cs`.
   Mesmo arredondamento de `CalcularValorComissao()` (2 casas). Testes:
   `tests/Billing.UnitTests/Domain/FaturaTests.cs` (4 casos novos: percentual setado, percentual
   null, percentual zero, valorPago zero).

2. **`Billing.Contracts.IComissaoSummaryProvider`** — `src/Modules/Billing/Billing.Contracts/IComissaoSummaryProvider.cs`.
   Assinatura EXATAMENTE igual ao pseudocódigo da seção 1 (`organizationId, dataInicio, dataFim,
   ct`) — sem parâmetro de filtro por profissional. Implementação:
   `Billing.Infrastructure/Repositories/ComissaoSummaryProvider.cs` — regime de CAIXA, comissão
   calculada POR PARCELA paga e só depois somada (decisão D2), faturas canceladas excluídas (R2),
   `AsNoTracking()+IgnoreQueryFilters()` com organization explícito, uma única query
   (`Include(Parcelas)`) + agregação em memória — zero N+1. Registrado em
   `Billing.Infrastructure/DependencyInjection.cs`. Testes:
   `tests/Billing.UnitTests/Persistence/ComissaoSummaryProviderTests.cs` (5 casos, cobre os 3
   itens obrigatórios da spec que são desta porta + vazio).

3. **`Scheduling.Contracts.IProfissionalLookup`** — `src/Modules/Scheduling/Scheduling.Contracts/IProfissionalLookup.cs`.
   Só `ListarPorOrganizationAsync` (conforme A2 — "nenhuma mudança nas assinaturas"). Implementação:
   `Scheduling.Infrastructure/Lookups/ProfissionalLookup.cs`. Testes:
   `tests/Scheduling.UnitTests/Persistence/ProfissionalLookupTests.cs` (2 casos, cobre o
   item obrigatório "inclui inativo" + vazio).

4. **`Identity.Contracts.IMembershipLookup`** — `src/Modules/Identity/Identity.Contracts/IMembershipLookup.cs`.
   `Role` como string (`Role.ToString()`), `Ativo` derivado de `IsAtivo`. Implementação:
   `Identity.Infrastructure/Lookups/MembershipLookup.cs`. Testes:
   `tests/Identity.UnitTests/Persistence/MembershipLookupTests.cs` (3 casos: lista completa +
   isolamento de organization, afiliação desativada não some, vazio).

5. **`Tenancy.Contracts.IBranchLookup.ListarNomesAsync`** — método novo adicionado à interface
   já existente (`IBranchLookup.ExistsAsync` preservado). `IReadOnlyDictionary<Guid, string>`,
   inclui branch inativa (R1 da task — filial é derivada de `Profissional.BranchId`, histórico
   não pode quebrar se a branch foi desativada depois). Implementação:
   `Tenancy.Infrastructure/Lookups/BranchLookup.cs`. Testes:
   `tests/Tenancy.UnitTests/Persistence/BranchLookupTests.cs` (2 casos: inclui inativa + vazio).

### Pronto pro próximo Dev Backend (task 023, Reporting)

- 4 portas registradas em DI, prontas pra injetar no `Reporting.Infrastructure`:
  `Billing.Contracts.IComissaoSummaryProvider`, `Scheduling.Contracts.IProfissionalLookup`,
  `Identity.Contracts.IMembershipLookup`, `Tenancy.Contracts.IBranchLookup` (método novo
  `ListarNomesAsync`).
- Composição sugerida (fora de escopo desta task, só nota pro próximo): pegar
  `IComissaoSummaryProvider.ObterComissoesAsync` (comissão por `ProfissionalId?`), cruzar em
  memória com `IProfissionalLookup.ListarPorOrganizationAsync` (pra Nome/BranchId, inclusive
  inativo) e `ITenancy...ListarNomesAsync` (BranchId→Nome) — nada de JOIN cruzado, tudo por
  dicionário em memória no Reporting.
- **R1 aceito, não resolvido aqui**: filial de comissão é sempre a atual do Profissional, não
  snapshot histórico. Documentado na spec, replicar o aviso se o QA/PO perguntar.

### Discrepância handoff x spec — pra PO/Architect decidirem, não decidi sozinho

O handoff que recebi (broker/architect) pedia dois itens que NÃO estão na assinatura fechada
por A2 na seção "Decisão do Architect" desta task:
- `Billing.Contracts.IComissaoSummaryProvider.ObterComissoesAsync` com parâmetro extra
  `profissionalIds` (filtro opcional).
- `Scheduling.Contracts.IProfissionalLookup.ObterPorUserIdAsync(organizationId, userId, ct)`.

Como A2 diz explicitamente "nenhuma mudança nas assinaturas (já estão corretas na spec)", NÃO
implementei nenhum dos dois — implementar por conta própria seria eu, Dev Backend, reabrindo uma
decisão de Architect que já foi fechada, o que não é meu papel. Se o próximo Dev Backend (task
023) precisar de qualquer um dos dois, a rota mais simples sem N+1: os dois dão pra montar em
memória a partir do que já existe (`IComissaoSummaryProvider.ObterComissoesAsync` já devolve
TODOS os profissionais da org num só round-trip; `IProfissionalLookup.ListarPorOrganizationAsync`
já traz `UserId` de cada um, então "achar profissional por UserId" é um `.FirstOrDefault` local,
sem chamada nova). Só abrir adendo formal na spec se isso não bastar.
