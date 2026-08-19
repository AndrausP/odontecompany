---
task: "016"
sprint: "6"
status: done
---

# 016 — Convite de afiliação (modelo + criar/aceitar, sem envio de email)

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Owner convida um email pra sua organização com um papel; o convidado
aceita e passa a ter membership ativa naquela organização. Convite pra email **sem conta** fica
pendente e é aceitável assim que a conta for criada. Convite expirado ou já aceito não pode ser
usado de novo.

## Escopo técnico (Tech Lead)

**Escopo travado pelo PO: modelo + endpoints. NÃO tem envio de email nesta task.**

1. Entidade `Invite` (Identity.Domain): `OrganizationId`, `Email` (normalizado lowercase),
   `Role`, `TokenHash`, `Status` (Pendente/Aceito/Expirado/Revogado), `ExpiraEm`, `ConvidadoPor`
   (UserId), timestamps.
2. `POST /api/organizations/{id}/invites` — `Owner` ou `Admin` da org. Cria convite pendente.
   Email já com membership ativa na org → 409. Convite pendente duplicado pro mesmo email/org →
   reaproveita ou revoga o anterior (Architect decide).
3. `GET /api/organizations/{id}/invites` — lista convites da org (Owner/Admin).
4. `GET /api/me/invites` — convites **pendentes para o email do usuário logado**, em qualquer
   organização. É isso que faz o fluxo "convite pra quem não tinha conta" fechar: criou a conta
   com aquele email, o convite aparece.
5. `POST /api/invites/{token}/accept` — autenticado. Valida: token existe, status Pendente, não
   expirado, e o **email do convite bate com o email do usuário logado**. Cria
   `OrganizationMembership(Role do convite, Ativo)` e marca convite `Aceito`. Transação única.
6. `IInviteNotifier` — interface com implementação **no-op que só loga** (`ILogger`) o token
   gerado. Provider real de email é task futura, fora desta sprint.
7. Expiração: default 7 dias, configurável em `AuthOptions`. Convite vencido é `Expirado` na
   leitura (lazy) — **sem job de background nesta rodada**.

## Decisão do Architect — BLOQUEANTE RESOLVIDO

### 1. Token do convite: guardar HASH (padrão de RefreshToken)

**Decisão:** Guardar apenas o hash do token, nunca o valor em claro. Usar gerador/hasher idêntico ao de `RefreshToken` — `Sha256InviteTokenGenerator` (novo, mesmo padrão).

**Motivo:** Consistência; tokens são segredos e merecem proteção no banco.

**Contrato:**
```csharp
public class Invite : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; }  // Normalizado lowercase
    public Role Role { get; private set; }
    public string TokenHash { get; private set; }  // Nunca o valor em claro
    public InviteStatus Status { get; private set; }  // Pendente, Aceito, Expirado, Revogado
    public DateTime ExpiresAt { get; private set; }
    public Guid? InvitedByUserId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    
    public static Result<(Invite, string tokenValue)> Create(
        Guid organizationId,
        string email,
        Role role,
        Guid invitedByUserId,
        TimeSpan lifetime)
    {
        var tokenValue = Guid.NewGuid().ToString("N");  // 32 chars hex
        var tokenHash = Sha256InviteTokenGenerator.Hash(tokenValue);
        
        var invite = new Invite
        {
            OrganizationId = organizationId,
            Email = email.ToLowerInvariant(),
            Role = role,
            TokenHash = tokenHash,
            Status = InviteStatus.Pendente,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            InvitedByUserId = invitedByUserId
        };
        
        return Result.Success((invite, tokenValue));  // Retorna tupla: entidade + token legível
    }
    
    public void Accept() => Status = InviteStatus.Aceito;
    public void Revoke() => Status = InviteStatus.Revogado;
    public void MarkExpired() => Status = InviteStatus.Expirado;
}

public enum InviteStatus
{
    Pendente = 1,
    Aceito = 2,
    Expirado = 3,
    Revogado = 4
}

// Gerador de hash (cópia do padrão de RefreshToken)
public static class Sha256InviteTokenGenerator
{
    public static string Hash(string token)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
```

