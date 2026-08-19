---
task: "008"
sprint: "4"
status: done
---

# 008 — Inteligência: relatórios, BI e dashboards (Fase 4)

**Sprint:** docs/sprints/sprint-4.md
**Critério de aceite:** Projeções CQRS dedicadas para leitura de relatório; queries pesadas
roteadas para read replicas do PostgreSQL, sem impacto no caminho de escrita.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Dashboard/relatórios agregando Pacientes+Agenda+Financeiro sem violar fronteira de módulo nem esperar por read replica real (não disponível nesta fase). |
| 2-5 | Broker (sem Tech Lead/Architect/QA dedicado — limite de gasto em subagent) | Composição via novas portas de leitura agregada (`I{Modulo}SummaryProvider`) em cada módulo existente, consumidas por um módulo `Reporting` novo (só Application+Contracts, sem Domain/Infrastructure — não há dado próprio, é puramente composição). |
| 6. Implementação | Broker (thread principal) | `Patients.Contracts.IPatientSummaryProvider`, `Scheduling.Contracts.IAgendaSummaryProvider`, `Billing.Contracts.IFaturamentoSummaryProvider` — cada um implementado no Infrastructure do próprio módulo (só `COUNT`/`GROUP BY` na própria tabela, TenantId explícito, zero JOIN cruzado). `Reporting.Application` compõe os 3 providers em paralelo (`Task.WhenAll`) num `GetDashboardResumoQuery`, mais 2 queries dedicadas (`GetFaturamentoPorPeriodoQuery`, `GetAgendaOcupacaoQuery`) que só repassam um provider individual. RBAC: Admin-only (dado agregado de negócio sensível). |
| 7. Teste | Broker (auto-revisão) | Build 0 erro/aviso, 2/2 testes do handler de composição (feliz + edge case de divisão por zero na taxa de conclusão). Auto-revisão foi além do handler: escreveu teste de infraestrutura real (`UseInMemoryDatabase`, sem mock) pra cada um dos 3 `SummaryProvider` novos — e um deles (`FaturamentoSummaryProviderTests`) expôs um bug REAL pré-existente desde a task 006: `FaturaConfiguration` mapeava a coleção `Parcelas` de um jeito que quebrava a construção do Model do EF Core (`InvalidOperationException` só na primeira query real), invisível pros 26 testes anteriores porque todos usavam `Mock<IFaturaRepository>`. Corrigido (`HasMany(f => f.Parcelas)` em vez de `HasMany("_parcelas")`) + 2 `Include` string ajustados. Ver docs/knowledge/errors-aprendidos.md. 191/191 na solução completa após o fix (Identity 39 + Patients 40 + Scheduling 53 + Records 29 + Billing 28 + Reporting 2). |
| 8. Documentação | Broker | Registrado em docs/knowledge e docs/decisions.md. |

## Status

done — build 0 erro/aviso, 191/191 testes na solução completa (Identity 39 + Patients 40 +
Scheduling 53 + Records 29 + Billing 28 + Reporting 2). Desvio deliberado do critério de aceite
original, documentado:
- **Sem projeção CQRS persistida** — a composição é EM TEMPO REAL (3 queries paralelas nos
  módulos de origem a cada chamada), não uma tabela de projeção materializada sincronizada por
  evento. Motivo: sem worker de Outbox/bus de eventos rodando (dívida herdada da task 004), não
  há mecanismo de sincronização confiável pra manter uma projeção persistida atualizada. Escolha
  consciente: correto e simples agora > "projeção" que fica desatualizada silenciosamente.
- **Sem read replica real** — todas as queries agregadas apontam pro mesmo banco de escrita
  (`{Modulo}Db`, mesma connection string). Sem infraestrutura de réplica provisionada nesta fase.
  Se o volume/latência exigir, trocar a connection string das 3 implementações de
  `SummaryProvider` por uma variante read-only é a extensão natural — arquitetura já isola isso
  em `Infrastructure`, Application não muda.
- **`TaxaConclusao` não é ocupação de agenda contra capacidade teórica** — é
  Concluídos/Total-agendado. Não existe conceito de "expediente/capacidade total do dia" ainda
  (precisaria de configuração de clínica, fora de escopo).
- Migration EF Core gerada em 2026-08-17 (ver docs/tasks/009-rede-multi-unidade.md), validada no design-time — ainda não aplicada contra Postgres real (sem instância disponível nesta sessão).
- Sem par Dev Backend/QA dedicado (mesma situação das tasks 005-007).

## Notas

Depende do núcleo + financeiro (001-007) — relatórios cruzam Agenda/Financeiro/Prontuário
(prontuário NÃO entrou nesta primeira versão do dashboard — dado clínico sensível, agregação
dele em relatório fica pra quando houver requisito de produto específico, não implementado por
suposição).
