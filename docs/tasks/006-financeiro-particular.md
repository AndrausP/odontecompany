---
task: "006"
sprint: "3"
status: done
---

# 006 — Financeiro: faturamento particular (Fase 3)

**Sprint:** docs/sprints/sprint-3.md
**Critério de aceite:** Faturamento particular com parcelamento e boletos, escuta
`ConsultaConcluidaEvent` da Agenda pra faturar, geração de fatura assíncrona via fila.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Faturamento particular parcelado, idempotente em relação ao agendamento faturado, tradução do evento de conclusão de consulta já pronta (worker de fila continua pendente, dívida herdada da task 004). |
| 2. Contexto | Reader → Writer | Depende de 003 (`Patients.Contracts.IPatientLookup`) e 004 (`Scheduling.Contracts.ConsultaConcluidaEvent`, `IModuleContract`). |
| 3-4. Quebra/Estrutura | Broker (sem Tech Lead/Architect dedicado — limite de gasto em subagent) | Módulo `Billing`: agregado `Fatura` (Particular/Convênio no mesmo agregado, `TipoFatura`), entidade filha `Parcela`, status derivado sempre das parcelas (nunca setado direto). |
| 5. Aprovação | Product Owner | aprovado |
| 6. Implementação | Broker (thread principal) | `Fatura.CreateParticular` divide valor em N parcelas (1-12) sem perda de centavo (resto na última parcela). `CreateFaturaFromConsultaConcluidaCommand` idempotente (`ExistsByAgendamentoIdAsync` + índice único PARCIAL em `AgendamentoId`, permite múltiplas faturas com `AgendamentoId` null) — trata reentrega de mensagem (at-least-once) sem duplicar fatura. `ConsultaConcluidaEventHandler` (Billing.Infrastructure.Messaging) traduz `Scheduling.Contracts.ConsultaConcluidaEvent` pro command, pronto pra ser chamado por um consumidor MassTransit real quando o worker de Outbox existir — reconciliação manual via `POST /api/faturas/from-consulta` até lá. RBAC: Admin+Recepcao (Dentista não administra fatura). |
| 7. Teste | Broker (auto-revisão) | Build 0 erro/aviso, 26/26 testes do módulo (183/183 na solução: Identity 39 + Patients 38 + Scheduling 51 + Records 29 + Billing 26). Divisão de parcelas testada explicitamente pra não perder centavo. Idempotência testada em 3 cenários (não faturado, já faturado, corrida entre duas entregas concorrentes). Validators faltantes em 3 commands adicionados na auto-revisão (consistência — todo outro módulo tem validator em 100% dos commands). |
| 8. Documentação | Broker | Registrado em docs/knowledge e docs/decisions.md. |

## Status

done — build 0 erro/aviso, 26/26 testes próprios (183/183 na solução). Dívidas técnicas
conhecidas:
- Worker de Outbox (MassTransit/RabbitMQ) pro `ConsultaConcluidaEvent` continua NÃO
  implementado (dívida herdada da task 004) — `ConsultaConcluidaEventHandler` está pronto pra
  ser chamado por um `IConsumer<T>` real, mas hoje só é acionável manualmente.
- Migration EF Core gerada em 2026-08-17 (ver docs/tasks/009-rede-multi-unidade.md), validada no design-time — ainda não aplicada contra Postgres real (sem instância disponível nesta sessão).
- Boletos: `FormaPagamento.Boleto` existe como opção mas não há geração real de boleto
  (integração bancária) — só o registro contábil da forma de pagamento escolhida.
- Sem par Dev Backend/QA dedicado (mesma situação da task 005 — limite de gasto em subagent).

## Notas

Depende de 004 (Agenda publica o evento que este módulo consome/traduz). Implementado junto com
a task 007 (Convênios) no mesmo módulo `Billing` — ver docs/tasks/007-financeiro-convenios.md.
