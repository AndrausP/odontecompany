---
sprint: "2"
status: done
---

# Sprint 2

**Período:** 2026-08-17 → 2026-08-17
**Objetivo:** Fase 2 (Clínico) do roadmap — prontuário eletrônico com criptografia, auditoria e anexos.
**Aprovado por (PO):** Product Owner — 2026-08-17

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 005 | Prontuário eletrônico + anexos | done | Broker (sem subagent — ver notas) |

## Notas do Tech Lead

Sprint executado com **limite de gasto mensal em subagent atingido** durante a sessão — a task
005 não passou pelo par dedicado Dev Backend → QA usado nas tasks 001-004. Implementação e
auto-revisão feitas pelo broker diretamente no thread principal, seguindo à risca o padrão
arquitetural já consolidado na sprint 1 (Clean Architecture, CQRS, `ApplyTenantQueryFilters`,
`Result` pattern, `ValidationBehavior` compartilhado). Auto-revisão achou e corrigiu 1 bug real
(TOCTOU em `CreateProntuarioCommandHandler`, mesmo padrão já visto em Identity/Patients).

## Retrospectiva

**Entregue:** módulo `Records` (Prontuário eletrônico) completo — agregado `Prontuario` com
odontograma (JSONB), `EvolucaoClinica` com criptografia de campo em repouso (AES-256-GCM,
`IEncryptionService`), `AnexoMetadata` com object storage abstraído (`IObjectStorageService`,
fallback de disco local em dev), `RecordsAuditLog` append-only com trilha de toda leitura/escrita
gravada em transação própria. RBAC restrito a Admin+Dentista (Recepcao sem acesso a dado
clínico). Build 0 erro/0 aviso, **157/157 testes** na solução completa (Identity 39 + Patients 38
+ Scheduling 51 + Records 29).

**Padrão novo consolidado:** criptografia de campo via `ValueConverter` no EF Core, aplicada
manualmente em `OnModelCreating` (não via `ApplyConfigurationsFromAssembly`) quando a
configuration precisa de uma dependência injetada (`IEncryptionService`) — documentado em
`docs/knowledge/patterns.md`. Trilha de auditoria com `SaveChangesAsync` próprio, separado da
operação principal, como forma de tornar a auditoria resiliente a falha subsequente sem depender
de um worker de Outbox ainda não implementado.

**Risco real da sprint:** execução sem par Dev Backend/QA dedicado. Recomenda-se QA revisar o
módulo `Records` (foco: RBAC, isolamento de tenant, atomicidade da auditoria, criptografia) assim
que o subagent voltar a ficar disponível — mesmo com auto-revisão tendo pego um bug real, o
padrão de duas cabeças (implementa + revisa independente) provou nas sprints anteriores que pega
bugs que uma cabeça só não pega.

**Riscos técnicos conhecidos que ficam para depois (dívida documentada, não bloqueio):**
- Object storage de anexo é disco local (dev-only) — produção precisa de S3/Azure Blob.
- Chave de criptografia vem de config de app (dev-only) — produção precisa de KMS/Key Vault.
- Endurecimento de banco da trilha de auditoria (`REVOKE DELETE/UPDATE`) não aplicado — sem
  Postgres real disponível nesta sessão.
- Migration EF Core gerada em 2026-08-17 — validada no design-time, ainda não aplicada contra Postgres real.
- Worker de Outbox pro `ConsultaConcluidaEvent` (Scheduling) continua pendente — não afeta Records.