**Fluxo:**
- Owner convida email X → `Invite.Create()` retorna `(invite, tokenValue)`.
- Salvar `invite` (com tokenHash) no banco.
- Retornar token legível `tokenValue` na resposta (e em log se `IInviteNotifier` for no-op).
- Cliente (ou email futura) recebe `tokenValue`.
- Ao aceitar: `POST /api/invites/{tokenValue}/accept` → hashar o token recebido, buscar por hash, validar.

---

### 2. `Invite` é tenant-scoped com método explícito `GetPendingByEmailAcrossOrganizationsAsync`

**Decisão:** Sim, `Invite` implementa `IMustHaveOrganization` (cai no global query filter). MAS: criar método explícito `IInviteRepository.GetPendingByEmailAcrossOrganizationsAsync(email)` que **ignora o filtro de propósito**, com documentação inline.

**Motivo:** 
- `GET /api/me/invites` é um caso legítimo de leitura cross-org (usuário vê convites de orgs onde NÃO é membro — ainda).
- Nunca `IgnoreQueryFilters()` solto em LINQ ad-hoc — aumenta risco de vazamento silencioso.
- Método nomeado torna a intenção explícita: "este método deliberadamente bypassa isolamento, porque é correto neste contexto".
- Testável e auditável (procura por `GetPendingByEmailAcrossOrganizationsAsync` no codebase).

**Contrato:**
```csharp
public interface IInviteRepository
{
    Task CreateAsync(Invite invite);
    
    /// <summary>
    /// Busca convites PENDENTES para um email, em QUALQUER organização.
    /// 
    /// Deliberadamente ignora o global query filter porque o caso de uso é legítimo:
    /// usuário recém-criado (ou usuário existente) vê convites de organizações onde ainda não é membro.
    /// 
    /// Exemplo: Admin de Org A convida admin@example.com; admin@example.com cria conta com aquele email;
    /// chama GET /api/me/invites e vê que tem um convite pendente em Org A, embora não seja membro ainda.
    /// 
    /// REGRA: Usar APENAS em contexto de "ler convites pra o email do usuário logado";
    /// nunca em filtros ou queries genéricas que podem vazar dados.
    /// </summary>
    Task<List<Invite>> GetPendingByEmailAcrossOrganizationsAsync(string email);
    
    /// <summary>Busca convite por hash, ignorando filtro (necessário porque não sabemos org antes).</summary>
    Task<Invite?> GetByTokenHashAcrossOrganizationsAsync(string tokenHash);
    
    // Métodos normais (dentro do filtro de org)
    Task<List<Invite>> GetByOrganizationAsync(Guid organizationId);
    Task<Invite?> GetByIdAsync(Guid id);
}

public class InviteRepository : IInviteRepository
{
    private readonly IdentityDbContext _context;
    
    public async Task<List<Invite>> GetPendingByEmailAcrossOrganizationsAsync(string email)
    {
        // Ignora o global query filter deliberadamente
        return await _context.Invites
            .IgnoreQueryFilters()  // ← Explícito e documentado
            .Where(i => i.Email == email.ToLowerInvariant()
                && i.Status == InviteStatus.Pendente
                && !i.IsExpired)
            .ToListAsync();
    }
    
    public async Task<Invite?> GetByTokenHashAcrossOrganizationsAsync(string tokenHash)
    {
        return await _context.Invites
            .IgnoreQueryFilters()  // ← Explícito e documentado
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash && i.Status == InviteStatus.Pendente && !i.IsExpired);
    }
    
    public async Task<List<Invite>> GetByOrganizationAsync(Guid organizationId)
    {
        // Usa global filter — só vê convites da org escopada
        return await _context.Invites
            .Where(i => i.OrganizationId == organizationId)
            .ToListAsync();
    }
}
```

**Testes:**
```csharp
[Test]
public async Task GetPendingByEmailAcrossOrganizations_ReturnsInvitesFromMultipleOrgs()
{
    // Setup: Admin de org A convida admin@example.com
    //        Admin de org B também convida admin@example.com
    //        Criar invites pendentes em ambas
    
    var invites = await _inviteRepository.GetPendingByEmailAcrossOrganizationsAsync("admin@example.com");
    
    // Esperado: 2 invites, de orgs diferentes
    Assert.That(invites.Count, Is.EqualTo(2));
    Assert.That(invites.Select(i => i.OrganizationId).Distinct().Count(), Is.EqualTo(2));
}
```

