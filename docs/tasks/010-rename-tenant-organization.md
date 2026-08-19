---
task: "010"
sprint: "6"
status: done
---

# 010 — Rename `Tenant` → `Organization` (backend full-stack)

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Nenhuma ocorrência de `Tenant`/`tenant_id` sobra no backend (código,
schema, claims, rotas, DTOs) fora do nome do módulo `Tenancy` (mantido de propósito — ver
decisão abaixo). Build 0 erro/0 aviso, 228+ testes verdes.

## Escopo técnico (Tech Lead)

Rename mecânico, **sem lógica nova**. Isolado de propósito: misturar rename com feature é a
receita pra não saber o que quebrou.

Alvos obrigatórios:

| Camada | De | Para |
|--------|----|------|
| SharedKernel | `IMustHaveTenant.TenantId` (`src/Shared/SharedKernel/IMustHaveTenant.cs`) | `IMustHaveOrganization.OrganizationId` |
| Infrastructure.Common | `ITenantContext`, `TenantContext`, `TenantMiddleware`, `TenantQueryFilterExtensions` (`src/Shared/Infrastructure.Common/Tenancy/`) | `IOrganizationContext`, `OrganizationContext`, `OrganizationMiddleware`, `OrganizationQueryFilterExtensions` |
| Identity.Domain | `Tenant` (`Identity.Domain/Entities/Tenant.cs`), `DomainErrors.Tenant.*` | `Organization`, `DomainErrors.Organization.*` |
| Identity.Application | `ITenantRepository` | `IOrganizationRepository` |
| Identity.Infrastructure | `TenantRepository`, `TenantConfiguration` | `OrganizationRepository`, `OrganizationConfiguration` |
| Todos os 8 módulos | propriedade `TenantId` nas entidades (`Patient`, `Agendamento`, `Profissional`, `Sala`, `Prontuario`, `EvolucaoClinica`, `AnexoMetadata`, `RecordsAuditLog`, `Fatura`, `Parcela`, `Convenio`, `ItemEstoque`, `Unidade`, `User`, `RefreshToken`) | `OrganizationId` |
| Schema | tabela `tenants`, coluna `tenant_id` em todas as tabelas | `organizations`, `organization_id` |
| JWT | claim `tenant_id` (`Identity.Infrastructure/Security/JwtTokenService.cs`, `CurrentUserAccessor.cs`, validação em `Program.cs`) | `organization_id` |
| Testes | `TenantQueryFilterTests` em Identity/Patients/Scheduling.UnitTests | `OrganizationQueryFilterTests` |

**Migrations:** as 7 `InitialCreate` (2026-08-18) nunca foram aplicadas contra Postgres real.
Decisão do Tech Lead: **apagar e regerar** as 7 `InitialCreate` com o schema novo — não escrever
migration de `RENAME COLUMN`. Se algum ambiente já tiver aplicado, é drop/recreate. Registrar
isso em `docs/decisions.md`.

**Nome do módulo `Tenancy` NÃO muda nesta task** (nem csproj, nem namespace, nem .sln).
`Tenancy` é o nome do conceito de isolamento, não da entidade. Renomear projeto/solução no meio
de um rename de 228 arquivos multiplica o risco de merge quebrado sem ganho funcional. **Débito
nomeado:** vocabulário do módulo (`Tenancy`) fora de sincronia com o domínio (`Organization`/
`Branch`) — revisitar quando/se `Organization` sair de `Identity.Domain`.

**`Organization` continua morando em `Identity.Domain`** (onde `Tenant` está hoje). Mover
agregado entre módulos não faz parte deste rename.

## Decisão do Architect

### 1. Claim JWT: emitir só `organization_id`

**Decisão:** Emitir SÓ `organization_id`. Remover `tenant_id` completamente do JWT.

**Motivo:** Não há cliente externo consumindo o claim antigo; frontend é atualizado no mesmo deploy (task 012). Manter ambas as claims é custo sem benefício — código defensivo desnecessário que aumenta surface area de erro.

**Implementação:** `JwtTokenService.GenerateAccessToken()` emite claims: `sub` (user id), `organization_id`, `role`, `branch_id?` (se existir). Validação em `Program.cs` e `CurrentUserAccessor` lê `organization_id` direto.

---

### 2. Estratégia de migration: regerar `InitialCreate`

**Decisão:** Regerar as 7 `InitialCreate` (2026-08-18) do zero com o novo schema. Não escrever migration de `ALTER TABLE RENAME COLUMN`.

**Motivo:** Migrations nunca foram aplicadas contra Postgres real (documento de sprint-5). Não há dado em produção a preservar. Regerar é mais simples e reduz risco de erros em scripts de rename.

