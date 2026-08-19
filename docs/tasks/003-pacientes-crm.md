---
task: "003"
sprint: "1"
status: done
---

# 003 — Pacientes / CRM: cadastro

**Sprint:** docs/sprints/sprint-1.md
**Critério de aceite:** CRUD de paciente escopado por tenant (nome, contato, consentimento
LGPD), base para notificações e retorno. Sem campanhas/lembretes ainda (isso entra quando
Notifications existir).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | CRUD de paciente escopado por tenant, com consentimento LGPD, base para módulos futuros de notificação/retorno. |
| 2. Contexto | Reader → Writer | Módulo greenfield, seguindo o padrão Clean Architecture de 4 projetos já estabelecido pela task 002 (Identity). |
| 3. Quebra | Tech Lead | CRUD completo com CPF como VO, consentimento LGPD obrigatório, RBAC (Admin+Recepcao administram, Dentista só lê). |
| 4. Estrutura | Architect | Módulo `Patients` (Domain/Application/Infrastructure/Contracts), seguindo fronteira `*.Contracts` já documentada em docs/knowledge/patterns.md. |
| 5. Aprovação | Product Owner | aprovado |
| 6. Implementação | Dev Backend | Módulo `Patients` completo. CPF como Value Object com validação mod-11 real (rejeita sequência repetida), único por `(TenantId, Cpf)` — não global. `ConsentimentoLgpd` obrigatório na criação, validado em dupla camada (FluentValidation + `Patient.Create` no domínio). CPF imutável após criação. Soft delete real via `Desativar()`. RBAC: Admin+Recepcao administram, Dentista só lê. Extraiu `ValidationBehavior<TRequest,TResponse>` de `Identity.Application` pra `src/Shared/Application.Common` compartilhado, evitando registro duplicado de open generic no MediatR. |
| 7. Teste | QA | Revisou os 8 pontos do critério de aceite, todos PASS, sem bug bloqueante. Adicionou 1 teste de salvaguarda garantindo que `AtualizarDadosCadastrais` nunca ganhe parâmetro que mude `TenantId`/consentimento LGPD no futuro. Build 0 erro/0 aviso, 38/38 testes. |
| 8. Documentação | Writer | Registrado em `docs/knowledge/business-rules.md` (CPF único por tenant, consentimento LGPD dupla camada, CPF imutável, soft delete, RBAC Patients), `docs/decisions.md` (CPF único por tenant vs email global), `docs/knowledge/patterns.md` (ValidationBehavior compartilhado). |

## Status final

done — build 0 erro/aviso, 38/38 testes, QA sem bug bloqueante. Sprint 1 completa (todas as 4 tasks done).

## Notas

Depende de 001 (tenancy) e 002 (auth — só usuário autenticado cria/edita paciente).
