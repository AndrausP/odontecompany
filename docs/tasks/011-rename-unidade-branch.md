---
task: "011"
sprint: "6"
status: done
---

# 011 — Rename `Unidade` → `Branch`

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Nenhuma ocorrência de `Unidade`/`unidade_id` no backend; claim JWT
`branch_id`; rotas `/api/branches`. Build 0 erro/0 aviso, testes verdes.

## Escopo técnico (Tech Lead)

Segundo rename, separado do 010 de propósito: são conjuntos de arquivos parcialmente disjuntos
e o 010 já é grande o bastante. Alvos:

| Camada | De | Para |
|--------|----|------|
| Tenancy.Domain | `Unidade` (`Tenancy.Domain/Entities/Unidade.cs`) | `Branch` |
| Tenancy.Contracts | `IUnidadeLookup` | `IBranchLookup` |
| Tenancy.* | repositório, configuration, DTOs, commands, controller `/api/unidades` | `/api/branches` |
| Identity | `User.UnidadeId` | `User.BranchId` |
| Scheduling | `Profissional.UnidadeId`, `Sala.UnidadeId`, filtro `UnidadeId` em `ListAgendamentosQuery` | `BranchId` |
| Estoque | `ItemEstoque.UnidadeId` | `BranchId` |
| JWT | claim `unidade_id` (opcional, só quando escopado) | `branch_id` |
| `ICurrentUserAccessor` | `UnidadeId` | `BranchId` |
| Schema | tabela `unidades`, coluna `unidade_id` | `branches`, `branch_id` |

Mesma regra do 010: regerar `InitialCreate` dos contextos afetados (Tenancy, Identity,
Scheduling, Estoque), não escrever migration de rename.

Módulo `Tenancy` **mantém o nome** (mesma decisão da task 010).

## Decisão do Architect

**Nenhuma bloqueante.** O rename de `IBranchLookup` (contrato cross-module usado por `CreateUserCommand` no Identity) é mecânico — apenas o nome da interface e métodos mudam. Assinatura permanece idêntica, é injeção de dependência (nome de impl não quebra nada em tempo de design).

---

## Contrato arquitetural

| Artefato | Mudança |
|----------|---------|
| Entidade | `Unidade` → `Branch` em Tenancy.Domain |
| Contrato | `IUnidadeLookup` → `IBranchLookup` |
| Repositório | `IUnidadeRepository` → `IBranchRepository` |
| Rotas | `/api/unidades` → `/api/branches` |
| DTO/Configuration | `UnidadeDto` → `BranchDto`, etc |
| Schema | `unidades` → `branches`, `unidade_id` → `branch_id` |
| Claim JWT | `unidade_id` → `branch_id` (opcional, quando escopado) |
| Identity.User | `User.UnidadeId` → `User.BranchId` |
| Scheduling | `Profissional.UnidadeId` → `BranchId`, idem `Sala` |
| Estoque | `ItemEstoque.UnidadeId` → `BranchId` |
| `ICurrentUserAccessor` | Propriedade `UnidadeId` → `BranchId` |

**Estratégia de merge:** Após task 010 compilar (rename de Organization/tenant), esta tarefa é puramente paralela — toca conjuntos de arquivos disjuntos (Tenancy module principal, referências em Scheduling/Estoque). Depende de 010 só pra não ter dois grandes renames simultâneos — ordem é pré-caução de merge, não bloqueante técnica.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Rename total confirmado junto com Tenant→Organization. |
| 2. Contexto | Reader → Writer | `Unidade` é o 2º nível da hierarquia (Fase 5, task 009); referência solta por `Guid?`, sem FK de projeto. |
| 3. Quebra | Tech Lead | Esta task. |
| 4. Estrutura | Architect | [pendente] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | [pendente] |

## Metadados

- **Estimativa:** 2 dias
- **Depende de:** 010 (evita dois renames simultâneos nos mesmos arquivos)
- **Bloqueia:** parte do 012 (labels/tipos de unidade no frontend)
- **Risco:** baixo. **Esta é a primeira task a ser cortada se a sprint estourar** — é o único
  item que não bloqueia o pivot de multi-org. Se cortada, vai integral pra sprint 7.

## Status

planned → in-progress → in-review (QA) → **done**

### Execução (Dev Backend, 2026-08-18, na sequência direta da task 010, mesma sessão)

