---
sprint: "7"
status: done
---

# Sprint 7

**Período:** 2026-08-18 → (aberto)
**Objetivo:** Dashboard com filtro de filial + classe de funcionário, comprovante de pagamento
(comissão) com média por filtro de dia, redesign visual (verde+branco, minimalista, dark/light,
menu de navegação refeito).
**Aprovado por (PO):** Product Owner (decisão do usuário + broker, ver docs/decisions.md) — 2026-08-18

## Contexto herdado da sprint-6

`OrganizationMembership` (role por org), `Branch` (filial), `Fatura.ProfissionalId` +
`Fatura.CalcularValorComissao()` (Billing) e `Parcela.DataPagamento` já existem — a base de dados
pra "quem ganhou quanto, quando, em qual filial" já está toda modelada, esta sprint só lê e
apresenta. Módulo `Reporting` já tem `IFaturamentoSummaryProvider`/`GetFaturamentoPorPeriodoQuery`
— extensão, não módulo novo.

## Regra de negócio (decisão do PO — usuário confirmou "continue e conclua", broker decidiu o
detalhe sem interromper de novo)

- **Comprovante de pagamento** = comissão do profissional (`Fatura.ComissaoDentistaPercentual` /
  `CalcularValorComissao()`) sobre faturas com parcela paga (`Parcela.DataPagamento` dentro do
  período filtrado) — não é folha de salário fixo (não existe esse conceito no domínio hoje).
- Visão **colaborador**: vê só a própria comissão (RBAC — profissional só vê o que é seu, mesmo
  padrão de ownership já usado em Scheduling).
- Visão **empresa**: Admin/Recepcao vê agregado por filial (`BranchId`) e por classe (`Role` da
  membership) — soma total, quantas faturas, comissão total paga, faturamento bruto.
- **Média por filtro de dia**: usuário escolhe intervalo de datas; sistema mostra total do
  período E média diária (total ÷ número de dias do intervalo) — tanto pra colaborador quanto
  pra empresa.
- Sem PDF gerado no backend nesta rodada (sem lib de PDF no stack) — comprovante é uma
  view estruturada na tela, exportável/imprimível via `window.print()` do browser (CSS de
  impressão). PDF real fica de dívida nomeada se o usuário pedir depois.

## Tasks

| ID | Título | Tipo | Est. | Depende de | Status | Owner |
|----|--------|------|------|------------|--------|-------|
| [022](../tasks/022-contratos-leitura-comissao.md) | Contratos de leitura cross-módulo pra comissão | BACKEND | 2d | — | done | Dev Backend |
| [023](../tasks/023-query-comissoes-endpoint-rbac.md) | Query de comissões + endpoint + RBAC de ownership | BACKEND | 3d | 022 | done | Dev Backend |
| [024](../tasks/024-design-system-verde-dark-light.md) | Sistema visual verde+branco, tokens light/dark, spec de nav | DESIGN | 2d | — | done | Designer |
| [025](../tasks/025-frontend-tokens-tema-dark-light.md) | Fundação visual: tokens, ThemeProvider, re-skin `ui/` | FRONTEND | 2d | 024 | done | Dev Frontend |
| [026](../tasks/026-frontend-navegacao-redesenhada.md) | Navegação redesenhada (`AppLayout`) + toggle de tema | FRONTEND | 2d | 024, 025 | done | Dev Frontend |
| [027](../tasks/027-frontend-dashboard-filtros-filial-classe.md) | Dashboard com filtro de filial e classe | FRONTEND | 2d | 023, 025 | done | Dev Frontend |
| [028](../tasks/028-frontend-comprovante-pagamento.md) | Comprovante de pagamento + impressão | FRONTEND | 2d | 023, 025, 027 | done | Dev Frontend |
| [029](../tasks/029-qa-comissao-rbac-visual.md) | QA: RBAC de comissão, cálculo e regressão visual | QA | 2d | 023, 025, 027, 028 | done | QA |
| [030](../tasks/030-owner-role-parity-rbac.md) | Owner precisa de paridade de acesso com Admin (RBAC) — **hotfix fora do plano original**, achado durante a 027 | BACKEND | — | — | done | Dev Backend |