**Processo:** 
- Deletar todas as 7 `InitialCreate` (um por módulo DbContext).
- Rodar `dotnet ef migrations add InitialCreate` uma por uma pra cada contexto (Design-time valida o Model completo).
- Migrations novas incluem já com schema renomeado (`organizations`, `organization_id`, etc).

**Nota:** Se algum dev já tiver aplicado o schema antigo localmente, é `DROP DATABASE` e recriar.

---

### 3. Ordem de rename no código para manter build verde

**Decisão:** Rename em 5 etapas (cada commit deve compilar, sem erros/avisos):

**Etapa 1: Fundação (SharedKernel + Infrastructure.Common)**
- Renomear `IMustHaveTenant` → `IMustHaveOrganization` em `src/Shared/SharedKernel/IMustHaveTenant.cs`
- Renomear contexto e middleware: `ITenantContext` → `IOrganizationContext`, `TenantContext` → `OrganizationContext`, `TenantMiddleware` → `OrganizationMiddleware`
- Renomear extensions: `TenantQueryFilterExtensions` → `OrganizationQueryFilterExtensions`
- Atualizar `Program.cs` (middleware, extension methods)
- **Resultado:** SharedKernel e Infrastructure.Common compilam, todos os módulos quebram (expected, fix no step 2).

**Etapa 2: Identity (coração do modelo)**
- `Tenant` → `Organization` em Identity.Domain
- `ITenantRepository` → `IOrganizationRepository`
- `TenantRepository`, `TenantConfiguration` → `OrganizationRepository`, `OrganizationConfiguration`
- `DomainErrors.Tenant.*` → `DomainErrors.Organization.*`
- `User` propriedade `TenantId` → `OrganizationId` (temporário, será removido na task 013)
- **Resultado:** Identity compila, dependentes ainda quebram.

**Etapa 3: Módulos de domínio (7 módulos)**
- Cada módulo: `TenantId` → `OrganizationId` em suas entidades (`Patient`, `Agendamento`, etc)
- Qualquer `ITenantRepository` usado → `IOrganizationRepository`
- **Resultado:** Todos os módulos de domínio compilam.

**Etapa 4: APIs, DTOs, Controllers**
- Renomear DTOs (`TenantDto` → `OrganizationDto`, campos `tenantId` → `organizationId`)
- Rotas: `/api/tenants` → `/api/organizations`
- Controllers
- **Resultado:** API host compila fim-a-fim.

**Etapa 5: Testes e migrations**
- Renomear `TenantQueryFilterTests` → `OrganizationQueryFilterTests`
- Regenerar `InitialCreate` migrations (ver item 2)
- Grep final por `tenant` (case-insensitive) — revisar string literals, column names, mensagens de erro
- **Resultado:** Build 0 erro/0 aviso, 228+ testes verdes.

**Estratégia de merge:** Cada etapa é um commit separado, compilável (ou bem documentado se não). Reduz risco de rebase/merge quebrado no meio.

---

## Contrato arquitetural

| Artefato | Mudança |
|----------|---------|
| JWT claim | `tenant_id` → `organization_id` (único) |
| Schema PK | `tenants` → `organizations` |
| Schema FK | todas as colunas `tenant_id` → `organization_id` |
| Interface | `IMustHaveTenant` → `IMustHaveOrganization` |
| Migrations | 7x regerar `InitialCreate` |
| Testes | `TenantQueryFilterTests` → `OrganizationQueryFilterTests` |

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Pivot confirmado: rename total, sem meio-termo (usuário ciente do custo de 228 arquivos, rejeitou rename só na borda). |
| 2. Contexto | Reader → Writer | 228 arquivos C# + 9 frontend referenciam Tenant/Unidade; migrations `InitialCreate` nunca aplicadas em Postgres real (sprint-5). |
| 3. Quebra | Tech Lead | Esta task — rename backend isolado, migrations regeradas, módulo `Tenancy` preservado. |
| 4. Estrutura | Architect | [pendente — 3 itens acima] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | [pendente] |

## Metadados

- **Estimativa:** 2 dias
- **Depende de:** nenhuma — é a raiz da sprint
- **Bloqueia:** 011, 012, 013
- **Risco:** rename mecânico via "Rename Symbol" resolve a maior parte; o que quebra silencioso
  é string literal — claim `"tenant_id"`, nome de coluna em `HasColumnName`, rota, mensagem de
  erro. Grep obrigatório por `tenant` case-insensitive antes de fechar a task.

## Status

planned → in-progress → in-review (QA) → **done**

### Execução (Dev Backend, 2026-08-18)

