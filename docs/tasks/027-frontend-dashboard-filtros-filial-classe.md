---
task: "027"
sprint: "7"
status: done
---

# 027 — Dashboard com filtro de filial e classe de funcionário

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** FRONTEND
**Estimativa:** 2 dias
**Depende de:** 023 (endpoint `/api/reports/comissoes`), 025 (tokens/tema)
**Critério de aceite:**
1. `ReportsPage` ganha, além do período já existente, filtros de **filial** e **classe de
   funcionário**; mudar qualquer filtro refaz a consulta e atualiza os números.
2. Bloco novo de comissões: KPIs (comissão total, valor pago total, qtd. faturas, **média
   diária**) + tabela por profissional (nome, filial, classe, valor pago, comissão, média diária).
3. Filtros entram na `queryKey` do React Query (cache correto, sem dado velho de outro filtro).
4. Dropdown de filial alimentado por `GET /api/branches`; opção "Todas as filiais" como padrão.
5. Dropdown de classe com as roles reais (`Owner`/`Admin`/`Dentista`/`Recepcao`) + "Todas".
6. Estado vazio explícito ("Nenhuma comissão no período com esses filtros"), não tabela em branco.
7. Erro de API renderizado via `getApiErrorMessage`, mesmo padrão da página atual.
8. Tudo legível nos dois temas; zero cor hardcoded (só tokens da 025).
9. Os 3 blocos existentes (Resumo geral, Faturamento, Ocupação de agenda) **continuam funcionando**
   — esta task expande a tela, não substitui.

## Escopo técnico

- `frontend/src/features/reports/api.ts`: função `getComissoesResumo(params)` com
  `dataInicio`, `dataFim`, `branchId?`, `classe?`.
- `frontend/src/types/reports.ts`: tipos `ComissaoResumo` / `ComissaoLinha`, espelhando o DTO da 023.
- `ReportsPage.tsx`: barra de filtros conforme spec da 024 + bloco de comissões.
- Reaproveitar `Card`/`Select`/`Input`/`Label` já existentes (re-skinnados na 025). Se a tabela
  exigir um componente novo (`Table`), criá-lo em `components/ui/` seguindo a spec da 024 — não
  inventar estilo inline.
- Buscar filiais: `GET /api/branches` (Admin-only). Se a resposta vier 403 (usuário não-Admin),
  esconder o filtro de filial em vez de mostrar erro.

## Fora de escopo

- Gráficos (barra/linha). Não foram pedidos; KPI + tabela resolvem. Se quiserem depois, task nova.
- Propagar `branchId`/`classe` para os KPIs de pacientes/agenda — **corte consciente**, ver
  "Notas do Tech Lead" na sprint-7.

## Riscos

- **R9 (médio):** o filtro de classe sobre comissão devolve praticamente só `Dentista`/`Owner`
  (recepção não gera comissão). Sem estado vazio decente, parece bug pro usuário. Critério 6 é
  obrigatório, não opcional.
- **R10 (baixo):** `ReportsPage` já faz 3 queries; a quarta aumenta o tempo de carga. Manter o
  `enabled: periodoValido` que já existe e não bloquear a tela toda numa query só.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Dashboard com filtro de filial + classe (sprint-7) |
| 2. Contexto | Reader → Writer | ReportsPage atual: date range + 3 queries + cards |
| 3. Quebra | Tech Lead | Escopo acima |
| 4. Estrutura | Architect | [pendente — só o contrato do DTO, que já vem da 023] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Frontend | `api.ts` (getComissoesResumo/getBranches), `types/reports.ts` (ComissaoResumo/ComissaoLinha/Branch), `ReportsPage.tsx` expandida com bloco Comissões + filtros filial/classe. `npm run build` limpo. |
| 7. Teste | QA | [na 029] |
| 8. Documentação | Writer | [pendente] |

## Status

planned → in-progress → in-review (QA) → done (build limpo) → reaberta in-review (QA
detectou gating externo faltando Owner após revalidação por causa da task 030) → **done
(2026-08-19, revalidação final QA — gating externo corrigido em App.tsx + AppLayout.tsx +
PatientsPage.tsx + FinanceiroPage.tsx, comentário de reports/api.ts corrigido, varredura
completa do frontend confirma zero gate `Admin`-only isolado, backend confere paridade 1:1
com o que o frontend agora libera — nenhuma tela abre acesso que geraria 403).**

**QA (Jubileu) — 2026-08-19 (revalidação final):** PASS. Varredura completa do frontend
(`grep` por `=== 'Admin'`, `=== "Admin"`, `includes('Admin')`, `'Admin'` isolado e `roles:
[...]` em qualquer formato) devolve **zero ocorrência** de gate `Admin`-only sem `Owner`
junto. Backend reconfirmado 1:1:

