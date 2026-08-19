---
task: "013"
sprint: "6"
status: done
---

# 013 — `OrganizationMembership` N:N + papel `Owner`

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Um mesmo `User` pode estar afiliado a 2+ `Organization` com papéis
diferentes em cada uma. `User` deixa de carregar `OrganizationId`/`Role` diretos. Papel `Owner`
existe e é atribuído a quem cria a organização. Isolamento por organização continua garantido
(teste de query filter verde em todos os módulos).

## Escopo técnico (Tech Lead)

Esta é a task que **quebra o modelo atual** — `User.OrganizationId` + `User.Role` 1:1 viram
relação N:N. É o coração do pivot, e o maior risco da sprint.

Entrega:
1. Entidade nova `OrganizationMembership` (Identity.Domain): `UserId`, `OrganizationId`, `Role`,
   `Status` (Ativo/Inativo), timestamps. Chave única `(UserId, OrganizationId)`.
2. `Role` ganha `Owner` — **aditivo**, não substitui `Admin`/`Dentista`/`Recepcao`. RBAC
   existente segue valendo; `Owner` é o teto (quem criou a org, pode convidar e transferir).
3. `User` perde `OrganizationId` e `Role`. Mantém `Nome`, `Email` (único **global**),
   `PasswordHash`, `Ativo`. `BranchId` migra pra `OrganizationMembership` (escopo de filial é por
   afiliação, não por pessoa).
4. `SeedFirstAdmin` passa a criar `Organization` + `User` + `Membership(Owner)`.
5. `CreateUserCommand` (admin cria user dentro da org) passa a criar `User` + `Membership` na org
   do chamador — comportamento equivalente ao de hoje, agora via membership.

## Decisão do Architect — BLOQUEANTE RESOLVIDO

### 1. `User` vira entidade GLOBAL (perde `IMustHaveOrganization`)

**Decisão:** `User` NÃO implementa mais `IMustHaveOrganization` após esta task. Perde propriedade `OrganizationId`.

**Motivo:** 
- Email é único global hoje — `User` já é semanticamente global.
- Afiliação a org(s) é responsabilidade de `OrganizationMembership`, não de `User`.
- Simplifica modelo: um usuário existe, ponto; ele pode ter 0 a N memberships (em 0 a N orgs).
- Query filter de `User` é removido de `IdentityDbContext.ApplyTenantQueryFilters()`.

**Contrato:**
```csharp
public class User : AggregateRoot
{
    public string Nome { get; private set; }
    public string Email { get; private set; }  // Único global
    public string PasswordHash { get; private set; }
    public Guid? BranchId { get; private set; }  // Migra pra OrganizationMembership
    public bool Ativo { get; private set; }
    
    // Removido: OrganizationId, Role
    // Adicionado: nenhum campo de org-scoping direto
}
```

**Migração de dados:** Seed do primeiro admin (`SeedFirstAdmin`) passa a:
1. Criar `User` (sem org)
2. Criar `Organization`
3. Criar `OrganizationMembership(Owner, Ativo)` linkando User→Org
4. Copiar `User.BranchId` → `Membership.BranchId` (se usar branch-scoping por membership, decisão de UX, mas provavelmente admin vê tudo = null)

**Teste:** `OrganizationQueryFilterTests` passa a testar `OrganizationMembership`, não `User`. `User` nunca é filtrado por org (query global).

---

### 2. `RefreshToken.OrganizationId` é REMOVIDO

**Decisão:** `RefreshToken` não carrega `OrganizationId`. É do USUÁRIO, não da sessão-numa-org.

**Contrato:**
```csharp
public class RefreshToken : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }  // NOVO: escopado a org
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    
    // (mantém OrganizationId de IMustHaveOrganization para query filter)
}
```

**Motivo:** Refresh token é emitido no login, que já eleita uma org. Se usuário trocar de org depois via `switch-organization`, o token de acesso novo é emitido, novo refresh token é emitido também. Refresh token é de curta validade + rotativo — não precisa de "portabilidade entre orgs".

**Espera, contradição acima.** Deixa eu clarificar:

Refresh token É de uma org (a que foi eleita no login). Se usuário trocar de org, emitimos novo refresh token (junto com novo access token). Simples assim.

Portanto: `RefreshToken.OrganizationId` EXISTE e é escopado a `IMustHaveOrganization`. Apenas NÃO carrega `TenantId` separado (era redundância — `OrganizationId` já define o escopo).

Schema: `refresh_tokens` terá `organization_id` (renomeado de `tenant_id` na task 010), `user_id`, `token_hash`, etc. Nada chamado `tenant_id` separado.

---

### 3. `OrganizationMembership` mora em `Identity.Domain`

**Decisão:** Sim, junto de `User` e `Organization`.

**Motivo:**
- `User` e `Organization` já estão em Identity.Domain (raiz do isolamento).
- `OrganizationMembership` é a relação N:N entre eles — semanticamente pertence lá.
- Evita criar dependência circular cross-module: Tenancy depende de Identity pra User, não o inverso.

