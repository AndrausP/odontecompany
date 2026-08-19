---
task: "036"
sprint: "11"
status: done
---

# 036 — Backend: `User.OnboardingSkipped` + `POST /api/me/skip-onboarding`

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BACKEND
**Depende de:** nenhuma
**Critério de aceite:**
1. Usuário sem organization pode marcar "pulei o onboarding" e essa escolha persiste no backend
   (sobrevive a reload, outro device, outra sessão).
2. `GET /api/me` devolve o flag pro frontend decidir se redireciona pro `/onboarding` ou não.
3. `dotnet build` limpo.

## Contexto

Grill-me (task guarda-chuva "landing + onboarding guiado"): usuário quer poder pular a criação de
empresa no primeiro login ("por enquanto não"). Decisão travada: persistência no BACKEND (não
localStorage) — não pode voltar a pedir em outro device/sessão depois que o usuário já disse não.

## Execução

- `Identity.Domain.Entities.User`: propriedade `OnboardingSkipped` (bool, default false) +
  método `PularOnboarding()`.
- `UserConfiguration.cs`: mapeamento (`IsRequired().HasDefaultValue(false)`).
- Migration `AddUserOnboardingSkipped` (`Identity.Infrastructure`), aplicada no Postgres local
  (`ALTER TABLE "Users" ADD "OnboardingSkipped" boolean NOT NULL DEFAULT FALSE`).
- `UserDto`/`MeResultDto`: `UserDto` ganhou o campo `OnboardingSkipped`; `GetMeQueryHandler`
  repassa `user.OnboardingSkipped`.
- Novo `SkipOnboardingCommand`/`SkipOnboardingCommandHandler` (`Identity.Application/Commands/
  SkipOnboarding/`) — mesmo padrão de `AcceptInviteCommand` (UserId sempre do JWT, nunca do
  body).
- `MeController`: `POST /api/me/skip-onboarding`, `[Authorize]` simples (sem
  `RequireActiveOrganization` — é chamado exatamente por quem não tem organization nenhuma).

## Verificação

- `dotnet build` — limpo, 0 erros.
- Migration aplicada (`dotnet ef database update`) — coluna confirmada no Postgres local.
- Smoke test: `GET /api/me` e `POST /api/me/skip-onboarding` sem token → `401` (rota existe,
  exige auth — não `404`).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Skip precisa persistir no backend, não só no browser |
| 2. Contexto | Reader → Writer | `User`/`UserDto`/`GetMeQueryHandler`/`MeController` mapeados; padrão de `AcceptInviteCommand` como referência |
| 3. Quebra | Tech Lead | 1 task — entidade + migration + command + endpoint |
| 4. Estrutura | Architect | Flag simples no `User` (não tabela de preferências nova — YAGNI pra 1 bool) |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Backend | `User.cs`, `UserConfiguration.cs`, migration, `UserDto`, `GetMeQueryHandler`, `SkipOnboardingCommand`+Handler, `MeController` |
| 7. Teste | QA | `dotnet build` limpo; migration aplicada; smoke test dos 2 endpoints |
| 8. Documentação | Writer | `docs/knowledge/business-rules.md`, `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
