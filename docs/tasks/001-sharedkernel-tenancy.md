---
task: "001"
sprint: "1"
status: done
---

# 001 — SharedKernel + fundação de tenancy

**Sprint:** docs/sprints/sprint-1.md
**Critério de aceite:** Solução `OdontoPlatform.sln` existe com `src/Shared/SharedKernel`,
`src/Shared/Infrastructure.Common`, `src/Shared/Contracts.Abstractions` e
`src/Bootstrap/OdontoPlatform.Api`. `SharedKernel` expõe `AggregateRoot`, `Entity`, `ValueObject`,
`IDomainEvent`. `Infrastructure.Common` expõe `ITenantContext`, middleware de resolução de tenant
e extensão para `HasQueryFilter(tenant_id)` reutilizável por qualquer `DbContext` de módulo.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Base multi-tenant reutilizável por todos os módulos, sem JOIN cruzado nem vazamento de tenant. |
| 2. Contexto | Reader → Writer | Repo greenfield — só o PDF de arquitetura como fonte. |
| 3. Quebra | Tech Lead | Criar solução, VOs base, middleware de tenant, filtro global EF Core. |
| 4. Estrutura | Architect | Ver docs/decisions.md — shared schema com `tenant_id`. |
| 5. Aprovação | Product Owner | aprovado |
| 6. Implementação | Dev Backend | Formalizou o que a task 002 já criava inline + preencheu gaps: `src/Shared/Contracts.Abstractions` (`PagedResult<T>`, `PageRequest`, `IHasPagination`, `IModuleContract`); generalizou `ApplyTenantQueryFilters<TContext>(ModelBuilder, TContext)` em `Infrastructure.Common`. Corrigiu 2 bugs antes de fechar: closure de `ITenantContext` (serviço scoped) no `Model` cacheado do EF Core teria vazado tenant entre requests em produção — mudou pra capturar o `TContext` (DbContext), reavaliado por instância viva a cada query; e reflection usando overload não-genérica `ModelBuilder.Entity(Type)` em vez de `Entity<TEntity>()`, que compilava mas quebrava em runtime. |
| 7. Teste | QA | Build 0 erro/0 aviso. 39/39 testes, incluindo `TenantQueryFilterTests.cs` novo (provider InMemory) provando isolamento de dados entre 2 tenants distintos. |
| 8. Documentação | Writer | Registrado em `docs/knowledge/errors-aprendidos.md` (2 bugs), `docs/knowledge/patterns.md` (filtro de tenant genérico por `TContext`), `docs/decisions.md` (xmin excluído daqui, é da 004 — decisões próprias da 001 registradas). |

## Status final

done — build 0 erro/aviso, 39/39 testes. Sprint 1 completa (todas as 4 tasks done).

## Notas

Depende de nada (é a base). Task 002 usa uma versão mínima inline até esta task rodar formalmente.
