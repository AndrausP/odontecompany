---
task: "012"
sprint: "6"
status: done
---

# 012 — Rename no frontend (claims, tipos, api clients)

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Login funciona fim-a-fim contra o backend renomeado (task 010); nenhuma
referência a `tenant_id`/`unidade_id` no código do frontend; build TS 0 erro.

## Escopo técnico (Tech Lead)

O frontend **quebra no instante em que o backend muda a claim** — esta task tem que sair no
mesmo deploy da 010, não depois.

Alvos (`frontend/src/`):
- `types/auth.ts` — `AccessTokenClaims.tenant_id` → `organization_id`, `unidade_id` → `branch_id`.
- `lib/auth-store.ts` — decode/persistência do token.
- `lib/api-client.ts` — qualquer header/query de tenant.
- `features/*/api.ts` e `types/*.ts` — campos `tenantId`/`unidadeId` nos DTOs espelhados
  (patients, scheduling, billing, estoque, reports).
- Rotas chamadas: `/api/unidades` → `/api/branches` (se a task 011 entrar; se for cortada,
  esta parte sai junto).

## Decisão de UX (Tech Lead — não escalada, sem impacto de negócio)

**Código em inglês, label visível em pt-BR sem mudança.** O usuário final continua lendo
"Unidade" na tela; internamente é `Branch`. Para `Organization`, label visível fica
**"Organização"**. Trocar vocabulário de tela é retrabalho de Designer + reaprendizado do
usuário por zero ganho nesta rodada. Se o PO quiser alinhar a UI ao novo vocabulário, vira task
própria de Designer.

## Decisão do Architect

**Validação:** Nenhuma bloqueante. Contrato vem pronto de 010 (JWT claim `organization_id`, schema `organizations`) e parcial de 011 (schema `branches` se task for incluída nesta sprint).

---

## Contrato arquitetural

| Arquivo/Tipo | Mudança |
|--------------|---------|
| `frontend/src/types/auth.ts` | `AccessTokenClaims.tenant_id` → `organization_id`, `unidade_id` → `branch_id` |
| `frontend/src/lib/auth-store.ts` | Leitura de `organization_id` do token decodificado (em vez de `tenant_id`) |
| `frontend/src/lib/api-client.ts` | Qualquer header/query param referenciando tenant → organization |
| `frontend/src/features/*/api.ts` | Campos nos DTOs espelhados: `tenantId` → `organizationId`, `unidadeId` → `branchId` |
| `frontend/src/types/*.ts` | Tipos TypeScript de requests/responses |
| Rotas chamadas | `GET /api/organizations`, `GET /api/branches` (se 011 entrar) |

**Visual/Labels:** Código em inglês, labels para o usuário final permanecem em pt-BR ("Organização" → "Organização", sem mudança; "Unidade" → "Unidade" se task 011 cortada, ou "Branch" de forma INTERNA se entrar). Trocar UI de vocabulário é tarefa de Designer, fora desta sprint.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Rename full-stack, sem meio-termo. |
| 2. Contexto | Reader → Writer | 9 arquivos do frontend referenciam tenant/unidade. |
| 3. Quebra | Tech Lead | Esta task — mesmo deploy da 010. |
| 4. Estrutura | Architect | [pendente — só validação] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Frontend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | [pendente] |

## Metadados

- **Estimativa:** 1 dia
- **Depende de:** 010 (obrigatório) e 011 (parcial — só a parte de branches)
- **Bloqueia:** 018
- **Risco:** claim decodificada é string literal — TS não pega em runtime se sobrar
  `claims.tenant_id`. Grep obrigatório antes de fechar.

## Status

planned → in-progress → in-review (QA) → **done**

### Execução (Dev Frontend, 2026-08-18)

Rename mecânico feito arquivo por arquivo (`Edit` direcionado, sem script de substring em massa —
volume pequeno, 9 arquivos, risco de falso-cognato já mapeado pelo Reader/Dev Backend na 011).

**Confirmado por grep (case-insensitive) em `frontend/src`:**
- `tenant`: **zero ocorrências** em todo o diretório.
- `unidade`: só sobraram `unidadeMedida` (campo/id/label — conceito de unidade de medida de
  estoque, não Branch, propositalmente preservado) e um comentário de prosa em
  `types/auth.ts` ("...quando o usuário está restrito a uma unidade...") que descreve o conceito
  de negócio em pt-BR, não é identificador de código.

**O que foi renomeado:**
- `frontend/src/types/auth.ts` — `AccessTokenClaims.tenant_id`→`organization_id`,
  `unidade_id`→`branch_id` (claim JWT, mantido `snake_case` igual ao token real); comentário JSDoc
  atualizado.
- `frontend/src/lib/auth-store.ts` — comentário JSDoc (`tenant_id/role/unidade_id` →
  `organization_id/role/branch_id`). Nenhum código de runtime lia esses campos diretamente (só
  `claims.exp` e `claims.role` são usados em `AppLayout.tsx`/`ProtectedRoute.tsx`/`auth-store.ts`)
  — confirmado via grep por `claims\.` antes de editar, então não há risco de campo órfão.
- `frontend/src/lib/api-client.ts` — comentário (mesma troca de vocabulário).
- `frontend/src/types/scheduling.ts` — `Profissional.unidadeId`→`branchId`,
  `Sala.unidadeId`→`branchId` (DTOs em camelCase, igual ao JSON do backend).
