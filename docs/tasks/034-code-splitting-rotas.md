---
task: "034"
sprint: "11"
status: done
---

# 034 — Code-splitting por rota (lazy loading das páginas)

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** FRONTEND
**Estimativa:** 0.25 dia
**Depende de:** 033 (achado lateral registrado na retrospectiva)
**Critério de aceite:**
1. Cada página de rota (`App.tsx`) vira chunk JS separado — sem `dynamic import()`, todas
   agrupadas no bundle principal.
2. `npm run build` sem aviso de chunk >500 kB.
3. Shell da aplicação (`AppLayout`, `ProtectedRoute`, `RequireOrganization`) continua eager — nav
   renderiza antes da página carregar, sem layout shift na troca de rota.
4. `npm run lint` e `npm run build` limpos.

## Execução

`App.tsx`: as 10 páginas de rota trocaram de `import` estático pra `React.lazy(() => import(...))`
(`LoginPage`, `SignupPage`, `OnboardingPage`, `InvitesPage`, `AgendaPage`, `PatientsPage`,
`FinanceiroPage`, `EstoquePage`, `ReportsPage`, `ComprovantePage`). `<Routes>` envolto em
`<Suspense fallback={<RouteFallback />}>`; `RouteFallback` é um componente novo, pequeno, no
próprio `App.tsx` — reusa o idioma de loading já presente em `InvitesPage`/`ComprovantePage`
(`text-sm text-ink-muted`), sem componente/lib nova.

**Resultado do build** (antes → depois):
- Bundle principal: 792.90 kB → 250.79 kB (`index-*.js`)
- Maior chunk pós-split: `AgendaPage-*.js`, 285.02 kB (FullCalendar + lógica da Agenda) — ainda
  abaixo do limite de 500 kB, sem aviso
- Chunks pequenos adicionais: `LoginPage` (1.95 kB), `SignupPage` (2.69 kB), `OnboardingPage`
  (2.72 kB), `InvitesPage` (0.66 kB), `EstoquePage` (6.47 kB), `ReportsPage` (7.24 kB),
  `ComprovantePage` (9.01 kB), `PatientsPage` (10.03 kB), `FinanceiroPage` (12.57 kB) — cada um só
  baixa quando o usuário navega pra aquela rota

## Fora de escopo

- Split de libs pesadas dentro de uma página só (ex.: FullCalendar dentro de `AgendaPage`
  carregado só quando o widget realmente renderiza) — o chunk por rota já resolveu o aviso de
  build; otimização mais fina fica pra se o bundle voltar a crescer.
- Prefetch de rota no hover do link de nav (React Router/Vite suportam, mas não foi pedido).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Puxar achado lateral da 033 (bundle >500 kB), critério = build sem aviso, zero mudança de comportamento visual/UX |
| 2. Contexto | Reader → Writer | `App.tsx` mapeado — 10 imports estáticos de página, shell (`AppLayout` etc.) separado |
| 3. Quebra | Tech Lead | 1 task, mesma sprint (11), reaberta same-day |
| 4. Estrutura | Architect | Lazy loading por rota via `React.lazy`/`Suspense` nativo do React — sem lib nova; shell eager, só página split |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Frontend | `App.tsx` |
| 7. Teste | QA | `npm run lint` + `npm run build` limpos; bundle principal 792.90 kB → 250.79 kB, nenhum chunk >500 kB |
| 8. Documentação | Writer | `docs/sprints/sprint-11.md` (retrospectiva atualizada) |

## Status

planned → in-progress → in-review (QA) → **done**.

## Notas

Sem verificação visual em navegador (mesma limitação de infra das tasks 031-033 — backend não
roda nesta máquina). Verificação foi build + lint + comparação do tamanho de chunk antes/depois.