**Total: 17 dias de trabalho** (8 tasks planejadas) + 030 (hotfix não estimado, achado em execução).

## Ordem de execução

Duas trilhas em paralelo — backend e visual não se bloqueiam até a 027.

```
Trilha A (backend):  022 ──► 023 ─────────────────┐
Trilha B (visual):   024 ──► 025 ──► 026 (corta se apertar)
                                 └──────► 027 ──► 028 ──► 029
```

Regra: **começar 022 e 024 no mesmo dia**. A 024 é caminho crítico do lado visual — se o Designer
começar depois, o Dev Frontend fica ocioso.

## Notas do Tech Lead

### Veredito de viabilidade

```
Sprint 7 (escopo integral)
Veredito: ⚠️ viável com risco
Motivo: 17 dias de dev pra um júnior meio período = ~7 semanas de calendário. Escopo é sprint
dupla, não sprint única.
Pra virar ✅: cortar a 026 (nav redesenhada) e aceitar ~15 dias, OU aceitar que a sprint 7 é
sprint dupla e comunicar o prazo real ao usuário. Não cortar 023 nem 028 — são o pedido.
```

Item por item:

```
Dashboard com filtro de filial + classe (022, 023, 027)
Veredito: ✅ viável
Motivo: os dados já existem (ProfissionalId, BranchId, Role de membership). É leitura e
composição, sem migration nem mudança de escrita.
```

```
Comprovante de pagamento com média diária (023, 028)
Veredito: ✅ viável
Motivo: Parcela.DataPagamento + ComissaoDentistaPercentual dão a base de caixa. Média diária é
divisão. Sem PDF real — dívida nomeada, aceita.
```

```
Redesign visual completo (024, 025, 026)
Veredito: ⚠️ viável com risco
Motivo: dark mode obriga a tokenizar cor hardcoded em TODAS as telas já prontas — o volume está
nas telas antigas, não nas novas. Risco de estourar de 4 pra 7 dias.
Pra virar ✅: a task 024 tem que entregar a lista completa de utilitários de cor em uso hoje
(R5). Sem essa lista, não liberar a 025.
```

### Decisões tomadas pelo Tech Lead (não reabrir)

- **D1 — filial da comissão é derivada, não persistida.** `Fatura` **não tem** `BranchId` (só
  `Profissional` e `OrganizationMembership` têm). Adicionar exigiria migration + backfill +
  mudança nos 3 fluxos de criação de fatura — fora de escopo de uma sprint de leitura. Filial vem
  de `Profissional.BranchId` por composição. **Consequência aceita:** profissional que muda de
  filial leva o histórico junto. Dívida nomeada (R1, task 022).
- **D2 — comissão em regime de caixa, proporcional ao valor pago.** `Fatura.CalcularValorComissao()`
  existente é competência sobre a fatura inteira e **não serve** aqui: fatura de 3 parcelas com 1
  paga pagaria comissão cheia. Base correta: `ValorParcelaPaga * pct / 100`.
- **D3 — uma query flexível, não duas.** Visão colaborador e visão empresa são a mesma query com
  filtro diferente. Duas queries duplicariam agregação e média diária pro júnior manter.
- **D4 — controller novo (`ComissoesController`), não método novo no `ReportsController`.**
  `ReportsController` é `[Authorize(Roles="Admin")]` na classe; em ASP.NET Core os `[Authorize]`
  são cumulativos (AND), então atributo no método **não afrouxa** — o `Dentista` seria bloqueado.
  Controller separado evita regressão nos 3 endpoints existentes.
- **D5 — escopo resolvido no controller, nunca aceito do cliente.** Mesmo padrão de
  `SchedulingController.ListAgendamentos`: `Dentista` tem `ProfissionalId` forçado ao próprio.
- **D6 — média diária calculada no backend.** É regra de negócio (definição de "dias do período":
  inclusivo nas duas pontas, mínimo 1), não formatação de tela.