---

### 3. Convite duplicado: REVOGAR anterior, criar novo

**Decisão:** Se já existe convite pendente pra (email, org), revogar o anterior e criar novo.

**Motivo:**
- Workflow simples para Owner (redigita email sem pensarA em duplicação, novo convite sobrescreve).
- Revogação é barata (uma flag).
- Alternativa (retornar existente) exigiria lógica de "se expirou, qual status?; se ainda pendente, qual timestamp mandar?".

**Contrato:**
```csharp
[HttpPost("organizations/{organizationId}/invites")]
[Authorize(Policy = "RequireActiveOrganization")]
public async Task<IActionResult> CreateInvite(
    Guid organizationId,
    [FromBody] CreateInviteRequest request)
{
    // Validações
    var membership = await _membershipRepository.GetByUserAndOrgAsync(currentUserId, organizationId);
    if (membership?.Role is not (Role.Owner or Role.Admin))
        return Forbid();  // Só Owner/Admin pode convidar
    
    // Verificar: email já é membro ativo
    var existingMembership = await _membershipRepository
        .GetByEmailAndOrgAsync(request.Email, organizationId);
    if (existingMembership?.Status == MembershipStatus.Ativo)
        return Conflict(new { error = "Email já é membro desta organização" });
    
    // Verificar: já existe convite pendente
    var existingInvite = await _inviteRepository
        .GetPendingByEmailAndOrgAsync(request.Email, organizationId);
    if (existingInvite != null)
    {
        existingInvite.Revoke();  // Revogar o anterior
    }
    
    // Criar novo convite
    var (newInvite, tokenValue) = Invite.Create(
        organizationId,
        request.Email,
        request.Role,
        currentUserId,
        TimeSpan.FromDays(7)
    ).Value;
    
    context.Invites.Add(newInvite);
    await context.SaveChangesAsync();
    
    // Notificar (no-op nesta sprint)
    await _inviteNotifier.SendInviteAsync(request.Email, tokenValue, organizationId);
    
    return CreatedAtAction(nameof(GetInvite), new { id = newInvite.Id }, newInvite);
}

public class CreateInviteRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    public Role Role { get; set; }  // Owner, Admin, Dentista, Recepcao
}
```

---

## Contrato arquitetural

| Artefato | Mudança |
|----------|---------|
| Entidade `Invite` (Identity.Domain) | OrganizationId, Email (lowercase), Role, TokenHash (nunca valor), Status enum, ExpiresAt, InvitedByUserId, timestamps |
| `IMustHaveOrganization` | `Invite` implementa (tenant-scoped) |
| `IInviteRepository` | Métodos: `GetPendingByEmailAcrossOrganizationsAsync` (ignora filter explicitamente), `GetByTokenHashAcrossOrganizationsAsync`, normal CRUD |
| Generator | `Sha256InviteTokenGenerator` (nova classe, padrão idêntico a RefreshToken) |
| `POST /api/organizations/{id}/invites` | Owner/Admin convida email; 409 se email já é membro; revoga convite anterior se existe |
| `GET /api/organizations/{id}/invites` | Lista convites da org (Owner/Admin) |
| `GET /api/me/invites` | Lista convites pendentes pra email do usuário (em qualquer org) |
| `POST /api/invites/{tokenHash}/accept` | Aceita convite, cria membership, marca Aceito |
| `IInviteNotifier` | Interface com impl no-op (log); email real é task futura |
| Migrations | Regenerar `InitialCreate` (nova tabela `invites`) |
| Testes | "Convite duplicado revoga anterior", "GET /api/me/invites vê convites de orgs não-member", "Accept convite cria membership" |

---

## Riscos residuais