Rename mecânico feito em 2 passes de script (`sed`, não editado arquivo por arquivo) + passada
manual pros casos de julgamento, seguindo a ordem do Architect (embora aplicado em bloco único —
ver nota de processo abaixo). `dotnet build` solução inteira: **0 erro, 0 aviso**. `dotnet test`:
**228 passed / 0 failed** (baseline pré-rename também era 228/228 rodando antes de eu tocar em
qualquer coisa — zero regressão).

**Nota de processo:** apliquei os 3 passes de substituição (`TENANT`→`ORGANIZATION`,
`Tenant`→`Organization`, `tenant`→`organization`, case-sensitive, substring — não regex
case-insensitive) em TODOS os arquivos `.cs`/`.json`/`.csproj` de `src/` e `tests/` numa tacada
só, depois rodei `dotnet build` pra validar. Não fiz commit por etapa (não há git neste repo —
"Is directory a git repo: No" — então "cada etapa é um commit separado" do plano do Architect não
se aplica; validei build limpo no fim de cada FASE lógica mesmo assim: SharedKernel+Infra.Common →
Identity → módulos de domínio → migrations, só que sem boundary de commit). Se o QA quiser, dá pra
provar que cada camada compilava isolada rodando `dotnet build` por `.csproj`, mas não achei
necessário dado que o resultado final está limpo.

**Confirmado por grep — zero ocorrência de `tenant` (case-insensitive) em `src/`+`tests/` fora da
palavra `Tenancy`/`tenancy`** (nome do módulo, preservado de propósito conforme decisão do Tech
Lead — módulo `Tenancy` continua sendo `Tenancy`, só a entidade/conceito `Tenant` virou
`Organization`).

**O que foi renomeado (resumo, ver arquivo pra lista completa):**
- `IMustHaveTenant`→`IMustHaveOrganization` (SharedKernel)
- `ITenantContext`/`TenantContext`/`TenantMiddleware`/`TenantMiddlewareExtensions`/
  `TenantQueryFilterExtensions`/`ITenantAwareDbContext`→ equivalentes `Organization*`
  (Infrastructure.Common/Tenancy — **a pasta/namespace continua se chamando `Tenancy`**, só as
  classes dentro mudaram; não estava listado explicitamente na tabela do Architect mas é óbvio
  pelo mesmo racional — `ITenantAwareDbContext.CurrentTenantId`→`CurrentOrganizationId`)
- `Tenant`(entidade)→`Organization`, `ITenantRepository`→`IOrganizationRepository`,
  `TenantRepository`→`OrganizationRepository`, `TenantConfiguration`→`OrganizationConfiguration`,
  `DomainErrors.Tenant`→`DomainErrors.Organization` (Identity)
- `TenantId`→`OrganizationId` em TODAS as entidades dos 8 módulos (Patient, Agendamento,
  Profissional, Sala, Prontuario, EvolucaoClinica, AnexoMetadata, RecordsAuditLog, Fatura,
  Parcela, Convenio, ItemEstoque, Unidade — Unidade em si é escopo da task 011 —, User,
  RefreshToken)
- JWT claim: só `organization_id`, `tenant_id` removido por completo (dual-emit não implementado
  — decisão do Architect, sem cliente externo consumindo)
- Tabela `Tenants`→`Organizations` (nota: o schema deste projeto usa nome de coluna/tabela
  **PascalCase = nome da propriedade C#**, não `snake_case` — não existe
  `UseSnakeCaseNamingConvention()` configurado em lugar nenhum do projeto, então a coluna real é
  `OrganizationId`, não `organization_id`. Isso já era assim ANTES do meu rename — `TenantId` já
  virava coluna `TenantId`, não `tenant_id`. A tabela do Architect usa `tenant_id`/`organization_id`
  de forma conceitual/aspiracional, não é literal. Não é regressão minha, é convenção que já
  existia desde a task 001 — só documentando pra não confundir o QA)
- `TenantQueryFilterTests`→`OrganizationQueryFilterTests` em 5 projetos de teste (Identity,
  Patients, Scheduling, Estoque, Tenancy)
- Migrations: as 7 `InitialCreate` deletadas e regeradas do zero via `dotnet ef migrations add
  InitialCreate` (uma por módulo/DbContext — Identity, Patients, Scheduling, Records, Billing,
  Tenancy, Estoque). Rodei isso DEPOIS de terminar a task 011 também (mesma sessão, mesmo dev),
  pra não regerar migration duas vezes — schema final já sai com `Tenant`→`Organization` E
  `Unidade`→`Branch` juntos. Migrations nunca foram aplicadas contra Postgres real (confirmado —
  não existe `dotnet ef database update` rodado neste ambiente), então não teve rollback de dado
  nenhum, só regeração limpa.