- **D7 — Designer entrega spec + tokens, não código de feature.** A 024 produz
  `docs/design/design-system.md` + bloco `@theme` pronto. O Dev Frontend aplica nas 025-028. Sem
  isso, cada tela inventa uma cor e o resultado é exatamente o que o usuário disse que não quer.

### Corte de escopo consciente — precisa de ciência do PO

O filtro de **filial + classe recorta apenas o bloco de comissão/produtividade por profissional**,
não os demais KPIs do dashboard (pacientes ativos, agendamentos, faturamento geral). Motivo:
propagar `branchId` por `IPatientSummaryProvider`, `IAgendaSummaryProvider` e
`IFaturamentoSummaryProvider` é uma sprint inteira sozinha, e "paciente por classe de funcionário"
não tem significado de negócio. **Dívida nomeada, task futura se o PO quiser.**

### Dívidas técnicas assumidas nesta sprint (nomeadas, não escondidas)

1. Comprovante sem PDF real — `window.print()` do browser. Não é documento fiscal (task 028).
2. Filial da comissão derivada do profissional, sem snapshot histórico (D1).
3. Composição em memória no Reporting carrega profissionais + memberships por request — ok em
   dezenas, não em milhares (R3, task 023).
4. Filtros de filial/classe não propagados aos KPIs não-financeiros (corte acima).

### Decisões que travam Dev — Architect precisa fechar ANTES

| # | Decisão | Trava a task |
|---|---------|--------------|
| A1 | Onde mora `CalcularComissaoSobre(valorPago)` — Domain (preferência TL) ou provider | 022 |
| A2 | Assinatura final dos 3 lookups (batch, sem N+1) | 022 |
| A3 | De onde vem `BranchNome` — estender `IBranchLookup` com `ListarAsync` ou o profissional já traz | 023 |
| A4 | Como o controller resolve `UserId → ProfissionalId` sem vazar Scheduling pro host | 023 |
| A5 | **Estratégia de dark mode no Tailwind v4** — `.dark` + `@custom-variant`, `data-theme` ou `@media`. Errar aqui obriga a refazer 026-028 | 025 |
| A6 | Dependência nova de ícones no `package.json`, se a 024 pedir | 026 |

## Retrospectiva

**Entregue:** as 9 tasks planejadas (022-029) fechadas com QA passando, mais 030 (hotfix não
estimado, achado em execução) — sprint completa, sem corte, apesar do veredito inicial de "viável
com risco" (17 dias estimados). Três frentes de valor: (1) dashboard de comissões com filtro de
filial e classe de funcionário, lendo `Fatura`/`Parcela`/`OrganizationMembership` já existentes
sem migration nova (022, 023, 027); (2) comprovante de pagamento (comissão) com total do período e
média diária, exportável/imprimível via `window.print()` com CSS de impressão dedicado, mesmo com
tema escuro ativo (028); (3) redesign visual completo verde+branco, minimalista, com dark/light
via `.dark`+`@custom-variant` e script anti-FOUC, e navegação (`AppLayout`) refeita (024, 025,
026). Solução completa: backend `dotnet test` **330/330** verde (baseline 293 da sprint-6 + 37
testes novos), zero `[Ignore]`/`[Explicit]`/`Assert.Inconclusive`; os 3 endpoints antigos de
`/api/reports` seguem intocados e Admin-only, confirmado por QA.

**Padrão de arquitetura consolidado:** três extensões documentadas em `docs/knowledge/patterns.md`
— (1) "Regime de caixa: cálculo por unidade paga (parcela), nunca pelo total do documento pai"
(`Fatura.CalcularComissaoSobre(valorPago)`, decisão D2/A1, distinto do já existente
`CalcularValorComissao()` de competência); (2) "Controller separado quando `[Authorize(Roles=...)]`
de classe existente bloquearia role nova por AND cumulativo" (`ComissoesController` separado de
`ReportsController`, decisão D4); (3) "Dark mode via `.dark` class + `@custom-variant` + script
anti-FOUC inline no `index.html`" (decisão A5). Também confirmado como reforço de padrão já
existente: "RBAC de escopo forçado pelo controller, nunca aceito como input" (D5, mesma linha do
`SchedulingController` desde a Fase 5).

