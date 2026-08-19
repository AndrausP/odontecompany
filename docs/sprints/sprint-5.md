---
sprint: "5"
status: done
---

# Sprint 5

**Período:** 2026-08-17 → 2026-08-17
**Objetivo:** Fase 5 (Rede) do roadmap — hierarquia multi-unidade e estoque. Última fase do
roadmap original.
**Aprovado por (PO):** Product Owner (decisão do usuário, ver docs/tasks/009) — 2026-08-17

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 009 | Rede: múltiplas unidades + estoque | done | Broker (sem subagent — ver notas) |

## Notas do Tech Lead

Escopo explicitamente reduzido em relação ao critério de aceite original (sem schema-per-tenant
real, RBAC de unidade só em profundidade no módulo Scheduling) — decisão consciente de entregar
uma extensão ADITIVA e testada da hierarquia de tenancy em vez de uma migração de isolamento
físico que não dá pra validar sem Postgres real. Ver docs/tasks/009-rede-multi-unidade.md.

## Retrospectiva

**Entregue:** módulo `Tenancy` (hierarquia `Unidade`), `UnidadeId` opcional propagado por
Scheduling (`Profissional`/`Sala`) e Identity (`User`, claim JWT `unidade_id`), RBAC de unidade
aplicado em `ListAgendamentosQuery` (Recepcao só vê a própria unidade), módulo `Estoque`
completo (`ItemEstoque`, entrada/saída sem nunca ficar negativo). Build 0 erro/0 aviso,
**224/224 testes** na solução completa — plataforma fecha as 5 fases do roadmap original com
8 módulos de domínio.

**Gap real descoberto e fechado (não fazia parte do escopo original da task 009):** o módulo
Scheduling (task 004) nunca teve endpoint pra criar `Profissional`/`Sala` — só existia
`GetByIdAsync` nos repositórios, nenhum `AddAsync`, nenhum command, nenhum controller. Isso só
apareceu porque tentar dar `UnidadeId` a essas entidades exigiu primeiro ter como criá-las de
verdade. `ProfissionaisController`/`SalasController` novos fecham a lacuna.

**Padrão consolidado desde a task 008 (lição aplicada proativamente):** todo módulo novo desta
sprint (Tenancy, Estoque) já nasceu com teste de infraestrutura real (`UseInMemoryDatabase`, sem
mock) desde o primeiro commit — não depois de um bug aparecer. Um desses testes (Estoque) achou
um bug no PRÓPRIO teste (esqueci de setar o tenant ambiente antes da query), não no código —
mas só achou porque o teste existia.

**Risco real da sprint:** mesma ressalva das sprints 2-4 — sem par Dev Backend/QA dedicado.
Cinco sprints seguidas sem QA externo é uma sequência longa; recomenda-se fortemente uma
passada de QA real (ou usuário revisando manualmente pontos críticos: RBAC, isolamento de
tenant, concorrência) antes de qualquer deploy de produção.

**Riscos técnicos conhecidos que ficam para depois (dívida documentada, não bloqueio):**
- Sem schema-per-tenant real (só shared-schema + `UnidadeId` opcional).
- RBAC de unidade só em Scheduling — Patients/Records/Billing/Estoque não filtram por unidade
  do usuário automaticamente ainda.
- Migrations `InitialCreate` geradas pros 7 módulos com DbContext em 2026-08-17 (ver
  docs/tasks/009-rede-multi-unidade.md) — validadas no design-time, ainda não aplicadas contra
  Postgres real.
- Demais dívidas já documentadas em sprints 1-4 continuam (worker de Outbox, integração de
  convênio real, object storage real, KMS real).

## Fechamento do roadmap original

Todas as 9 tasks do roadmap do doc de arquitetura (`Arquitetura-Plataforma-Odontologica.pdf`)
estão `done`: 001-004 (Núcleo), 005 (Clínico), 006-007 (Financeiro), 008 (Inteligência), 009
(Rede). Ver docs/modules.md pro estado de cada módulo e docs/decisions.md pro histórico
completo de decisões técnicas.
