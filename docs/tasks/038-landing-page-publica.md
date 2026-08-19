---
task: "038"
sprint: "11"
status: done
---

# 038 — Landing page pública ("/") com pricing vitrine

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** FRONTEND
**Depende de:** nenhuma
**Critério de aceite:**
1. Rota "/" pública: nav fixa (logo + Entrar/Criar conta), hero vendendo o produto, seção de
   planos (3 tiers estáticos), footer.
2. Autenticado visitando "/" pula direto pro app (`/agenda`), sem ver a landing de novo.
3. Pricing é vitrine estática nessa rodada — sem checkout/gateway de pagamento; CTA de qualquer
   plano leva pro `/signup`.
4. `npm run build`/`npm run lint` limpos.

## Contexto

Usuário pediu landing pública antes do login, inspirada na composição da home não-logada do
Spotify (nav fixa em cima, hero, seções abaixo — só o formato, não conteúdo/marca). Grill-me
travou: regra de negócio é converter visitante → trial/conta paga; pricing é vitrine estática
nessa rodada (sem integrar gateway de pagamento).

## Execução

- `features/marketing/LandingPage.tsx` (novo): `NavBar` (logo + link "Planos" âncora + Entrar +
  Criar conta), `Hero` (headline + CTA duplo), `Features` (4 cards: Agenda, Prontuário, Financeiro,
  Estoque — reflete as telas reais do produto, não copy genérica), `Pricing` (3 planos: Starter
  R$129/mês 1 unidade, Profissional R$349/mês até 3 unidades — destacado, Rede sob consulta
  unidades ilimitadas), `Footer`.
- CTAs são `<Link>` estilizado (classes copiadas de `Button.tsx` variant primary/secondary), não
  `<Link>` dentro de `<Button>` — ver docs/knowledge/patterns.md.
- `App.tsx`: rota `/` nova (`LandingGate` — autenticado→`Navigate /agenda`, senão `LandingPage`,
  lazy-loaded como as demais páginas desde a 034). Removida a antiga `<Route index>` dentro do
  wrapper `AppLayout` (ficou redundante/ambígua com a `/` explícita nova). Catch-all (`path="*"`)
  trocou o destino de `/agenda` pra `/`.

## Verificação

- `npm run build` — limpo, `LandingPage-*.js` 7.43 kB (chunk próprio), nenhum aviso de bundle.
- `npm run lint` — limpo.
- claude-in-chrome: `/` sem sessão renderiza nav+hero+planos+footer corretamente, zero erro de
  console, todos os `<a>`/CTA apontam pro destino certo (`/login`, `/signup`, `#planos`).

## Fora de escopo

- Checkout/gateway de pagamento real (Stripe ou similar) — decisão do PO, dívida técnica nomeada
  em docs/knowledge/business-rules.md.
- Copy/preço final dos planos — esboço do time, editável a qualquer momento (conteúdo estático,
  sem dependência de backend).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Converter visitante → conta; pricing vitrine, sem checkout |
| 2. Contexto | Reader → Writer | `App.tsx`/rotas/`Button.tsx` mapeados |
| 3. Quebra | Tech Lead | 1 task, sem dependência de backend |
| 4. Estrutura | Architect | Rota `/` pública nova + `LandingGate`; CTA como `<Link>` estilizado, não `<Button>` |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Frontend | `LandingPage.tsx` (novo), `App.tsx` |
| 7. Teste | QA | `npm run build`/`npm run lint` limpos; validado ao vivo via claude-in-chrome |
| 8. Documentação | Writer | `docs/knowledge/patterns.md`, `business-rules.md`, `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