- **Método `GetPendingByEmailAcrossOrganizationsAsync` é único ponto de bypass:** Qualquer uso futuro deve ser revisor em code review explicitamente. Documentação inline não é prova, mas aumenta visibilidade.
- **Token em claro no log do no-op `IInviteNotifier`:** Segurança relativa (ambiente de dev/test). Em produção, guardar token em claro é risco — sender (email provider) não deve nunca logar dados sensíveis. Débito nomeado.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Owner convida por email; convite pra email sem conta fica pendente; sem envio real de email nesta rodada. |
| 2. Contexto | Reader → Writer | Padrão de token com hash já existe (`RefreshToken` + `Sha256RefreshTokenGenerator`); leitura cross-tenant já existe (`GetByEmailAcrossTenantsAsync`). |
| 3. Quebra | Tech Lead | Esta task. |
| 4. Estrutura | Architect | [pendente — 3 itens acima, bloqueantes] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | Fechado. Bug latente (membership inativa não reativada no accept) registrado em `docs/knowledge/errors-aprendidos.md`; padrão IDOR de `{id}` de rota validado contra `organization_id` do token registrado em `docs/knowledge/patterns.md`; task de follow-up criada em `docs/tasks/020-membership-reativar-em-invite.md`. |

## Metadados

- **Estimativa:** 3 dias
- **Depende de:** 013, 014
- **Bloqueia:** 017 (parcial), 018
- **Risco:** ⚠️ item 2 é o risco real — furar o global query filter é exatamente o mecanismo que
  garante isolamento entre organizações. Tem que ser um método único, nomeado, testado, e não um
  `IgnoreQueryFilters()` espalhado.
- **Débito nomeado:** sem envio de email, o token só existe no log — inutilizável em produção.
  Task futura obrigatória antes de qualquer usuário real.

## Status

planned → in-progress → in-review → qa-approved-with-caveat → **done, com ressalva não-bloqueante** (Writer, 2026-08-18)

**Ressalva registrada, não escondida:** aprovada com o bug latente descrito abaixo em
"IMPORTANTE — bug latente em `AcceptInviteCommandHandler`" — inalcançável via API hoje (nenhum
endpoint desativa membership ainda), mas vira crítico no dia em que um endpoint de
remover/desativar membro entrar em produção. Fix não implementado nesta task — ver task de
follow-up `docs/tasks/020-membership-reativar-em-invite.md` (Writer, 2026-08-18), sprint em
aberto, com nota bloqueante explícita: não implementar endpoint de desativar/remover membro antes
dela.

### Validação QA (2026-08-18)

Todas as verificações críticas passaram. Um bug latente encontrado (não bloqueante hoje — vira crítico com a chegada de "desativar membership").

- **Invite.TokenHash:** `Invite.Create` só recebe `tokenHash` pré-computado (linha 50 do domain); token em claro é gerado pelo `IInviteTokenGenerator` na Application e nunca cruza a fronteira Domain. `CreateInviteCommandHandler` linha 63: `var (plainToken, tokenHash) = _inviteTokenGenerator.Generate();` — só o hash persiste. Plain volta na resposta HTTP + `IInviteNotifier` no-op (log — débito nomeado pra produção). Expiração lazy: `AcceptInviteCommandHandler` linha 50-56 chama `invite.MarkExpired()` + `SaveChangesAsync` ANTES de retornar `NaoEncontrado` — sem "Pendente zumbi" no banco. Coberto por `Should_ReturnNaoEncontrado_And_MarkExpired_When_InviteIsExpired`.
- **Convite duplicado revoga anterior:** `CreateInviteCommandHandler` linha 60-61 busca `existingInvite` via `GetPendingByEmailAndOrganizationAsync` e chama `.Revoke()` (rastreado pelo EF Core, comita no mesmo `SaveChangesAsync`). Teste `Should_RevokePreviousPendingInvite_When_DuplicateInviteForSameEmailAndOrganization` prova. Sem duplicar, sem erro.
- **GetPendingByEmailAcrossOrganizationsAsync:** método nomeado explícito no `IInviteRepository` (interface), XML doc inline detalha o motivo do bypass. `IgnoreQueryFilters()` só aparece nos DOIS métodos "AcrossOrganizations" do `InviteRepository` (linhas 24, 32) — cada um com comentário. `GetByOrganizationAsync` respeita o filtro global. Grep de `IgnoreQueryFilters` no módulo Identity: só ocorre nos métodos "Across" documentados (invites, memberships, refresh tokens — todos os pontos que precisam operar antes de saber a org, e nenhum além). Sem vazamento silencioso.
- **Accept invite validações:** hash → `Status == Pendente` → não expirado → `user.Ativo` → email do convite bate com email do usuário. TODAS as falhas devolvem `NaoEncontrado` genérico (anti-enumeração). 8 testes cobrem cada ramo (`Should_ReturnNaoEncontrado_When_TokenDoesNotExist`, `_When_InviteAlreadyAccepted`, `_When_InviteWasRevoked`, `_When_InviteIsExpired`, `_When_UserEmailDoesNotMatchInviteEmail`, `Should_ReturnUsuarioInativo_When_UserIsDeactivated`).

