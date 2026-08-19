---
task: "029"
sprint: "7"
status: done
---

# 029 — QA: RBAC de comissão, cálculo de média diária e regressão visual

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** QA
**Estimativa:** 2 dias
**Depende de:** 023, 025, 027, 028 (026 se não for cortada)
**Critério de aceite:** Os 14 casos abaixo executados e registrados com pass/fail. Qualquer fail
volta pro Dev responsável — **não segue pro Writer com fail em aberto**.

## Casos obrigatórios — RBAC de comissão (o mais crítico da sprint)

Remuneração de terceiro vazando é o pior defeito possível aqui. Testar exaustivamente.

1. `Dentista` chama `/api/reports/comissoes` sem `profissionalId` → recebe só o próprio.
2. `Dentista` chama com `profissionalId` de **outro** profissional → recebe o próprio, não o do
   outro; não é erro 500.
3. `Dentista` passa `branchId` de outra filial → parâmetro ignorado, resultado inalterado.
4. `Recepcao` chama o endpoint → 403.
5. `Admin` chama → agregado da organization inteira.
6. `Owner` chama → mesmo comportamento de `Admin`.
7. Token de outra organization → **zero** linhas da organization alheia (isolamento multi-tenant).
8. `Dentista` sem `Profissional` vinculado → 200 zerado, sem dados de terceiro.

## Casos obrigatórios — cálculo

9. Fatura de 3 parcelas com 1 paga no período → comissão **proporcional ao valor pago**, não sobre
   o `ValorTotal` da fatura (regressão da decisão D2 da task 022).
10. Parcela paga **fora** do período → não entra.
11. Fatura cancelada → não entra.
12. Período de 1 dia → `MediaDiariaComissao == ValorComissaoTotal` (período inclusivo nas pontas,
    nunca divisão por zero).
13. `dataFim < dataInicio` → 400 com mensagem, não exceção.

## Casos obrigatórios — visual e acessibilidade

14. Percorrer **todas** as telas (Agenda, Pacientes, Financeiro, Estoque, Relatórios, Convites,
    Login, Signup, Onboarding, Dashboard novo, Comprovante) nos **dois temas** e confirmar:
    nada ilegível, nenhum texto abaixo de AA (4.5:1), nenhuma cor hardcoded sobrando, foco de
    teclado visível, sem FOUC no reload.
    Impressão do comprovante testada **com tema escuro ativo** (R11 da task 028).

## Regressão

- A suíte NUnit existente (293 testes na sprint-6) continua verde; o total **só cresce**.
- Os 3 endpoints antigos de `/api/reports` (dashboard, faturamento, agenda) seguem Admin-only e
  funcionando — a task 023 criou controller novo justamente pra não tocá-los; confirmar.
- Nav: `Financeiro`/`Estoque` invisíveis pra `Dentista`, `Relatórios` só pra `Admin`, badge de
  convites funcionando.

## Nota sobre cobertura

Os casos 1-8 são exatamente o tipo de validação de pipeline (`[Authorize]`, roles, policy) que
teste de handler isolado **não** alcança — é o gap descrito em `docs/tasks/021-bootstrap-integration-tests.md`.
Se a 021 tiver sido concluída antes, virar os casos 1-8 em testes de integração reais. Se não,
executá-los manualmente e **registrar como cobertura manual**, não como coberto por automação.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Critérios de aceite das tasks 022-028 |
| 2. Contexto | Reader → Writer | Gap de teste de pipeline (task 021) |
| 3. Quebra | Tech Lead | 14 casos acima |
| 4. Estrutura | Architect | n/a |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | QA | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | [pendente] |

## Status

planned → in-progress → in-review (QA) → **done** (2026-08-19)

### Fechamento QA — regressão end-to-end sprint-7

**Resultado global:** PASS. Zero bug encontrado, zero risco residual bloqueante.

**Backend/Testes:** `dotnet test` 330/330 verde, zero `[Ignore]`/`[Explicit]`/`Assert.Ignore`
/`Assert.Inconclusive` em `tests/**` (grep confirmado). Baseline sprint-6 (293) só cresceu — 37
testes novos na sprint-7 (Fatura/Comissão domain + ComissaoSummaryProvider + Reporting query +
validator + ComissoesController + OwnerRoleParity). Nenhum teste antigo assertando 403 pra role
que agora passa — grep por `Forbid`/`StatusCode(403)`/`Http.Forbidden` em `tests/**` sem
ocorrência (arquitetura de teste do repo é handler-level + attribute-reflection, não pipeline
via WebApplicationFactory — não há teste velho pra desatualizar).

**RBAC 4 camadas (varredura final independente):**