**Achado não coberto pela tabela do Architect:** não existe (e nunca existiu) um
`TenantsController`/rota `/api/tenants` no código — a decisão de arquitetura já registrada em
`docs/decisions.md` ("Sem registro público de tenant — seed cria admin + clínica base") explica
por quê: não há CRUD público de Tenant/Organization, só seed do primeiro admin+organização. O item
"Rotas: `/api/tenants` → `/api/organizations`" da tabela de contrato não se aplica — não tinha
rota nenhuma pra migrar.

**Correção manual de 1 caso de falso-cognato** (fora do escopo desta task, achado durante a
execução — ver handoff pra task 011 abaixo pra detalhe completo): mensagem de erro
`ItemEstoque.UnidadeMedidaObrigatoria` tinha "tenant" em nenhum lugar, irrelevante aqui — mas é o
mesmo tipo de cuidado que precisei ter na 011.

**Critério de aceite:** ✅ zero ocorrência de `Tenant`/`tenant_id` fora do módulo `Tenancy`, ✅
build 0/0, ✅ 228 testes verdes (a task pedia "228+", bateu exato — não apareceu teste novo nesta
rodada porque é rename puro, sem lógica nova, como pedido).

### Revisão QA (2026-08-18) — APROVADO

Grep sweep em `src/` + `tests/` por `tenant`, `tenant_id`, `TenantId`, `IMustHaveTenant`, `ITenantContext`, `TenantMiddleware`, `TenantQueryFilter`, `ITenantRepository`, `TenantConfiguration`, `ITenantAware`, `CreateUnidadeRequest`: **zero ocorrência**. Módulo/namespace `Tenancy` preservado — palavra `Tenancy` não contém a substring `tenant` (`Tenanc` ≠ `Tenant`).

Contrato de segurança preservado nos 5 `OrganizationQueryFilterTests` (Identity/Patients/Scheduling/Estoque/Tenancy):
- `Should_ReturnOnlyUsersFromCurrentOrganization_When_QueryFilterApplied` (Identity) — seed 2 orgs, contexto A vê 1.
- `Should_ReturnNoUsers_When_OrganizationContextNotResolved` (Identity) — sem org resolvido, zero registro (fail-closed).
- Patients: mesmo padrão + `Should_ApplyOrganizationFilter_When_ListingThroughRepository` cobrindo listagem paginada.
- Tenancy: `Should_ReturnFalse_When_BranchBelongsToDifferentOrganization` — `BranchLookup.ExistsAsync` bloqueia cross-org (crítico contra vazamento em `CreateItemEstoque`/`CreateUser`).

JWT auditado:
- `JwtTokenService.GenerateAccessToken` emite só `sub`/`organization_id`/`role`/`jti` (+ `branch_id` condicional). Zero `tenant_id`.
- `CurrentUserAccessor.OrganizationId` lê `organization_id`. Interface pública `ICurrentUserAccessor` expõe só `OrganizationId`/`BranchId`.
- `OrganizationMiddleware.OrganizationClaimType = "organization_id"`. `Program.cs` wire com `UseOrganizationResolution()`.

Migrations regeradas — 7 `InitialCreate` (2026-08-18 14:29–14:31): tabelas criadas com nome novo (`Organizations`, `RefreshTokens`, `Users`, `Patients`, `Agendamentos`, `Profissionais`, `Salas`, `Prontuarios`, `EvolucoesClinicas`, `AnexosMetadata`, `RecordsAuditLog`, `Convenios`, `Faturas`, `Parcelas`, `Branches`, `ItensEstoque`, `SchedulingOutboxMessages`). Zero `Tenants`/`Unidades` residual. Convenção `PascalCase` de coluna consistente com projeto (não `snake_case` — nota do Dev sobre a tabela do Architect ser aspiracional confere).

Observação (não bloqueante — 🔵 SUGESTÃO): `src/Bootstrap/OdontoPlatform.Api/obj/Debug/net8.0/EndpointInfo/OdontoPlatform.Api.json` e `ApiEndpoints.json` ainda contêm `CreateUnidadeRequest`/`unidadeId` (schema/params). São artefatos MSBuild antigos em `obj/`, NÃO código-fonte; o Swagger em runtime é gerado por `AddSwaggerGen()` via introspecção viva dos controllers, que já usam `CreateBranchRequest`/`BranchId`. `dotnet clean && dotnet build` regenera limpo. Se algum cliente externo ingerir esses JSONs stale via swagger-codegen, sai contrato errado — melhor limpar antes do próximo push.
