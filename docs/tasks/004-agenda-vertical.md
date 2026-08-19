---
task: "004"
sprint: "1"
status: done
---

# 004 — Agenda: vertical completo (domínio → API)

**Sprint:** docs/sprints/sprint-1.md
**Critério de aceite:** Agregado `Agendamento` com invariantes de não-sobreposição de horário
(RowVersion otimista + lock curto no Redis ao confirmar), disponibilidade servida por cache,
publica `ConsultaConcluidaEvent` via Outbox. Módulo de maior tráfego — serve de molde CQRS pros
demais.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Agenda como módulo de maior tráfego do sistema — vertical completo domínio→API, servindo de molde CQRS pros demais módulos. |
| 2. Contexto | Reader → Writer | Depende de 001 (tenancy), 002 (auth), 003 (paciente — via `IPatientLookup`). |
| 3. Quebra | Tech Lead | Agregado `Agendamento` com máquina de estados, concorrência, lock de slot, cache de disponibilidade, Outbox pro evento de conclusão. |
| 4. Estrutura | Architect | Estrutura desenhada antes da implementação — decisões registradas em docs/decisions.md (concorrência otimista + lock Redis, Outbox pattern, Profissional como entidade local, cache de disponibilidade). |
| 5. Aprovação | Product Owner | aprovado |
| 6. Implementação | Dev Backend | Módulo `Scheduling` completo. Agregado `Agendamento` com máquina de estados (Agendado→Confirmado→Concluido, ou →Cancelado), VO `PeriodoHorario` (início<fim, duração mínima 15min). Concorrência otimista via `xmin` do Postgres (não `RowVersion byte[]` da spec original do Architect — decisão própria, xmin é padrão nativo Npgsql). Lock Redis curto (TTL 5s, chave tenant+profissional+minuto) pra serializar corrida de confirmação/criação. Cache de disponibilidade (Redis, TTL 1h, invalidado em Create/Confirmar/Cancelar, não em Concluir). `ConsultaConcluidaEvent` via Outbox na mesma transação do SaveChanges que conclui o agendamento (worker de publicação MassTransit/RabbitMQ NÃO implementado — dívida técnica conhecida, pendente pra quando task 006/Financeiro definir o consumidor). Fronteira de módulo: só referencia `Patients.Contracts` via novo `IPatientLookup`, nunca `Patients.Domain`/`Infrastructure`. RBAC: Dentista gerencia só a própria agenda (`Profissional.UserId`), Admin/Recepcao qualquer uma do tenant. |
| 7. Teste | QA | Achou e confirmou fix de 2 bugs: (1) race real na criação — `CreateAgendamentoCommandHandler` sem lock permitia dois `Agendado` sobrepostos, corrigido adicionando o mesmo lock Redis do Confirmar + revalidação de sobreposição dentro do lock; (2) RBAC sem ownership — Dentista conseguia mexer na agenda de outro dentista, corrigido com `AgendaOwnershipGuard` novo aplicado em Confirmar/Cancelar/Concluir (não em Create, pendência de produto documentada). Confirmou via grep zero violação de fronteira de módulo. Build 0 erro/0 aviso. Testes finais da solução: 128/128 (Identity 39 + Patients 38 + Scheduling 51). |
| 8. Documentação | Writer | Registrado em `docs/knowledge/errors-aprendidos.md` (race de criação, RBAC sem ownership), `docs/knowledge/patterns.md` (lock serializa + revalidação garante invariante, xmin nativo, `IPatientLookup` como porta cross-module), `docs/knowledge/business-rules.md` (máquina de estados, RBAC Scheduling, ConsultaConcluidaEvent), `docs/decisions.md` (xmin vs RowVersion). |

## Status final

done — build 0 erro/aviso, 51/51 testes próprios (128/128 na solução completa). Dívida técnica
conhecida: worker de publicação do Outbox (MassTransit/RabbitMQ) ainda não implementado, fica
pendente pra task 006 (Financeiro). Sprint 1 completa (todas as 4 tasks done).

## Notas

Depende de 001, 002, 003 (agenda referencia paciente e profissional).