**Contrato:**
```csharp
public class OrganizationMembership : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public Role Role { get; private set; }  // Owner, Admin, Dentista, Recepcao
    public MembershipStatus Status { get; private set; }  // Ativo, Inativo
    public Guid? BranchId { get; private set; }  // Opcional, se RBAC por unidade
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Índice único: (OrganizationId, UserId)
    
    public static Result<OrganizationMembership> Create(Guid organizationId, Guid userId, Role role, Guid? branchId = null)
    { ... }
    
    public void Deactivate() => Status = MembershipStatus.Inativo;
}

public enum MembershipStatus
{
    Ativo = 1,
    Inativo = 2
}
```

**Navegações no DbContext:**
```csharp
modelBuilder.Entity<User>()
    .HasMany(u => u.OrganizationMemberships)
    .WithOne()
    .HasForeignKey(m => m.UserId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<Organization>()
    .HasMany(o => o.Members)  // Navigation ICollection<OrganizationMembership>
    .WithOne()
    .HasForeignKey(m => m.OrganizationId)
    .OnDelete(DeleteBehavior.Cascade);
```

---

### 4. Migration: REGERAR `InitialCreate`

**Decisão:** Regerar `InitialCreate` de Identity (e qualquer outro módulo que toque User/Organization/Membership). Mesma regra de task 010.

**Processo:**
1. Deletar `Identity/Infrastructure/Migrations/InitialCreate`
2. Rodar `dotnet ef migrations add InitialCreate` pra Identity
3. Schema novo já inclui:
   - `organizations` (antes `tenants`)
   - `users` (sem `organization_id`, sem `role` — agora global)
   - `organization_memberships` (novo) com `(organization_id, user_id, role, status, branch_id?, created_at, updated_at, index unique (organization_id, user_id))` 
   - `refresh_tokens` com `organization_id` (antes `tenant_id`, renomeado em task 010)

**Seed:** `SeedFirstAdmin` de teste:
```csharp
var user = User.Create("Admin", "admin@test.com", hashedPassword).Value;
var org = Organization.Create("Clínica Teste").Value;
var membership = OrganizationMembership.Create(org.Id, user.Id, Role.Owner).Value;

context.Users.Add(user);
context.Organizations.Add(org);
context.OrganizationMemberships.Add(membership);
await context.SaveChangesAsync();
```

---

## Contrato arquitetural

| Artefato | Mudança |
|----------|---------|
| Entidade `User` | Perde `OrganizationId` (vira global), perde `Role` (agora em Membership) |
| Entidade nova `OrganizationMembership` | Identity.Domain; (UserId, OrganizationId) 1:N com index único |
| `IMustHaveOrganization` | `User` deixa de implementar, `OrganizationMembership` implementa |
| `IdentityDbContext` | Remove `User` de `ApplyTenantQueryFilters()`, adiciona `OrganizationMembership` |
| `RefreshToken` | Mantém `OrganizationId`, perde campo `TenantId` separado (redundante) |
| Schema | `users` sem `organization_id` + `role`, nova tabela `organization_memberships` |
| Migrations | Regerar Identity `InitialCreate` |
| Testes | Ajustar seed + `OrganizationQueryFilterTests` (User não é filtrado) |
| `CreateUserCommand` | Passa a criar `User` + `Membership` na org do chamador, em transação |

---

## Riscos residuais

- **Isolamento:** Se `OrganizationMembership` não cair no filter corretamente, admin de org A consegue ver memberships de org B. Teste obrigatório em QA (ver task 019).
- **RefreshToken:** Se `OrganizationId` não for escopado, refresh token de uma org pode ser reusado em outra. Teste em QA também.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Dentista pode atuar em 2+ clínicas; quem cria a org vira Owner. |
| 2. Contexto | Reader → Writer | Hoje `User.TenantId` (1:1) + `User.Role` (1:1); JWT carrega ambos; login resolve tenant pelo email. |
| 3. Quebra | Tech Lead | Esta task — só modelo + persistência + seed. Auth/JWT fica na 014. |
| 4. Estrutura | Architect | [pendente — 4 itens acima, bloqueantes] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | Fechado. Padrão N:N registrado em `docs/knowledge/patterns.md`; débito de FK ausente registrado em `docs/knowledge/errors-aprendidos.md`. |

## Metadados

- **Estimativa:** 3 dias
- **Depende de:** 010
- **Bloqueia:** 014, 015, 016, 017
- **Risco:** ⚠️ alto. Mexe no invariante de isolamento multi-tenant que sustenta 8 módulos. Se o
  filter de `User` for removido sem substituto, abre vazamento entre organizações. Teste de
  isolamento é critério de aceite, não opcional.

## Status

planned → in-progress → in-review (QA) → QA pass → **done** (Writer, 2026-08-18)

### Notas QA (2026-08-18)

Validado por leitura de código + suite existente (248/248). Nenhum bug bloqueante.

