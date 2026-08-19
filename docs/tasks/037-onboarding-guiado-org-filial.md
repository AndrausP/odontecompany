---
task: "037"
sprint: "11"
status: done
---

# 037 — Frontend: onboarding guiado (Empresa → Unidade) + skip persistido

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** FRONTEND
**Depende de:** 036 (backend skip-onboarding)
**Critério de aceite:**
1. Criar organization não cai direto no app — passo 2 pede a primeira Branch/unidade
   (`CreateBranchForm`, usando `POST /api/branches` já existente).
2. Botão "Por enquanto não" no passo 1 chama `POST /api/me/skip-onboarding` e entra no app.
3. Quem pulou (sem organization) entra no `AppLayout` normal (nav/tema/logout funcionam), mas a
   área de conteúdo mostra convite pra criar empresa, não as páginas de gestão reais.
4. `npm run build`/`npm run lint` limpos.

## Contexto

Grill-me confirmou: Organization = empresa, Branch = cada unidade/filial — hierarquia já existente
(sem conceito novo). Backend de Branch (`CreateBranch`/`ListBranches`) já pronto desde a task 030
— esta task é só o fluxo guiado no frontend.

## Execução

- `CreateOrganizationForm.tsx`: parou de navegar sozinho — recebe `onCreated: () => void`, quem
  chama decide o próximo passo.
- `CreateBranchForm.tsx` (novo): mesmo padrão do form de organization, chama `createBranch` (novo
  em `organizations/api.ts`, `POST /api/branches`).
- `OnboardingPage.tsx`: state local `step: 'organizacao' | 'unidade'`. Passo 1 (organization) cria
  → avança pra passo 2 (branch) → cria → `navigate('/agenda')`. Botão "Por enquanto não" (mutation
  `skipOnboarding`, novo em `organizations/api.ts`) só aparece no passo 1.
- `RequireOrganization.tsx`: gate mudou de `organizations.length === 0` pra
  `organizations.length === 0 && !user.onboardingSkipped` — quem pulou passa pro `AppLayout`.
- `AppLayout.tsx`: `EmptyOrganizationState` novo — quando `me.organizations.length === 0`, `<main>`
  não renderiza `<Outlet/>` (nenhuma página real, todas dependem de `organizationId` do JWT que não
  existe nesse estado — evitaria 401 espalhado à toa), mostra card centralizado "Crie sua empresa
  pra começar" com link pro `/onboarding`.
- `types/auth.ts`: `UserProfile.onboardingSkipped: boolean` novo.

## Fora de escopo

- Gate por Branch (organization sem nenhuma branch continua deixando navegar — só ausência de
  Organization bloqueia; ver docs/decisions.md).
- Criar/gerenciar branches adicionais fora do onboarding (fluxo normal do app, já existe via
  `BranchesController`, sem tela dedicada nesta rodada).

## Verificação

- `npm run build` — limpo, sem erro TS, nenhum chunk >500 kB.
- `npm run lint` (oxlint) — limpo.
- `dotnet build` (backend, mesma sessão) — limpo.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Fluxo guiado Empresa→Unidade, skip persistido, sem bloquear quem pulou |
| 2. Contexto | Reader → Writer | `OnboardingPage`/`CreateOrganizationForm`/`RequireOrganization`/`AppLayout`/`BranchesController` mapeados |
| 3. Quebra | Tech Lead | 1 task, depende de 036 (flag no backend) |
| 4. Estrutura | Architect | State local de step (não rota nova `/onboarding/filial` — YAGNI, não precisa sobreviver a reload) |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Frontend | `CreateOrganizationForm.tsx`, `CreateBranchForm.tsx` (novo), `OnboardingPage.tsx`, `RequireOrganization.tsx`, `AppLayout.tsx`, `organizations/api.ts`, `types/auth.ts` |
| 7. Teste | QA | `npm run build`/`npm run lint` limpos |
| 8. Documentação | Writer | `docs/knowledge/business-rules.md`, `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