**Bugs reais encontrados/corrigidos — o mais importante da sprint:** **Owner sem paridade de
acesso com Admin em 4 camadas independentes de RBAC**, achado incrementalmente ao longo de
022-030, não numa única rodada. A task 030 (hotfix) corrigiu a camada 1 (backend
`[Authorize(Roles=...)]`, 10 controllers) achando que resolvia por completo; o QA, em 2 rodadas
sucessivas de revalidação (027 e 028), encontrou o MESMO padrão de gate (`role === 'Admin'`/
`isAdmin` sem `Owner`) ainda quebrado nas camadas 2 (frontend rota `ProtectedRoute` em `App.tsx`),
3 (frontend menu `AppLayout`) e 4 (componente inline — `ReportsPage`, `ComprovantePage`,
`FinanceiroPage`→`ConveniosCard`, `PatientsPage.canManage`). Cada fix pontual revelava a próxima
camada ainda bloqueando Owner na prática — código morto sem erro/exception visível, só
comportamento idêntico ao de antes do fix. A varredura final da task 029 (independente, não
reaproveitando as anteriores) confirmou zero gate `Admin`-only isolado remanescente nas 4 camadas,
cross-check backend↔frontend sem nenhum caso "backend concede mas frontend esconde" nem o oposto.
Lição virou padrão explícito ("Widening de RBAC precisa varrer TODAS as camadas de gate") e erro
aprendido, ambos já registrados em sessão anterior — task 029 não achou bug novo, só confirmou a
varredura fechada. Fora esse achado, QA não encontrou nenhum outro bug bloqueante: os 14 casos
obrigatórios da task 029 (RBAC de ownership de comissão, isolamento multi-tenant, cálculo
proporcional por parcela paga, borda de período de 1 dia, visual/acessibilidade nos dois temas)
passaram todos, cobertura mista de automação (37 testes novos) e inspeção manual registrada como
tal (não como coberta por automação, seguindo a nota da própria task sobre o gap da suíte de
integração — task 021).

**Riscos técnicos conhecidos que ficam para depois (dívida documentada, não bugs):**
- **Sem suíte de integração real (`WebApplicationFactory`)** — os 8 casos de RBAC de pipeline da
  task 029 (roles/policy) foram cobertos por teste de handler + reflection sobre atributos
  declarativos, não por teste runtime de pipeline HTTP real; recomendação de backlog já registrada:
  `docs/tasks/021-bootstrap-integration-tests.md` (status `planned`, herdada da sprint-6, ainda não
  puxada).
- **`docs/tasks/020-membership-reativar-em-invite.md`** (status `planned`, herdada da sprint-6) —
  segue bloqueante antes de qualquer endpoint de desativar/remover membro; não tocada nesta
  sprint.
- **Comprovante sem PDF real** — `window.print()` do browser via CSS de impressão dedicado (D2 da
  sprint, task 028); não é documento fiscal. Dívida nomeada desde a abertura da sprint, vira task
  futura se o PO pedir.
- **Sem verificação de email no signup** e **sem envio real de email de convite** — dívidas
  herdadas da sprint-6 (tasks 015/016), intocadas nesta sprint; seguem bloqueantes antes de
  qualquer usuário real em produção.
- Filtro de filial/classe recorta só o bloco de comissão do dashboard, não os demais KPIs
  (pacientes ativos, agendamentos, faturamento geral) — corte de escopo consciente do Tech Lead,
  dívida nomeada, task futura se o PO quiser propagar `branchId` aos outros summary providers.
- Filial da comissão é derivada de `Profissional.BranchId`, sem snapshot histórico (D1) —
  profissional que muda de filial leva o histórico junto; aceito nesta sprint.
- Demais dívidas herdadas das sprints 1-6 seguem abertas e intocadas: worker de Outbox, convênio
  real, object storage real, KMS real, migrations `InitialCreate` nunca aplicadas contra Postgres
  real, FK cascade de `OrganizationMembership`, RBAC de unidade só em Scheduling.