Mesma técnica de rename scriptado da 010, rodada DEPOIS da 010 estar 100% verde (build+test).
`dotnet build`: **0 erro, 0 aviso**. `dotnet test`: **228 passed / 0 failed** (mesmo baseline —
rename puro, nenhum teste novo, nenhuma regressão).

**Achado crítico não mencionado na task nem pelo Architect — falso-cognato `UnidadeMedida`:**
`Estoque.Domain.Entities.ItemEstoque` tem DUAS propriedades com "Unidade" no nome que são
conceitos **completamente diferentes**:
- `UnidadeId` (Guid?) — referência à Branch/clínica física (ESTE é o escopo da rename)
- `UnidadeMedida` (string) — "unidade de medida" do item de estoque (ex: "un", "kg", "cx") — **NÃO
  tem nada a ver com Branch**, é sobre unidade de medida/measurement unit

Um rename ingênuo de substring `Unidade`→`Branch` teria transformado `UnidadeMedida` em
`BranchMedida` (sem sentido) e a mensagem de erro `"Unidade de medida é obrigatória."` em `"Branch
de medida é obrigatória."` (errado semanticamente — troquei o conceito errado). **Protegi
`UnidadeMedida`/`unidadeMedida` com um placeholder antes de rodar o rename e restaurei depois**
(mesma técnica: `sed` com token de proteção temporário). Peguei 1 ocorrência que escapou da
proteção automática porque estava como TEXTO LIVRE com espaço (`"Unidade de medida é
obrigatória."`, na mensagem de erro, não como identificador `UnidadeMedida` colado) — corrigida na
mão em `Estoque.Domain/Errors/DomainErrors.cs` (`ItemEstoque.UnidadeMedidaObrigatoria`). Se o QA
rodar grep case-insensitive por `unidade` vai achar exatamente essas ocorrências de
`UnidadeMedida`/`unidadeMedida` — são intencionais, não sobrou nada do conceito de Branch/local
físico com esse nome.

**Regra geral que vale registrar pro próximo rename mecânico:** antes de rodar substring replace
em massa, `grep -o` por todo token que CONTÉM a palavra-alvo (não só a palavra isolada) pra achar
falsos-cognatos tipo esse ANTES de rodar o script, não depois. Fiz isso via
`grep -rohiE "[a-zA-Z_]*unidade[a-zA-Z_]*" ... | sort -u` — listou todos os ~50 tokens distintos
que continham "unidade", e foi olhando essa lista que achei o `UnidadeMedida`.

**O que foi renomeado:**
- `Tenancy.Domain.Entities.Unidade`→`Branch`, arquivo `Unidade.cs`→`Branch.cs`
- `IUnidadeLookup`→`IBranchLookup` (Tenancy.Contracts, contrato cross-module usado por
  `CreateUserCommand` do Identity — troca só de nome, assinatura idêntica, sem bloqueante como o
  Architect já havia previsto)
- `IUnidadeRepository`→`IBranchRepository`, `UnidadeRepository`→`BranchRepository`,
  `UnidadeConfiguration`→`BranchConfiguration`, `UnidadeDto`→`BranchDto`,
  `UnidadeMappingExtensions`→`BranchMappingExtensions`, `UnidadeLookup`→`BranchLookup`
- Commands/Queries + PASTAS renomeadas (não só arquivo): `Commands/CreateUnidade/`→
  `Commands/CreateBranch/` (+ `CreateUnidadeCommand`→`CreateBranchCommand` e Handler/Validator),
  `Commands/DeactivateUnidade/`→`Commands/DeactivateBranch/` (idem),
  `Queries/ListUnidades/`→`Queries/ListBranches/` (`ListUnidadesQuery`→`ListBranchesQuery` — nota:
  plural `Unidades`→`Branches`, não `Branchs`; troquei a ordem do script pra fazer o replace do
  plural ANTES do singular, senão "Unidades" viraria "Branchs" ao invés de "Branches" — inglês não
  pluraliza por sufixo simples igual português)
- Controller: `UnidadesController.cs`→`BranchesController.cs`, rota `/api/unidades`→
  `/api/branches`, `[Authorize(Roles = "Admin")]` preservado sem mudança
  (`api/branches` continua Admin-only, igual antes)
- `User.UnidadeId`→`BranchId` (Identity), `Profissional.UnidadeId`/`Sala.UnidadeId`→`BranchId`
  (Scheduling), filtro `UnidadeId` em `ListAgendamentosQuery`→`BranchId`,
  `ItemEstoque.UnidadeId`→`BranchId` (Estoque, **não confundir com `UnidadeMedida` que NÃO
  mudou**, ver acima)