- **Isolamento de `OrganizationMembership`**: confirmado. `ApplyOrganizationQueryFilters` varre
  o modelo por `IMustHaveOrganization` via reflection e aplica filter no
  `IdentityDbContext.OnModelCreating` — `OrganizationMembership` cai automaticamente
  (`Identity.Domain.Entities.OrganizationMembership : Entity, IMustHaveOrganization`). Teste
  `Should_ReturnOnlyMembershipsFromCurrentOrganization_When_QueryFilterApplied` no
  `OrganizationQueryFilterTests.cs` (Identity.UnitTests) valida isolamento efetivo via provider
  InMemory (não é só compilável — usa 2 orgs seedadas e assert de contagem+id).
- **`User` global**: confirmado. `User` não implementa mais `IMustHaveOrganization` (removido em
  `Entities/User.cs`), não aparece no filter, e o teste
  `Should_ReturnAllUsers_Regardless_Of_OrganizationContext_Because_UserIsGlobal` prova que mesmo
  com um `organizationContext` arbitrário resolvido, `Users.ToListAsync()` devolve todo mundo.
  Login por email (`UserRepository.GetByEmailAsync`) roda sem `IgnoreQueryFilters` de propósito —
  não há filtro pra contornar.
- **`RefreshToken.OrganizationId` cross-org**: confirmado impossível.
  `RefreshTokenCommand` só carrega a string do token (nunca `organizationId` do cliente); handler
  extrai `existingToken.OrganizationId` do próprio token persistido e passa direto pro
  `JwtTokenService.GenerateAccessToken` — sem oportunidade de spoofing. Testes novos
  `Should_ReturnInvalido_When_MembershipForTokenOrganizationIsMissing/IsInactive` cobrem o caso
  "usuário perdeu a afiliação" (risco residual da própria 013).
- **Migration `InitialCreate`**: schema confere.
  `20260818163423_InitialCreate.cs`: `Users` sem `OrganizationId`/`Role`, `OrganizationMemberships`
  nova com `IX_OrganizationMemberships_OrganizationId_UserId` UNIQUE + `IX_...UserId` de apoio,
  `RefreshTokens` com `OrganizationId` (sem `TenantId` solto), `Users.Email` UNIQUE global.
- **Domain rules**: `OrganizationMembershipTests.cs` novo cobre create válido, com/sem
  `BranchId`, `OrganizationId`/`UserId` vazios (Result.Failure), e `Desativar()`.

**Observação não-bloqueante (baixo risco):** `OrganizationMembershipConfiguration` e
`UserConfiguration` não definem FK `HasForeignKey(...).OnDelete(Cascade)` que o Architect listou
na spec (item 3 da decisão). Migration reflete: nenhuma `ForeignKey` em `OrganizationMemberships`.
Como `User.Desativar()` e `Organization.Desativar()` são soft-delete (nunca hard delete no
código), não há caminho que cause membership órfã em produção. Divergência da spec, sem risco
funcional. Se aparecer necessidade de hard-delete no futuro, adicionar FKs vira débito.

### Notas Dev Backend (retomada de sessão, 2026-08-18)

Implementação (Domain/Application/Infrastructure/Presentation) já estava pronta ao retomar a
sessão — só faltavam migration e testes, ambos concluídos agora:

- **Migration**: `InitialCreate` regerada em
  `src/Modules/Identity/Identity.Infrastructure/Persistence/Migrations/` (a pasta estava vazia —
  sessão anterior deletou a antiga sem regerar). Comando usado:
  ```
  dotnet ef migrations add InitialCreate --project src/Modules/Identity/Identity.Infrastructure --startup-project src/Bootstrap/OdontoPlatform.Api --context IdentityDbContext -o Persistence/Migrations
  ```
  Schema confere com o contrato: `Users` sem `OrganizationId`/`Role`; `OrganizationMemberships`
  nova com índice único `(OrganizationId, UserId)` + índice de apoio em `UserId`; `RefreshTokens`
  com `OrganizationId` (não mais `TenantId` solto). Nunca aplicada em Postgres real (sem instância
  rodando no ambiente de dev) — sem risco de dado, mesma situação já documentada desde a task 002.
  Nenhum outro módulo referencia `OrganizationMembership`/`Users` na própria Persistence (grep
  confirmado) — não precisou regerar mais nenhuma migration.
- **Testes**: `OrganizationQueryFilterTests` reescrito — agora testa isolamento de
  `OrganizationMembership` (não mais `User`, que é global) e ganhou um 3º caso explícito
  provando que `User` NUNCA é filtrado por organization, mesmo com um organization arbitrário
  resolvido no contexto. Domain ganhou `OrganizationMembershipTests.cs` novo (zero cobertura
  antes) cobrindo `Create`/validações/`Desativar`.
- Nenhuma decisão técnica nova tomada nesta retomada — só completei o que a spec do Architect já
  tinha decidido. `dotnet build` da solução inteira (API + todos os módulos + testes): 0
  erro/0 aviso. `dotnet test`: 248/248 (0 falha) — ver detalhe em docs/tasks/014.
