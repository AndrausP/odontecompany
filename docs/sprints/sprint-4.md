---
sprint: "4"
status: done
---

# Sprint 4

**Período:** 2026-08-17 → 2026-08-17
**Objetivo:** Fase 4 (Inteligência) do roadmap — relatórios, BI e dashboards.
**Aprovado por (PO):** Product Owner — 2026-08-17

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 008 | Inteligência: relatórios, BI e dashboards | done | Broker (sem subagent — ver notas) |

## Notas do Tech Lead

Escopo ajustado conscientemente em relação ao critério de aceite original ("projeções CQRS
dedicadas" + "read replicas") — sem worker de Outbox/bus de eventos e sem infra de réplica
disponíveis, a entrega real foi composição em tempo real via portas de leitura agregada nos
módulos existentes. Ver docs/tasks/008-inteligencia-bi.md pro racional completo.

## Retrospectiva

**Entregue:** módulo `Reporting` (só Application+Contracts — sem dado próprio) compondo 3 novas
portas de leitura agregada (`IPatientSummaryProvider`, `IAgendaSummaryProvider`,
`IFaturamentoSummaryProvider`), 3 endpoints Admin-only (`/api/reports/dashboard`,
`/api/reports/faturamento`, `/api/reports/agenda`). Build 0 erro/0 aviso, **191/191 testes** na
solução completa.

**Bug real encontrado E corrigido nesta sprint:** ao escrever teste de infraestrutura real (não
mockado) pro `FaturamentoSummaryProvider`, apareceu um `InvalidOperationException` na construção
do Model do EF Core — `FaturaConfiguration` mapeava a coleção `Parcelas` de um jeito que criava
duas navegações conflitantes pro mesmo campo. Bug existia desde a task 006, invisível pros 26
testes de Billing anteriores porque nenhum deles instanciava o `DbContext` de verdade (só
`Mock<IFaturaRepository>`). Corrigido + documentado como padrão novo em
docs/knowledge/patterns.md ("todo módulo com DbContext precisa de ao menos um teste que construa
o Model de verdade").

**Padrão novo consolidado:** módulo "read-only puro" pode legitimamente ter só
Application+Contracts, sem Domain nem Infrastructure — quando não há invariante de negócio pra
proteger nem dado próprio pra persistir, forçar as 4 camadas de todo módulo anterior seria
over-engineering. Referenciar `*.Contracts` de MÚLTIPLOS módulos (Patients+Scheduling+Billing) é
esperado e correto pra um módulo cuja função é justamente compor leitura entre módulos — não
viola a regra de fronteira (que proíbe Domain/Infrastructure cruzado, não múltiplos Contracts).

**Risco real da sprint:** mesma ressalva das sprints 2-3 — sem par Dev Backend/QA dedicado. Mas
esta sprint é a primeira em que a auto-revisão pegou um bug de verdade (não só gap de
consistência) — reforça que "build limpo + testes passando" não é garantia nenhuma quando os
testes só mockam a camada de dados; testes de infraestrutura real continuam sendo o que
efetivamente pega esse tipo de problema.

**Riscos técnicos conhecidos que ficam para depois (dívida documentada, não bloqueio):**
- Sem projeção persistida — se o volume de dados crescer, compor 3 queries em tempo real a cada
  chamada de dashboard pode ficar caro; migrar pra projeção sincronizada por evento quando o
  worker de Outbox existir.
- Sem read replica real — mesma connection string de escrita usada pra leitura agregada.
- Todas as dívidas já documentadas em sprints 1-3 continuam (migrations, worker de Outbox,
  boleto real, integração de convênio real).