- `ICurrentUserAccessor.UnidadeId`→`BranchId`, `CurrentUserAccessor` lê claim `unidade_id`→
  `branch_id`, `JwtTokenService` emite `branch_id` só quando o usuário está escopado (mesmo
  comportamento condicional de antes, só o nome da claim mudou)
- Testes: `AgendamentoListByUnidadeTests`→`AgendamentoListByBranchTests`,
  `CreateUnidadeCommandHandlerTests`→`CreateBranchCommandHandlerTests`,
  `UnidadeTests`→`BranchTests` — todos os métodos `[Test]` internos também tiveram `Unidade`→
  `Branch` no nome (ex: `Should_CreateBranch_When_DataIsValid`)
- Migrations: regeradas junto com a task 010 (ver nota lá — rodei os 7 `dotnet ef migrations add
  InitialCreate` DEPOIS de terminar tanto 010 quanto 011, pra sair um schema final só com as duas
  renomeações já aplicadas). Índice `IX_Unidades_OrganizationId_Nome`→
  `IX_Branches_OrganizationId_Nome` confirmado na migration nova.
- Módulo `Tenancy` **não foi renomeado** (nem namespace, nem csproj, nem pasta) — confirmado, é a
  mesma decisão da 010, só reafirmando que não toquei nisso por engano.

**Critério de aceite:** ✅ zero ocorrência de `Unidade`/`unidade_id` fora de `UnidadeMedida` (que
é outro conceito, ver acima), ✅ claim `branch_id`, ✅ rotas `/api/branches`, ✅ build 0/0, ✅ 228
testes verdes.

### Revisão QA (2026-08-18) — APROVADO

Falso-cognato `UnidadeMedida` confirmado preservado via grep case-sensitive em `src/Modules/Estoque` — todas as ocorrências restantes de "unidade" no backend estão em contexto correto:
- `ItemEstoque.UnidadeMedida` (entidade), `ItemEstoqueDto.UnidadeMedida`, `CreateItemEstoqueCommand.UnidadeMedida`, validator, migration `Property<string>("UnidadeMedida")`, config, `CreateItemEstoqueRequest.UnidadeMedida`, mensagem `"Unidade de medida é obrigatória."` em `DomainErrors.ItemEstoque.UnidadeMedidaObrigatoria`.
- Todas as outras propriedades de branch em Estoque estão como `BranchId`: entidade, DTO, command, config, migration (`IX_ItensEstoque_OrganizationId_BranchId_Nome`), handler, repository, `IBranchLookup`, `DomainErrors.ItemEstoque.BranchInvalida`.

Também renomeado corretamente:
- `Branch.cs` em Tenancy.Domain (implementa `IMustHaveOrganization`, `OrganizationId` + `BranchId` próprio Guid).
- `BranchesController` (rota `/api/branches`, `[Authorize(Roles = "Admin")]` preservado, `CreateBranchRequest(Nome, Endereco)`).
- Commands/Queries com PASTAS renomeadas: `Commands/CreateBranch/`, `Commands/DeactivateBranch/`, `Queries/ListBranches/` — plural inglês correto (`Branches`, não `Branchs`), replace do plural antes do singular confirma pela ordem dos handlers.
- `User.BranchId` (Identity), `Profissional.BranchId`/`Sala.BranchId` (Scheduling), `ItemEstoque.BranchId` (Estoque) — todos como `Guid?` (opcional).
- `ICurrentUserAccessor.BranchId`, `JwtTokenService` emite `branch_id` só quando `branchId is not null` (comportamento condicional preservado).
- `IBranchLookup.ExistsAsync(organizationId, branchId)` — o teste `Should_ReturnFalse_When_BranchBelongsToDifferentOrganization` em Tenancy.UnitTests prova que checar branch de org errada não vaza.

Grep case-insensitive por `\bUnidade\b|\bunidade\b|UnidadeId|unidadeId|unidade_id` em `tests/`: **zero ocorrência** (todos os testes `AgendamentoListByUnidadeTests`, `CreateUnidadeCommandHandlerTests`, `UnidadeTests` renomeados corretamente, sem sobra do nome antigo nos `[Test]` methods).

Nenhum bug encontrado. Aprovado.