- `frontend/src/types/estoque.ts` — `ItemEstoque.unidadeId`→`branchId`. `unidadeMedida` (mesma
  interface) **não tocado** — falso-cognato, mesmo achado documentado pelo Dev Backend na task 011
  (`ItemEstoque.UnidadeMedida` no backend é "un/kg/cx", não tem nada a ver com Branch).
- `frontend/src/types/reports.ts` — `DashboardResumo.tenantId`→`organizationId`.
- `frontend/src/features/estoque/api.ts` — `ListItensEstoqueParams.unidadeId`→`branchId`,
  `CreateItemEstoquePayload.unidadeId`→`branchId`. `unidadeMedida` no payload **não tocado**
  (mesmo motivo acima).

**Não mexido (fora de escopo, confirmado):**
- `frontend/src/features/estoque/EstoquePage.tsx` e `CreateItemEstoqueModal.tsx` — só usam
  `unidadeMedida` (campo + label pt-BR "Unidade de medida") e um `<th>` "Unidade" ligado à célula
  `item.unidadeMedida` — é o conceito de unidade de medida de estoque, não Branch. Nenhum dos dois
  arquivos referenciava `unidadeId`/`tenantId` em código.
- Rotas: grep por `api/unidade`, `api/branches`, `api/organizations`, `api/tenants` em
  `frontend/src` → **zero ocorrências**. O frontend ainda não consome nenhum endpoint de
  Branch/Organization diretamente (não existe tela de gestão de unidades/organizações no frontend
  ainda) — então o item "Rotas chamadas: `/api/unidades` → `/api/branches`" do contrato da task não
  se aplicou, não tinha chamada pra migrar (mesmo achado do Dev Backend na 010 sobre não existir
  `/api/tenants`).
- Labels visíveis pt-BR — não mudei nenhum texto de UI, conforme decisão do Tech Lead registrada
  na task ("Organização"/"Unidade" continuam como estavam, é troca de vocabulário técnico, não de
  UX).

**Validação:** Sem acesso a terminal/Bash neste ambiente de execução — não rodei `npm run build`
nem `tsc --noEmit` diretamente. Validação foi manual: (1) grep completo confirma zero
`tenant`/`Tenant` e zero `unidade`/`Unidade` fora dos casos de falso-cognato documentados; (2) grep
por todos os pontos de consumo dos campos renomeados (`claims\.`, `\.tenantId`, `organizationId`,
`branchId`) confirma que não sobrou nenhuma referência ao nome antigo em nenhum outro arquivo do
`frontend/src`; (3) inspeção visual de cada arquivo editado confirma que a mudança de tipo é
1-para-1 (nome do campo, sem mudança de tipo/estrutura) — não deveria introduzir erro de TS, mas
recomendo ao QA rodar `npm run build` real pra confirmar 0 erro, já que não pude executar.

**Critério de aceite:** zero `tenant_id`/`unidade_id` no código do frontend — confirmado por grep.
Build TS — **não validado por execução real**, só por inspeção estática (ver nota acima). Login
fim-a-fim contra backend renomeado — não testado (sem ambiente rodando/Postgres neste ambiente).

### Revisão QA (2026-08-18) — APROVADO

Build TS confirmado pelo broker (`npm run build`: 0 erro).

Contrato JWT frontend ↔ backend valida 1-para-1:
- `frontend/src/types/auth.ts` → `AccessTokenClaims { sub, organization_id, role, branch_id?, exp }` bate exatamente com `JwtTokenService.GenerateAccessToken` (`sub`, `organization_id`, `role`, `branch_id?` condicional, `jti` — `jti` não precisa no client).
- `frontend/src/lib/auth-store.ts` decodifica esse tipo com `jwtDecode<AccessTokenClaims>()`. Runtime só usa `claims.exp` (auth-store) e `claims.role` (AppLayout/ProtectedRoute) — nenhum código órfão referenciando campo antigo.

Grep case-insensitive por `tenant|unidade` em `frontend/src`:
- `tenant`: **zero ocorrência**.
- `unidade`: só `unidadeMedida` em Estoque (types, api, modal, page) — falso-cognato preservado, mesmo achado documentado pelo Dev Backend na task 011 (`ItemEstoque.UnidadeMedida` no backend é "un/kg/cx", nada a ver com Branch).
- Um comentário JSDoc pt-BR em `types/auth.ts` ("restrito a uma unidade — Fase 5") descreve o conceito de negócio em português, não é identificador.
- Labels visíveis "Unidade"/"Unidade de medida" na UI de Estoque — decisão explícita do Tech Lead (código em inglês, UI em pt-BR, sem retrabalho de vocabulário nesta sprint).

Nenhum bug encontrado. Aprovado.

**Risco residual:** login fim-a-fim contra backend renomeado NÃO foi exercido em runtime (nem Dev Frontend nem QA rodaram browser → backend real com claim `organization_id`). Se algum campo tiver casing/tipo divergente do que `jwt-decode` espera, só sabemos em runtime. Recomendo smoke-test manual de login na primeira janela em que o Postgres estiver disponível — fora do escopo desta review, pra registro do Writer.
