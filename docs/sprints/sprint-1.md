---
sprint: "1"
status: done
---

# Sprint 1

**Período:** 2026-08-17 → (aberto)
**Objetivo:** Fase 1 (Núcleo) do roadmap — fundação multi-tenant, autenticação, cadastro de pacientes e agenda.
**Aprovado por (PO):** Product Owner — 2026-08-17

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 001 | SharedKernel + fundação de tenancy | done | Dev Backend |
| 002 | Identity & Access — login e autenticação | done | Dev Backend |
| 003 | Pacientes / CRM — cadastro | done | Dev Backend |
| 004 | Agenda — vertical completo (domínio→API) | done | Dev Backend |

## Notas do Tech Lead

Task 002 (Identity & Access) foi adiantada antes da 001 a pedido do usuário — como Identity
depende de `ITenantContext`/global query filter, a parte mínima de tenancy necessária (VO
`TenantId`, interface `ITenantContext`, resolução por claim do JWT) é implementada dentro da
própria task 002 e depois absorvida/generalizada pela task 001 quando o SharedKernel for
formalizado. Sem isso o login não teria como escopar o usuário por clínica.

## Retrospectiva

**Entregue:** as 4 tasks da Fase 1 (Núcleo) fechadas com QA passando e sem bug bloqueante
conhecido. Base multi-tenant formal (`SharedKernel`, `Infrastructure.Common`,
`Contracts.Abstractions`, filtro de tenant genérico), autenticação/RBAC (Identity, task 002),
cadastro de pacientes com CPF/LGPD (Patients, task 003) e agenda com máquina de estados,
concorrência e Outbox (Scheduling, task 004). Solução completa: build 0 erro/0 aviso,
**128/128 testes** (Identity 39 + Patients 38 + Scheduling 51).

**Padrão de arquitetura consolidado:** Clean Architecture por módulo (`Domain` / `Application` /
`Infrastructure` / `Contracts`), fronteira entre módulos só via `.Contracts` (portas
`I{Modulo}Lookup` pra leitura cross-module), `Result` pattern pra erro de negócio,
`ValidationBehavior` compartilhado — esse molde CQRS + Clean Architecture, validado em 3 módulos
completos, é o que os próximos módulos (Financeiro, Notifications, etc.) devem seguir.

**Bugs reais de segurança evitados/corrigidos na sprint:** vazamento de tenant por closure de
serviço scoped no `Model` cacheado do EF Core (001); race de criação sem lock permitindo
agendamentos sobrepostos (004); RBAC sem ownership check permitindo dentista mexer na agenda de
outro (004). Todos documentados em `docs/knowledge/errors-aprendidos.md`.

**Riscos técnicos conhecidos que ficam para depois (não bugs, dívida documentada):**
- Sem Postgres/Redis real rodando ainda — toda validação de concorrência (lock Redis, `xmin`,
  filtro de tenant) foi feita com testes automatizados/provider InMemory, não contra a
  infraestrutura real. Precisa de teste de integração/carga contra Postgres+Redis reais antes de
  produção.
- Sem migrations do EF Core geradas ainda para nenhum módulo — os DbContexts existem e compilam,
  mas o schema real do banco (incluindo a coluna `xmin` do Scheduling) nunca foi materializado.
- Worker de publicação do Outbox (MassTransit/RabbitMQ) para `ConsultaConcluidaEvent` não
  implementado — o evento é gravado atomicamente na tabela de Outbox, mas nada ainda o consome e
  publica. Fica pendente pra quando a task 006 (Financeiro) definir o consumidor.
- Nenhum teste de integração de concorrência real (duas requisições HTTP simultâneas de fato) —
  os testes que cobrem lock Redis e race de criação são testes unitários/de handler, não um
  teste de carga real contra a API subida.
- Pendência de produto registrada (não é bug): "dentista só cria na própria agenda" não foi
  pedido — hoje qualquer dentista pode CRIAR agendamento pra qualquer profissional, o ownership
  check só se aplica em Confirmar/Cancelar/Concluir.
