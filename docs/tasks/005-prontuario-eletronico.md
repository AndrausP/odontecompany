---
task: "005"
sprint: "2"
status: done
---

# 005 — Prontuário eletrônico + anexos (Fase 2 · Clínico)

**Sprint:** docs/sprints/sprint-2.md
**Critério de aceite:** Histórico clínico/odontograma com criptografia em repouso dos campos
sensíveis, trilha de auditoria append-only e independente, anexos em object storage com
metadados relacionais.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Prontuário eletrônico multi-tenant, dado clínico sensível sob LGPD — criptografia, auditoria e restrição de acesso são requisito estrutural, não opcional. |
| 2. Contexto | Reader → Writer | Depende de 001 (tenancy), 002 (auth), 003 (`Patients.Contracts.IPatientLookup` pra validar paciente). |
| 3. Quebra | Tech Lead | Módulo `Records`: agregado `Prontuario` (odontograma JSONB), entidades filhas `EvolucaoClinica` (cifrada) e `AnexoMetadata`, trilha `RecordsAuditLog` append-only. |
| 4. Estrutura | Architect | Clean Architecture 4 projetos, mesmo padrão dos módulos anteriores. `EvolucaoClinicaConfiguration` recebe `IEncryptionService` no construtor (única exceção a `ApplyConfigurationsFromAssembly`, aplicada manualmente em `OnModelCreating`). |
| 5. Aprovação | Product Owner | aprovado |
| 6. Implementação | Broker (sem subagent Dev Backend disponível — limite de gasto em subagent atingido na sessão, implementação feita diretamente no thread principal) | Módulo `Records` completo: `Prontuario` (agregado, odontograma `Dictionary<int,StatusDente>` como JSONB), `EvolucaoClinica` (histórico imutável, `DescricaoClinica` cifrada em repouso via AES-256-GCM, `IEncryptionService`/`AesEncryptionService`), `AnexoMetadata` (metadado relacional + binário em `IObjectStorageService`/`LocalDiskObjectStorageService`, fallback de dev — produção precisa de S3/Azure Blob), `RecordsAuditLog` (append-only, sem update/delete no código, gravado em `SaveChangesAsync` PRÓPRIO e separado da operação principal via `IAuditLogWriter`). Toda leitura de prontuário audita (`AcaoAuditoria.Leitura`), não é opcional. RBAC: Admin+Dentista têm acesso clínico completo (Recepcao excluído — dado sensível); visualização da trilha de auditoria é Admin-only. |
| 7. Teste | Broker (auto-revisão — sem QA subagent disponível na sessão) | Build 0 erro/aviso. 29/29 testes próprios (157/157 na solução completa: Identity 39 + Patients 38 + Scheduling 51 + Records 29). Auto-revisão achou e corrigiu 1 bug real: `CreateProntuarioCommandHandler` não tratava `UniqueConstraintViolationException` na constraint única `(TenantId, PacienteId)` — mesmo padrão TOCTOU já visto em Identity (email) e Patients (CPF); corrigido com try/catch traduzindo pra `Result.Failure(DomainErrors.Prontuario.JaExistePorPaciente)`, com teste de regressão. |
| 8. Documentação | Broker (Writer indisponível — limite de gasto em subagent) | Registrado em `docs/knowledge/business-rules.md`, `docs/knowledge/patterns.md`, `docs/knowledge/errors-aprendidos.md`, `docs/decisions.md`. |

## Status

done — build 0 erro/aviso, 29/29 testes próprios (157/157 na solução). Dívidas técnicas
conhecidas, documentadas e não bloqueantes:
- Object storage é fallback de disco local (`LocalDiskObjectStorageService`) — produção precisa
  de S3/Azure Blob real antes de qualquer deploy.
- Chave de criptografia vem de `appsettings.json` (`Records:Encryption:KeyBase64`) — DEV-ONLY,
  produção precisa de KMS/Key Vault gerenciado (mesma dívida já documentada pro `Jwt:SigningKey`
  do Identity).
- Endurecimento de produção da trilha de auditoria (`REVOKE DELETE/UPDATE` diretamente no
  Postgres) não configurado — sem banco real disponível neste ambiente pra aplicar. Hoje a
  garantia de append-only é só em nível de código (repositório não expõe update/delete).
- Migration EF Core gerada em 2026-08-17 (ver docs/tasks/009-rede-multi-unidade.md), validada no design-time — ainda não aplicada contra Postgres real (sem instância disponível nesta sessão).
- Esta task NÃO passou por um par dedicado de revisão (Dev Backend → QA) como as tasks 001-004 —
  o limite de gasto mensal em subagent foi atingido durante a sessão. Implementação e revisão
  foram feitas pelo broker no thread principal, com auto-revisão focada nos mesmos pontos que o
  QA checava nas tasks anteriores (race condition, RBAC, vazamento de tenant). Recomenda-se uma
  passada de QA real quando o subagent voltar a ficar disponível.

## Notas

Depende do núcleo (001-004). Dado clínico sensível — LGPD é requisito estrutural, não opcional.