### 🟡 IMPORTANTE — bug latente em `AcceptInviteCommandHandler` (idempotência com membership INATIVA)

**Arquivo:** `src/Modules/Identity/Identity.Application/Commands/AcceptInvite/AcceptInviteCommandHandler.cs` linhas 69-76.

**Cenário:**
1. Usuário X tem `OrganizationMembership` INATIVA na org Y (`Status = Inativo`, `IsAtivo = false`).
2. Owner cria novo convite pra X na org Y — permitido, pois `CreateInviteCommandHandler` só bloqueia se `existingMembership.IsAtivo` (linha 56). Comportamento documentado no teste `Should_AllowInvite_When_InvitedEmailHasInactiveMembership`.
3. X aceita o convite: `AcceptInviteCommandHandler` acha `existingMembership is not null` (não filtra por IsAtivo!) e cai no branch de idempotência: marca invite Aceito, retorna `Success` com o `existingMembership.Role`.
4. **Silent failure:** membership continua `Inativo`. X recebe `Ok`, pensa que entrou, mas a próxima requisição autenticada dele nessa org falha (a query de eleição de org no login filtra por Ativo — `GetActiveMembershipsForUserAcrossOrganizationsAsync`).

**Por que não é CRÍTICO hoje:** grep confirma que NÃO existe caminho de API que dispare `OrganizationMembership.Desativar()` — o método existe no Domain mas nenhum handler o chama. Cenário só é atingível via seed/migration/DB direto. Fluxo do release fica intacto.

**Por que vira CRÍTICO amanhã:** o endpoint de "remover membro" é feature natural do módulo Identity (roadmap Fase 1). No dia em que entrar, esse silent fail vira bug de produção instantâneo — Owner desativa alguém e depois convida de volta, o cara "aceita" e nunca mais consegue entrar.

**Fix recomendado (fora do escopo desta task):** ou (a) filtrar `existingMembership.IsAtivo` no accept e criar nova membership se inativa (mas o índice único `(OrganizationId, UserId)` quebra — precisa de método `Reativar()` no Domain), ou (b) adicionar `Reativar()` ao Domain e chamar dentro do branch de idempotência quando `!existingMembership.IsAtivo`. (b) é mais consistente com o padrão de soft-delete já usado. Abrir task de follow-up antes de qualquer endpoint de deactivate-membership entrar em produção.

Zero bug alcançável via API surface atual. Aprovado com o caveat acima registrado.

### Implementado (Dev Backend)

- `Invite` (`Identity.Domain/Entities/Invite.cs`) + `InviteStatus` enum. `IMustHaveOrganization`
  → cai no filtro global automaticamente (`IdentityDbContext.ApplyOrganizationQueryFilters`
  varre por interface, não precisou registrar nada a mais).
- `IInviteRepository`/`InviteRepository`, `IInviteTokenGenerator`/`Sha256InviteTokenGenerator`,
  `IInviteNotifier`/`LoggingInviteNotifier` (no-op, só loga — `ManualConvenioAdapter` do módulo
  Billing foi o modelo de referência pro padrão "implementação de referência sem integração real").
- `POST /api/organizations/{id}/invites`, `GET /api/organizations/{id}/invites` —
  `OrganizationsController` (Owner/Admin, `RequireActiveOrganization`).
- `GET /api/me/invites`, `POST /api/invites/{token}/accept` — `InvitesController` NOVO
  (sem `RequireActiveOrganization` — tolera token sem org).
- Migration incremental `AddInvites` (`dotnet ef migrations add AddInvites --project
  src/Modules/Identity/Identity.Infrastructure --startup-project src/Bootstrap/OdontoPlatform.Api
  --context IdentityDbContext -o Persistence/Migrations`) — NÃO regerei `InitialCreate`, task
  pedia explicitamente pra preferir incremental já que já existe uma migration válida da sessão
  anterior. `dotnet ef database update` não rodado (sem Postgres real neste ambiente, mesma
  situação de sempre).

