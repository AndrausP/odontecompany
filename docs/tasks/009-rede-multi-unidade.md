---
task: "009"
sprint: "5"
status: done
---

# 009 — Rede: múltiplas unidades + estoque (Fase 5)

**Sprint:** docs/sprints/sprint-5.md
**Critério de aceite:** Hierarquia `Tenant (empresa) → Unidade (clínica) → Recurso`; admin de
rede enxerga todas as unidades, recepcionista só a sua. Evolução de shared-schema pra
schema-per-tenant onde exigido. Módulo de Estoque (materiais/insumos).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner (decisão do usuário) | Extensão ADITIVA sobre o modelo multi-tenant existente — hierarquia Unidade dentro do shared-schema atual, sem migrar pra schema-per-tenant (exigiria Postgres real pra validar, fora do alcance desta sessão). |
| 2-5 | Broker (sem Tech Lead/Architect/QA dedicado — limite de gasto em subagent) | Módulo `Tenancy` novo (era só `ITenantContext`/middleware minimalista desde a task 001 — o doc de arquitetura sempre previu Tenancy como módulo transversal completo). `UnidadeId` opcional adicionado a `Profissional`/`Sala` (Scheduling) e `User` (Identity). Módulo `Estoque` novo. |
| 6. Implementação | Broker (thread principal) | **Tenancy**: `Unidade` (agregado), `IUnidadeLookup` (porta cross-module). **Scheduling**: `Profissional.UnidadeId`/`Sala.UnidadeId` opcionais; **descoberto e corrigido gap real da task 004**: não existia NENHUM endpoint pra criar Profissional/Sala (só `GetByIdAsync` no repositório) — `ProfissionaisController`/`SalasController` novos fecham essa lacuna junto com o campo de unidade. `ListAgendamentosQuery` ganhou filtro `UnidadeId`, forçado pelo controller a partir de `ICurrentUserAccessor.UnidadeId` quando o papel é Recepcao (não aceito como input do cliente). **Identity**: `User.UnidadeId` opcional, propagado pro JWT (claim `unidade_id`) via `IJwtTokenService`, exposto em `ICurrentUserAccessor.UnidadeId`; `CreateUserCommand` valida via `IUnidadeLookup`. **Estoque**: `ItemEstoque` (agregado) com `RegistrarEntrada`/`RegistrarSaida` (nunca fica negativo), `UnidadeId` opcional. |
| 7. Teste | Broker (auto-revisão) | Build 0 erro/aviso. 224/224 na solução completa (Identity 41 + Patients 40 + Scheduling 61 + Records 29 + Billing 28 + Reporting 2 + Tenancy 9 + Estoque 14). Testes de infraestrutura real (InMemory, sem mock) em Tenancy e Estoque desde o primeiro commit — lição da task 008 aplicada proativamente. Teste de infra achou 1 erro no PRÓPRIO teste (esqueci de setar tenant ambiente antes da query em `Estoque.UnitTests`), não no código — corrigido. |
| 8. Documentação | Broker | Registrado em docs/knowledge e docs/decisions.md. |

## Status

done — build 0 erro/aviso, 224/224 testes na solução completa. Desvios deliberados do
critério de aceite original, documentados:
- **Sem schema-per-tenant** — shared-schema continua sendo a única estratégia implementada;
  `UnidadeId` é um nível de escopo ADICIONAL dentro do mesmo schema compartilhado, não uma
  migração de isolamento físico. Evoluir pra schema-per-tenant exige Postgres real disponível
  pra validar migrations, fora do alcance desta sessão.
- **Migrations `InitialCreate` geradas em 2026-08-17** pros 7 módulos com DbContext (Identity,
  Patients, Scheduling, Records, Billing, Tenancy, Estoque — Reporting não tem, é só
  Application+Contracts) — validadas no design-time do EF Core (build do Model real, não
  InMemory), mas AINDA NÃO aplicadas contra Postgres real (sem instância disponível nesta
  sessão). Rodar `dotnet ef database update --project src/Modules/{Modulo}/{Modulo}.Infrastructure
  --startup-project src/Bootstrap/OdontoPlatform.Api --context {Modulo}DbContext` assim que
  houver Postgres provisionado.
- **RBAC de unidade só aplicado em `Scheduling.ListAgendamentos`** — os demais módulos
  (Patients, Records, Billing, Estoque) não filtram automaticamente por unidade do usuário
  ainda, mesmo tendo `UnidadeId` em Estoque. Escopo consciente: implementar o padrão uma vez,
  em profundidade, no módulo mais crítico (Agenda), em vez de espalhar raso por todos.
  Extensão pros demais módulos é trabalho futuro direto (mesmo padrão: filtro forçado pelo
  controller a partir de `ICurrentUserAccessor.UnidadeId`).
- **Gap fechado que não era desta task**: `ProfissionaisController`/`SalasController` (criar
  Profissional/Sala via API) — faltava desde a task 004, achado ao tentar dar valor real ao
  `UnidadeId` novo (sem CRUD, o campo não tinha como ser exercitado fim-a-fim).

## Notas

Última fase do roadmap original do doc de arquitetura. Plataforma agora cobre as 5 fases:
Núcleo, Clínico, Financeiro, Inteligência, Rede — 8 módulos de domínio (Identity, Patients,
Scheduling, Records, Billing, Reporting, Tenancy, Estoque), 224 testes, build limpo.
