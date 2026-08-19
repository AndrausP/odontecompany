---
sprint: "3"
status: done
---

# Sprint 3

**Período:** 2026-08-17 → 2026-08-17
**Objetivo:** Fase 3 (Financeiro) do roadmap — faturamento particular e por convênio.
**Aprovado por (PO):** Product Owner — 2026-08-17

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 006 | Financeiro particular | done | Broker (sem subagent — ver notas) |
| 007 | Financeiro convênios | done | Broker (sem subagent — ver notas) |

## Notas do Tech Lead

006 e 007 implementadas JUNTAS no mesmo módulo `Billing` — compartilham 100% do agregado
`Fatura`/`Parcela` (`TipoFatura` distingue particular de convênio no mesmo agregado, não dois
agregados paralelos). Continuação da sessão sem subagent (limite de gasto atingido antes da
sprint 2), mesmo padrão de auto-revisão.

## Retrospectiva

**Entregue:** módulo `Billing` completo — `Fatura` (particular parcelada até 12x sem perda de
centavo, ou convênio via ACL), `Convenio`, comissão de dentista calculável em qualquer fatura,
idempotência real contra reentrega de evento (`ExistsByAgendamentoIdAsync` + índice único
parcial), tradutor `ConsultaConcluidaEventHandler` pronto pra quando o worker de Outbox existir.
Build 0 erro/0 aviso, **183/183 testes** na solução completa (Identity 39 + Patients 38 +
Scheduling 51 + Records 29 + Billing 26).

**Padrão novo consolidado:** Anti-Corruption Layer via porta genérica (`IConvenioAdapter`) +
implementação de referência sem integração real — permite todo o resto do sistema (persistência,
idempotência, RBAC) ser construído e testado ANTES de qualquer credencial/API de operadora
existir; troca de adapter real é mudança isolada no DI. Índice único PARCIAL (`HasFilter`) como
ferramenta de idempotência de evento — mais forte que checar só na Application, sobrevive a
corrida de reentrega concorrente.

**Risco real da sprint:** mesma ressalva da sprint 2 — execução sem par Dev Backend/QA dedicado.
Auto-revisão pegou 1 gap (validators faltando em 3 commands, não bug funcional) desta vez, não um
bug de segurança/concorrência como nas sprints anteriores — mas a amostra é pequena, não é
garantia de que não há nada mais sério esperando revisão externa.

**Riscos técnicos conhecidos que ficam para depois (dívida documentada, não bloqueio):**
- Worker de Outbox (MassTransit/RabbitMQ) continua pendente desde a task 004 — acumula mais um
  consumidor esperando (`ConsultaConcluidaEventHandler`) sem bus real rodando.
- `ManualConvenioAdapter` não integra com nenhuma operadora real.
- Sem geração real de boleto bancário.
- Migration EF Core gerada pra todos os módulos em 2026-08-17 — validada no design-time, ainda não aplicada contra Postgres real.
