---
task: "028"
sprint: "7"
status: done
---

# 028 — Comprovante de pagamento (visão colaborador + visão empresa) com impressão

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** FRONTEND
**Estimativa:** 2 dias
**Depende de:** 023 (endpoint), 025 (tokens/tema), 027 (tipos e client de API já criados)
**Critério de aceite:**
1. Rota nova (ex.: `/comprovante`) renderiza o comprovante do período escolhido.
2. **Visão colaborador** (`Dentista`): vê só a própria comissão — total do período, valor pago,
   qtd. de faturas e **média diária**. Nenhum dado de terceiro na tela.
3. **Visão empresa** (`Owner`/`Admin`): mesmo documento, agregado, com quebra por profissional,
   filtrável por filial e classe.
4. `Recepcao` não vê o item no menu e, se acessar a rota direto, recebe uma mensagem clara de sem
   permissão — **nunca tela branca nem erro 403 cru**.
5. Botão "Imprimir" chama `window.print()`; na impressão saem **só** o documento (cabeçalho com
   organization, profissional, período; corpo com valores; rodapé) — sem nav, sem filtros, sem
   botões.
6. Impressão legível em preto e branco, independente do tema ativo na tela (CSS de impressão força
   fundo claro / texto escuro).
7. Estado vazio: "Nenhum pagamento no período" quando não há comissão.
8. Legível nos dois temas; só tokens da 025.

## Escopo técnico

- `frontend/src/features/reports/ComprovantePage.tsx` (ou `features/comissoes/`, o Dev alinha com a
  estrutura existente por feature).
- Reusa `getComissoesResumo` da 027 — **não** criar client de API duplicado.
- A visão é decidida pelo **backend** (`Dentista` já recebe só o próprio, task 023). O frontend
  **não** implementa regra de permissão de dado — só adapta a apresentação (`claims.role`) e
  esconde os filtros de filial/classe pro `Dentista`.
- CSS de impressão: `@media print` em `index.css` ou arquivo dedicado. Classe utilitária
  `print:hidden` do Tailwind pra nav/filtros/botões.
- Rota registrada em `App.tsx` sob `ProtectedRoute` + `RequireOrganization`, mesmo padrão das demais.

## Dívida técnica nomeada (decisão da sprint, registrada — não é omissão)

**Sem PDF gerado no backend.** Não há lib de PDF no stack e adicionar uma nesta rodada custaria
mais que o resto da task. Comprovante é uma view estruturada + `window.print()` do browser.
Consequência aceita: o arquivo resultante depende do browser do usuário e não tem assinatura
digital nem numeração de documento fiscal. **Não pode ser vendido como documento fiscal.**
Se o usuário pedir PDF real depois → task nova, sprint futura.

## Riscos

- **R11 (médio):** CSS de impressão com dark mode ativo é o clássico "imprimiu tudo preto".
  Critério 6 existe por isso — testar impressão com tema escuro ligado é obrigatório, não opcional.
- **R12 (baixo):** `Dentista` sem `Profissional` vinculado recebe totais zerados (tratado na 023).
  O frontend precisa mostrar isso como estado vazio, não como erro.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Comprovante de comissão, colaborador e empresa, média diária (sprint-7) |
| 2. Contexto | Reader → Writer | Sem lib de PDF no stack; padrão de rota protegida no App.tsx |
| 3. Quebra | Tech Lead | Escopo acima; dívida de PDF nomeada |
| 4. Estrutura | Architect | [pendente — validar que nenhuma regra de permissão vaza pro front] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Frontend | `ComprovantePage.tsx` criado, rota `/comprovante` registrada em `App.tsx`, build limpo |
| 7. Teste | QA | [na 029] |
| 8. Documentação | Writer | [pendente] |

## Status

planned → in-progress → in-review (QA — FAIL) → **done** (fix aplicado + revalidado pelo QA)

**QA (Jubileu) — 2026-08-18 (revalidação):** PASS. Broker aplicou o fix (`isAdmin → isEmpresa`
no render e no `enabled` da query de branches, comentário stale removido). Revalidação:
- Owner: `isEmpresa === true` → agora vê os 3 selects (filial/classe/profissional). Query
  `/api/branches` dispara. Critério 3 (visão empresa Owner/Admin) cumprido.
- Admin: comportamento inalterado (continua vendo). Sem regressão.
- Dentista: `isDentista === true`, não é `isEmpresa`, continua sem os 3 selects (correto — R12
  cobre o caso de Profissional não vinculado).
- Recepcao / role vazio: `semPermissao === true` → cai no branch "sem permissão" ANTES de
  renderizar filtros (critério 4 preservado).
- Rota `/comprovante` continua sem `roles=` no `ProtectedRoute` (App.tsx:74) — Owner passa.
- Menu inclui `['Owner', 'Admin', 'Dentista']` (AppLayout.tsx:64) — Owner enxerga o link.

**QA (Jubileu) — 2026-08-18 (registro histórico do FAIL original):** Bug funcional achado no
critério 3 da própria task 028, provocado pela paralelização com a task 030 na mesma sprint:

- 🟡 IMPORTANTE — `ComprovantePage.tsx` gate o filtro de filial por `isAdmin`
  (`role === 'Admin'`), tanto no render (linhas 213-225) quanto no `enabled` da query
  `getBranches` (linhas 85-89). Comentário nas linhas 83-84 explica: "/api/branches é
  Admin-only no backend (BranchesController, não inclui Owner)". Isso era verdade antes da 030,
  mas depois dela `BranchesController` virou `Owner,Admin` — o Owner AGORA pode chamar
  `/api/branches`, mas a UI continua escondendo o `<Select>` de filial pra ele. Viola o
  critério 3 ("Visão empresa Owner/Admin: filtrável por filial e classe"). Correção: trocar
  `isAdmin` por `isEmpresa` no render e no `enabled`, remover o comentário stale.
- 🔵 SUGESTÃO — linha 277 usa `emissao.nomeProfissional ?? 'Todos os profissionais'`. Em R12
  extremo (Dentista sem Profissional vinculado e `me?.user.nome` vazio) o cabeçalho mostraria
  "Profissional: Todos os profissionais" pra um Dentista. Não crasha; só um label mais fiel
  ao contexto (ex.: "Sem profissional vinculado" quando `role === 'Dentista'`).

Demais critérios validados:
- Critério 4 (Recepcao): `ProtectedRoute` propositalmente sem `roles=`, com check interno que
  renderiza `Card` de "sem permissão" — sem tela branca, sem redirect silencioso, sem 403 cru.
- Critério 5-6 (impressão): `.no-print`/`.print-area` aplicados corretamente; CSS em
  `index.css` linhas 84-88 força `background: white !important; color: black !important;` em
  `body`, `.print-area` e `.print-area *` — R11 (dark mode na impressão) coberta.
- Critério 7 (estado vazio) e R12 (Dentista sem Profissional) tratados sem crash.
- Critério 2 (Dentista vê só a própria): regra de dado delegada ao backend (task 023); o
  frontend só esconde filtros e não injeta `branchId`/`classe` pra Dentista (linhas 79-80).

Volta pro Dev Frontend só pro fix do 🟡 (2 linhas + 1 comentário). Rebuild + revalidação e a
task fecha. **Fechada em 2026-08-18 pelo QA após revalidação do fix aplicado pelo broker.**

## Notas

Entrega o item #2 do pedido original do usuário. **Não é cortável.**