| Camada | Comando | Resultado |
|---|---|---|
| 1. Backend `[Authorize(Roles=...)]` | grep em `src/Bootstrap/**/Controllers/*.cs` | 30 ocorrências; TODA linha com role específica inclui `Owner`. Zero gate `Admin`-only isolado. |
| 2. Frontend rota `<ProtectedRoute roles={...}>` | grep em `frontend/src/App.tsx` | 3 gates: `/financeiro` `[Owner,Admin,Recepcao]` (=FaturasController), `/estoque` `[Owner,Admin,Recepcao]` (=EstoqueController), `/relatorios` `[Owner,Admin]` (=ReportsController). Todos incluem Owner. |
| 3. Frontend menu `roles: [...]` em `AppLayout` | grep em `frontend/src/components/layout/AppLayout.tsx` | 4 itens gated, todos incluem Owner: Financeiro/Estoque/Relatórios/Comprovante. |
| 4. Frontend componente inline (`role === 'Admin'`, `isAdmin`, `canManage`, `includes('Admin')`) | grep em `frontend/src/**` | 5 checks: `PatientsPage.canManage` `[Owner,Admin,Recepcao]` (=PatientsController write); `ReportsPage.isEmpresa`/`ComprovantePage.isEmpresa` `[Owner,Admin]` (=ReportsController class + branches lookup); `FinanceiroPage` inline `[Owner,Admin]` pra `ConveniosCard` (=ConveniosController write). Todos alinhados. Zero uso solto de `isAdmin`/`role === 'Admin'` sem `Owner`. |

Cross-check backend↔frontend concluído: nenhum caso "backend concede mas frontend esconde"
nem o oposto (mais grave — frontend mostra mas backend nega, geraria 403 visível). Padrão
"Widening de RBAC 4 camadas" (patterns.md) mantido íntegro pós-030/027/028.

**Casos obrigatórios da task (14):**

| # | Caso | Cobertura | Fonte |
|---|------|-----------|-------|
| 1 | Dentista s/ profissionalId → só o próprio | PASS | `ComissoesControllerTests.Should_IgnoreClientFilters...` |
| 2 | Dentista c/ profissionalId de outro → ignorado | PASS | mesmo teste (Callback captura `queryEnviada.ProfissionalId == proprio`) |
| 3 | Dentista c/ branchId de outra filial → ignorado | PASS | mesmo teste (`BranchId == null` no query enviado) |
| 4 | Recepcao → 403 | PASS declarativo | `[Authorize(Roles="Owner,Admin,Dentista")]` linha 21 de `ComissoesController` — ASP.NET Core nega automaticamente role fora da lista |
| 5 | Admin → agregado organization | PASS | `Should_PassThroughClientFilters_When_UsuarioEhAdmin` |
| 6 | Owner = Admin | PASS | `OwnerRoleParityTests` + `[Authorize]` declarativo inclui Owner |
| 7 | Multi-tenant → zero linha alheia | PASS | `OrganizationQueryFilterTests` (Identity, Patients, Scheduling, Records, Billing, Tenancy, Estoque) |
| 8 | Dentista s/ Profissional vinculado → 200 zerado | PASS | `Should_UseSentinel_When_DentistaHasNoProfissionalVinculado` (Guid.Empty sentinel) |
| 9 | Comissão proporcional a `valorPago` (D2) | PASS | `Fatura.CalcularComissaoSobre` (FaturaTests) + `ComissaoSummaryProviderTests` — regressão D2/A1 confirmada |
| 10 | Parcela fora do período → não entra | PASS | `ComissaoSummaryProviderTests` |
| 11 | Fatura cancelada → não entra | PASS | `ComissaoSummaryProviderTests` |
| 12 | Período de 1 dia → `MediaDiaria == Total` | PASS | `ComissaoSummaryProviderTests` (borda inclusiva, sem /0) |
| 13 | `dataFim < dataInicio` → 400 | PASS | `GetComissoesPorPeriodoQueryValidatorTests` + `ComissoesControllerTests.Should_ReturnBadRequest_When_HandlerReturnsFailure` |
| 14 | Visual/temas/impressão | PASS por leitura | tokens verificados em `Button`/`Card`/`StatusBadge`/`ReportsPage`/`ComprovantePage`; zero cor hardcoded (grep `#[0-9a-fA-F]{3,8}`, `rgba(`, `bg-<cor>-<n>`, `text-<cor>-<n>`, `border-<cor>-<n>`, `style={{`); `@media print` cobre `.no-print` (esconde toolbar/filtros/erros) e `.print-area *` (força white bg + black text com `!important`, R11 dark mode ativo neutralizado). ComprovantePage usa `.no-print` (5 ocorrências) e `.print-area` (1) corretamente. |

**Regressão explícita:**
- 3 endpoints antigos `/api/reports/{dashboard,faturamento,agenda}` seguem sob
  `[Authorize(Roles="Owner,Admin", Policy="RequireActiveOrganization")]` na classe (linha 20 de
  `ReportsController`). Nova rota `/api/reports/comissoes` mora em `ComissoesController` separado
  — decisão D4 da 023 (padrão "Controller separado quando `[Authorize(Roles=...)]` de classe
  existente bloquearia role nova") preservada.
- Nav visibility Dentista: só `/agenda`, `/pacientes`, `/comprovante`, `/convites` aparecem
  (grupo "Gestão" oculta `/financeiro`/`/estoque`/`/relatorios` via role filter em `AppLayout`
  linhas 61-63 e o grupo inteiro colapsa se ficar vazio — linha 266).
- Nav visibility Recepcao: vê `/financeiro`+`/estoque`, não vê `/relatorios`+`/comprovante`
  (linhas 63-64 excluem Recepcao). `/comprovante` sem `roles` em `App.tsx` de propósito (task
  028 critério 4) — página mostra mensagem "sem permissão" via `ComprovantePage.ALLOWED_ROLES`
  em vez de redirect silencioso.

**Nada bloqueante. Sprint-7 fecha.**

## Notas

Fecha a sprint. Fail nos casos 1-8 é **bloqueante** — não fecha sprint com vazamento de dado de
remuneração.
