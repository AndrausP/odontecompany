---
sprint: "11"
status: done
---

# Sprint 11

**Período:** 2026-08-19 → 2026-08-19
**Objetivo:** Continuação do polimento visual pedido pelo usuário ("foque agora no polimento" /
escopo "tudo") — regra de negócio travada com o usuário: zero cor hardcoded remanescente em
`frontend/src`, toda tela usando só os tokens do design system (024/025). Reaberta 3x same-day:
validação de endpoints de create (bugfix real encontrado), depois landing pública + onboarding
guiado (feature grande, grill-me próprio).
**Aprovado por (PO):** Product Owner (decisão do usuário + broker) — 2026-08-19

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 033 | Sweep de cor hardcoded — verificação completa (8 features) | done | QA |
| 034 | Code-splitting por rota (lazy loading das páginas) | done | Dev Frontend |
| 035 | Fix: DateTime Kind=Unspecified em endpoints de create | done | Broker |
| 036 | Backend: `User.OnboardingSkipped` + skip-onboarding | done | Dev Backend |
| 037 | Frontend: onboarding guiado (Empresa → Unidade) + skip | done | Dev Frontend |
| 038 | Landing page pública ("/") com pricing vitrine | done | Dev Frontend |

<!-- Status possíveis: planned | in-progress | blocked | done -->

## Notas do Tech Lead

Escopo inicial (grill-me com usuário) era varredura + correção. Reader mapeou antes de abrir task
de implementação: `frontend/src/**/*.{tsx,ts,css}` já está 100% migrado pros tokens desde a
024/025 (sprint-7) — as 93 ocorrências mapeadas em `docs/design/design-system.md` §6 já foram
todas resolvidas em sprints anteriores, nenhuma reintroduzida nas sprints 8-9 (Agenda redesign,
dark reskin). Task 033 virou verificação, não implementação — sem handoff pro Dev Frontend.

## Retrospectiva

Sweep automatizado (`grep -rnE '\b(bg|text|border|divide|outline|ring)-(slate|red|amber|emerald|
blue|gray|zinc|neutral|green|yellow|orange)-\d{2,3}\b'` sobre `.tsx`/`.ts`/`.css`) — **zero
matches** em todo `frontend/src`. `npm run lint` (oxlint) e `npm run build` limpos. Design system
confirmado 100% aderente nas 8 features (auth, billing, estoque, patients, reports, scheduling,
organizations, invites) + `ui/`/`AppLayout`.

Achado lateral fora de escopo desta task (registrado, não corrigido): `vite build` avisa chunk
`index-*.js` de 792.90 kB (>500 kB) — nenhum `dynamic import()`/code-splitting configurado. Não é
cor/token, é performance de bundle; vira item de backlog se o usuário quiser puxar.

Usuário pediu pra puxar o achado lateral na sequência (task 034) — sprint reaberta same-day pra
fechar o item já nomeado acima em vez de abrir sprint nova pra 1 task de continuação direta.

`App.tsx`: as 10 páginas de rota (`LoginPage`, `SignupPage`, `OnboardingPage`, `InvitesPage`,
`AgendaPage`, `PatientsPage`, `FinanceiroPage`, `EstoquePage`, `ReportsPage`, `ComprovantePage`)
viraram `React.lazy()`, `<Routes>` envolto em `<Suspense fallback={<RouteFallback />}>`. Shell
(`AppLayout`/`ProtectedRoute`/`RequireOrganization`) continua eager — nav aparece antes da página
carregar, evita layout shift. `RouteFallback` usa o mesmo idioma de loading já usado dentro das
páginas (`text-sm text-ink-muted`), sem componente novo.

`npm run build` depois: bundle principal caiu de 792.90 kB → 250.79 kB (chunk `index-*.js`); maior
chunk agora é `AgendaPage-*.js` (285.02 kB, FullCalendar) — nenhum chunk passa dos 500 kB, aviso
de bundle size sumiu. `npm run lint` limpo.

## Reabertura 2 (035) — usuário reportou "não consigo criar nada"

Investigação real (browser + DevTools Network + log do backend colado pelo usuário) achou 500 em
`POST /api/patients`: `DbUpdateException` → `Cannot write DateTime with Kind=Unspecified to
PostgreSQL type 'timestamp with time zone'`. Causa raiz e correção completas em
docs/tasks/035-fix-datetime-utc-endpoints.md e docs/knowledge/errors-aprendidos.md — convenção EF
Core global (`ConfigureConventions`) nos 7 `DbContext`s, não fix pontual. Aproveitado pra corrigir
achado lateral relatado junto: ícone do date picker invisível no dark (`color-scheme` ausente).

## Reabertura 3 (036-038) — landing pública + onboarding guiado

Usuário pediu 2 mudanças grandes na mesma mensagem: landing pública antes do login (nav + hero +
pricing) e fluxo pós-login perguntando se quer criar empresa agora ou depois, com criação guiada
Empresa→Unidade. Grill-me (`/bigtask`) resolveu 3 decisões em aberto: (1) "franquia"/"filiado" são
a hierarquia Organization/Branch já existente, sem camada nova; (2) pular a criação de empresa
persiste no backend, não expira por sessão; (3) pricing é vitrine estática, sem checkout real
nessa rodada. Detalhes por task em docs/tasks/036, 037, 038; decisões consolidadas em
docs/decisions.md, regras em docs/knowledge/business-rules.md.

Verificação: `dotnet build` (backend) e `npm run build`/`npm run lint` (frontend) limpos em todas
as 4 tasks desta reabertura; landing e onboarding validados ao vivo via claude-in-chrome
(navegação real no browser, sem mock).