### Desvio #1 — IDOR fechado no controller: `{id}` da rota TEM que bater com `organization_id` do token

O contrato do Architect (`CreateInvite`) não menciona essa checagem — só valida role via
`[Authorize(Roles = "Owner,Admin")]`. Isso sozinho NÃO impede um Owner de organization A de
escrever um convite em organization B só trocando o guid `{id}` na URL (a policy de role checa
"é Owner/Admin *de alguma organization* — a do token", não "é Owner/Admin *desta* organization
específica da rota"). Adicionei `if (_currentUser.OrganizationId != id) return Forbid();` no
início de `CreateInvite`/`GetInvites` — mesma classe de bug (IDOR cross-tenant por id de rota não
validado contra o dono real) que já causei e corrigi numa sessão anterior (Harpia.WEB, ver
memória do agente) — documentado aqui pra não repetir a régua "achei sozinho, não foi o QA que
bloqueou desta vez".

### Desvio #2 — `IInviteTokenGenerator` é interface + DI, não a classe estática `Sha256InviteTokenGenerator`
do pseudocódigo do Architect

O contrato mostra uma classe `static` com `Hash(string)`. Troquei por uma interface
(`IInviteTokenGenerator`, mesmo shape de `IRefreshTokenGenerator`: `Generate()` + `Hash()`)
registrada como Singleton no DI. Motivo: consistência com o padrão JÁ estabelecido pra
`RefreshToken` (`IRefreshTokenGenerator`/`Sha256RefreshTokenGenerator`) — uma classe estática
seria intestável via mock nos handlers (`CreateInviteCommandHandlerTests`/
`AcceptInviteCommandHandlerTests` dependem de poder mockar o token gerado/hasheado). O algoritmo
em si é idêntico (SHA-256, token de 256 bits em Base64 URL-safe — troquei o `Guid.NewGuid().ToString("N")`
do pseudocódigo por bytes aleatórios de maior entropia, mesmo gerador de `RefreshToken`).

### Desvio #3 — checagem de "email já é membro" reusa `IUserRepository.GetByEmailAsync` +
`IOrganizationMembershipRepository.GetByUserAndOrganizationAcrossOrganizationsAsync`, sem método novo

O pseudocódigo assume um método `IOrganizationMembershipRepository.GetByEmailAndOrgAsync(email, orgId)`
que não existe (membership não guarda email, só `UserId`). Resolvido por composição dos dois
repositórios que já existiam — sem adicionar método novo à interface de membership.

### Aditivo — `IOrganizationRepository.GetByIdsAsync(IEnumerable<Guid>)`

Necessário pra resolver nome de organization em lote (convites cross-org e memberships da task
017 — ver `InvitePendingResolver`, `Identity.Application/Common/`). Só a implementação concreta
(`OrganizationRepository`) precisou de ajuste; mocks existentes não quebraram (Moq não exige
implementar 100% da interface).

### Expiração lazy — `Invite.MarkExpired()` É persistido no accept, não só ignorado em memória

Quando `AcceptInviteCommandHandler` encontra um convite Pendente mas com `ExpiresAt` no passado,
chama `invite.MarkExpired()` + `SaveChangesAsync` ANTES de devolver o erro genérico — deixa o
status do banco consistente com a realidade sem precisar de job de background (conforme pedido),
evitando um "Pendente zumbi" que nunca reflete a expiração.

### Testes novos (Identity.UnitTests)

`Domain/InviteTests.cs` (9), `Commands/CreateInviteCommandHandlerTests.cs` (5),
`Commands/AcceptInviteCommandHandlerTests.cs` (8), `Queries/GetMyInvitesQueryHandlerTests.cs` (2)
— o teste `Should_ReturnInvitesFromMultipleOrganizations...` é o teste de isolamento
cross-organization pedido explicitamente pelo handoff.

`dotnet build` solução inteira: 0 erro/0 aviso. `dotnet test` solução inteira: **281/281**
(baseline 248/248 + 33 testes novos entre 015/016/017 — Identity.UnitTests sozinho: 61→94).
Zero regressão em qualquer módulo fora de Identity.