- `BranchesController` → `Owner,Admin` = frontend `reports/api.ts:getBranches` gated pra
  `Owner|Admin` via `enabled` em `ReportsPage`/`ComprovantePage` ✔
- `ReportsController` → `Owner,Admin` = `App.tsx` rota `/relatorios` gated pra
  `['Owner','Admin']` ✔
- `EstoqueController` → `Owner,Admin,Recepcao` = `App.tsx` `/estoque` gated pra
  `['Owner','Admin','Recepcao']` ✔
- `FaturasController` → `Owner,Admin,Recepcao` (todos os verbos) = `App.tsx` `/financeiro`
  gated pra `['Owner','Admin','Recepcao']` ✔
- `PatientsController` → GET aberto pra qualquer papel; POST/PUT/DELETE `Owner,Admin,
  Recepcao` = `App.tsx` `/pacientes` sem role gate (leitura livre) + `canManage` no
  `PatientsPage` gated pra `Owner|Admin|Recepcao` (todas as ações de escrita) ✔
- `ConveniosController` → classe `Owner,Admin,Recepcao` (GET), POST `Owner,Admin` =
  `CreateFaturaModal` chama GET (Recepcao consegue listar convênios pra criar fatura sem
  403) + `ConveniosCard` visível pra `Owner|Admin` (frontend mais restritivo que o backend
  no card auxiliar, mas isso é seguro — nunca gera 403, apenas oculta feature de config) ✔
- `ComissoesController` → `Owner,Admin,Dentista` (rota interna, sem link direto no menu) =
  `ReportsPage` chama pra `Owner|Admin` via `enabled: isEmpresa`; `ComprovantePage` chama
  pra `Owner|Admin|Dentista` via `ALLOWED_ROLES` ✔

Nenhuma regressão em Admin/Recepcao/Dentista (nenhum role removido, só Owner adicionado
onde faltava).

🔵 SUGESTÃO (não bloqueia, sinalizado pro Writer registrar como saneamento futuro):
`frontend/src/features/billing/ConveniosCard.tsx:9` tem comentário desatualizado
("Admin-only") — hoje o card é renderizado pra `Owner|Admin` (task 030 deu paridade).
Trocar por "Owner+Admin-only" alinha o comentário com o comportamento real. Um único ponto
de doc desatualizada não justifica task própria — pode ser adendo no PR de saneamento da
task 030 ou fix de oportunidade.

**QA (Jubileu) — 2026-08-18 (revalidação):** FAIL. Reabrindo. O broker aplicou fix pontual em
`ReportsPage.tsx` (`isAdmin → isEmpresa` na query `branches` e no filtro `<Select>` de Filial),
o que internamente ficou correto — MAS o gating externo continua limitado a `['Admin']` em dois
pontos, tornando o fix interno **código morto pra Owner**:

- 🔴 CRÍTICO — `frontend/src/App.tsx:65` — `<ProtectedRoute roles={['Admin']}>` na rota
  `/relatorios`. Owner é redirecionado pra `/agenda` antes de `ReportsPage` renderizar. Fix
  necessário: `roles={['Owner', 'Admin']}`.
- 🔴 CRÍTICO — `frontend/src/components/layout/AppLayout.tsx:63` — item "Relatórios" do menu
  gated por `roles: ['Admin']`. Owner nem vê o link na sidebar. Fix necessário:
  `roles: ['Owner', 'Admin']`.

Consequência: `isEmpresa = role === 'Owner' || role === 'Admin'` dentro de `ReportsPage.tsx`
(linha 36) nunca é exercido pra Owner — o Owner é bloqueado ANTES do componente carregar. O
comportamento visível pro Owner é idêntico ao de antes do fix.

**Fix interno já validado** (não precisa refazer):
- `ReportsPage.tsx:36`: `isEmpresa` inclui Owner ✔
- `ReportsPage.tsx:68`: `enabled: isEmpresa` na query `branches` ✔
- `ReportsPage.tsx:108`: `{isEmpresa && (...)}` no `<Select>` de Filial ✔
- Filtro Classe (linha 121) usa `!isDentista` (comportamento pré-existente, Owner/Admin/Recepcao
  todos veem) — não é regressão do fix atual, sem alteração necessária.
- `npm run build` limpo.

Falta apenas: mudar gating externo (menu + rota). Volta pro Dev Frontend (ou broker faz —
edição de 2 linhas). Depois rebuild + revalidação e a task fecha.

**Não é regressão da 027 pura** — a 027 foi entregue quando `/api/branches` era Admin-only, e o
gating `['Admin']` no menu/rota era consistente com o backend. O problema surgiu quando a task
030 (paralelizada na mesma sprint) deu paridade de acesso ao Owner sem propagar as mudanças pra
todos os pontos de gating externo do frontend. Bug de coordenação de sprint, mas ainda assim
bug funcional em produção.

## Notas

Não depende da 026 — se a nav redesenhada for cortada, esta task segue normal.
