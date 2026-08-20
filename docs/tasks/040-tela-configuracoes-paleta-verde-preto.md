---
task: "040"
sprint: "11"
status: done
---

# 040 — Tela de configurações (Empresa/Unidades/Plano) + paleta dark verde+preto + cards de módulo

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BACKEND (2 endpoints novos) + FRONTEND
**Depende de:** 039 (sistema de planos) — fecha o item "fora de escopo" que a 039 tinha anunciado
como futuro ("tela de configurações... item futuro").
**Critério de aceite:**
1. Tela `/configuracoes` (Owner/Admin) edita Nome/Cnpj/Telefone/Endereço da empresa.
2. Mesma tela lista e edita Nome/Endereço/Telefone das unidades (modal).
3. Mesma tela mostra o plano ativo e permite trocar (reusa os cards de plano do onboarding).
4. Cards de plano (onboarding + configurações) e itens de nav da sidebar redesenhados — pedido do
   usuário "melhora os módulos".
5. Paleta dark trocada de teal-ciano/roxo/azul pra verde+preto (light já era verde+branco, sem
   mudança).
6. `dotnet build`/`dotnet test` e `npm run build`/`npm run lint` limpos.

## Contexto

Pedido do usuário: "faça a parte da tela de config, melhora os módulos, além de trocar a paleta
de cores pra verde e branco ou verde e preto". Grill-me (3 perguntas) resolveu: (1) dark vira verde
+ preto puro, sem teal/roxo/azul remanescente; (2) "módulos" = cards de plano E itens de nav da
sidebar; (3) tela de config edita Empresa+Unidade+Plano (não só Empresa) — é exatamente o item que
a task 039 tinha deixado como "fora de escopo... item futuro".

## Execução

### Backend

- `Organization.AtualizarDados`/`Branch.AtualizarDados` (domínio) — mesma validação de `Create`.
- `Identity.Application`: `GetOrganizationQuery`/`UpdateOrganizationCommand` (+handlers,
  validators), `OrganizationDto` novo em `Identity.Application.DTOs`.
- `Tenancy.Application`: `UpdateBranchCommand` (+handler, validator), reusa `BranchDto` existente.
- `OrganizationsController`: `GET`/`PUT /api/organizations/{id}` (Owner/Admin, mesma defesa IDOR
  do resto do controller — `id` tem que bater com o `organization_id` do token).
- `BranchesController`: `PUT /api/branches/{id}` — `UpdateBranchCommand` recebe `OrganizationId`
  do token (nunca da rota); handler confere `branch.OrganizationId == request.OrganizationId` e
  devolve `Branch.NaoEncontrada` (404) nos dois casos (branch inexistente OU de outra
  organization) — fechado numa reabertura da mesma task, ver docs/decisions.md. `DELETE
  /api/branches/{id}` (`DeactivateBranchCommand`) ganhou o mesmo tratamento numa 2ª reabertura
  logo em seguida — `OrganizationId` do token, mesmo `Branch.NaoEncontrada` nos dois casos.

### Frontend

- `features/organizations/SettingsPage.tsx` novo — 3 seções (Empresa/Unidades/Plano), rota
  `/configuracoes` (Owner/Admin).
- `features/subscriptions/PlanCards.tsx` novo — grid de 3 cards (era lista de linhas), com
  `activeTier` opcional pra diferenciar "plano atual" de "trocar"; reusado por `SelectPlanStep`
  (onboarding) e `SettingsPage`.
- `AppLayout.tsx`: item de nav ganhou chip de ícone (fundo arredondado), novo grupo "Sistema" com
  o link Configurações (Owner/Admin).
- `index.css`: `.dark` trocou `--color-brand`/`--color-brand-hover`/`--color-brand-subtle`/
  `--color-focus-ring` de teal-ciano (`#2DD4BF`) pra verde (`#22C55E`/`#4ADE80`); gradiente do
  botão primário achatado (era teal→azul, 2 tons fora da marca); flourish decorativo da sidebar
  trocou o 2º radial de roxo (`#7C6CF0`) pra verde. Light não mudou (já era verde+branco).

## Fora de escopo

- Downgrade de plano com validação de limite de filial (mesma dívida técnica assumida na 039 —
  `SelectPlanCommand` ainda não confere).
- Editar Cnpj com máscara/validação de formato — segue cru, mesma regra da criação (039).

## Verificação

- `dotnet build OdontoPlatform.sln` — limpo.
- `dotnet test OdontoPlatform.sln` — 353 testes, 0 falhas (+12 novos: `UpdateOrganizationCommandHandlerTests`,
  `GetOrganizationQueryHandlerTests`, `UpdateBranchCommandHandlerTests` e
  `DeactivateBranchCommandHandlerTests` — os 2 últimos cobrem o caso de IDOR
  (`Should_ReturnNaoEncontrada_When_BranchBelongsToDifferentOrganization`)).
- `npm run build`/`npm run lint` — limpos.
- Validado AO VIVO via claude-in-chrome: signup → onboarding (3 passos, cards novos, paleta verde
  +preto confirmada) → `/configuracoes` (GET pré-preenche Empresa, editar+salvar Unidade via modal
  funcionou, `PlanCards` mostra "Plano atual" no tier certo) → nav sidebar com chip de ícone e
  grupo "Sistema" confirmados no drawer mobile.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Tela de config (Empresa/Unidade/Plano) + módulos melhorados + paleta verde+preto |
| 2. Contexto | Reader → Writer | Task 039 mapeada (item "fora de escopo" citando esta tela); `index.css`/`AppLayout`/`SelectPlanStep` mapeados |
| 3. Quebra | Tech Lead | 1 task — 2 endpoints novos (Organization/Branch) + tela nova + redesign de cards + paleta |
| 4. Estrutura | Architect | DTO/Query/Command seguindo o padrão já existente (`CreateOrganization`/`CreateBranch`); `PlanCards` extraído como componente compartilhado |
| 5. Aprovação | Product Owner | Aprovado (grill-me) |
| 6. Implementação | Dev Backend / Dev Frontend | Endpoints + `SettingsPage` + `PlanCards` + paleta + nav |
| 7. Teste | QA | `dotnet test` (349/349), `npm run build`/`lint`, validação end-to-end real via browser |
| 8. Documentação | Writer | `docs/knowledge/patterns.md`, `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
