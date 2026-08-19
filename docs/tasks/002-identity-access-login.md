---
task: "002"
sprint: "1"
status: done
---

# 002 — Identity & Access: login e autenticação

**Sprint:** docs/sprints/sprint-1.md
**Critério de aceite:**
- Login (`POST /api/auth/login`) autentica por email+senha, escopado por `tenant_id`, retorna
  JWT de curta duração + refresh token.
- `POST /api/auth/refresh` troca refresh token válido por novo par access+refresh (rotação).
- Senhas com hash Argon2id.
- RBAC com 3 papéis mínimos: `Admin`, `Dentista`, `Recepcao`.
- Sem registro público: um seed cria o primeiro `Admin` de uma clínica base (tenant seed);
  demais usuários (dentista/recepção) só são criados por um `Admin` autenticado, dentro do
  próprio tenant (`POST /api/users`, autorizado por policy `RequireRole(Admin)`).
- Token carrega `tenant_id`, `user_id`, `role` como claims.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Autenticação multi-tenant desde o dia 1; sem signup público — seed cria admin+clínica base, admin cria os demais usuários dentro do tenant. |
| 2. Contexto | Reader → Writer | Repo greenfield, só PDF de arquitetura. |
| 3. Quebra | Tech Lead | Domain (User, Role VO, RefreshToken), Application (Login/Refresh/CreateUser commands), Infrastructure (EF Core, Argon2, JWT issuer), API (AuthController, UsersController). |
| 4. Estrutura | Architect | Ver docs/decisions.md — JWT 15min + refresh 7 dias rotativo em tabela própria; Argon2id via `Konscious.Security.Cryptography`. |
| 5. Aprovação | Product Owner | aprovado |
| 6. Implementação | Dev Backend | Solução `.NET 8` real (`OdontoPlatform.sln`), Clean Architecture: `src/Shared/SharedKernel`, `src/Shared/Infrastructure.Common` (`ITenantContext`, `TenantMiddleware`, filtro global EF Core), `src/Modules/Identity/{Identity.Domain,Identity.Application,Identity.Infrastructure,Identity.Contracts}`, `src/Bootstrap/OdontoPlatform.Api`. Login, refresh rotativo, criação de usuário por admin, seed de tenant+admin. Decisões técnicas: email único global, `IMustHaveTenant` no SharedKernel, SHA-256 pro refresh token, ports em Application/adapters em Infrastructure, `ValidationBehavior<TRequest,TResponse>` devolvendo Result — ver docs/decisions.md e docs/knowledge/patterns.md. |
| 7. Teste | QA | 5 bugs encontrados e corrigidos pelo Dev Backend: (1) crítico — `MapInboundClaims` ausente quebrava `CurrentUserAccessor.UserId`; (2) timing side-channel no login permitia enumerar emails; (3) reuso de refresh token revogado não era detectado/revogado em cascata; (4) fail-open com signing key hardcoded de dev se config `Jwt` sumisse; (5) TOCTOU na criação de usuário gerava 500 cru em corrida de emails duplicados. Sugestão aplicada: login recusa tenant inativo. Estado final: `dotnet build OdontoPlatform.sln` limpo (0 erros/avisos), `dotnet test tests/Identity.UnitTests` → 37/37 passando. Detalhe de cada bug em docs/knowledge/errors-aprendidos.md. |
| 8. Documentação | Writer | Registrado em docs/knowledge/business-rules.md, docs/knowledge/patterns.md, docs/knowledge/errors-aprendidos.md e docs/decisions.md. |

## Status

done — implementado, revisado pelo QA (5 bugs corrigidos), documentado. Riscos residuais
conhecidos (não bloqueiam o fechamento da task, ver seção Notas): sem Postgres real rodando
ainda (migration `InitialCreate` não gerada/aplicada), sem testes de integração
(WebApplicationFactory/Testcontainers), JWT signing key de dev em texto puro no `appsettings`
(ok pra dev, precisa ir pra secrets antes de deploy). Credenciais de seed (dev): email
`admin@clinicademo.local`, senha `Admin@123456`, tenant "Clínica Demo" — vêm de
`appsettings.json` seção `Auth`, precisam mudar em produção.

## Notas

Primeira parte solicitada pelo usuário. Depende de uma versão mínima de tenancy (inline aqui,
generalizada depois na task 001).
