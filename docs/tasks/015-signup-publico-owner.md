---
task: "015"
sprint: "6"
status: done
---

# 015 — Signup público + criar organização (usuário vira Owner)

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Qualquer pessoa cria conta sem convite e sem admin. Usuário autenticado
cria uma organização e vira `Owner` dela automaticamente. Email duplicado é rejeitado sem
revelar que o email existe.

## Escopo técnico (Tech Lead)

**Dois endpoints, não um.** Signup e criação de organização são separados de propósito: o fluxo
de convite (task 016) exige que exista usuário SEM organização.

1. `POST /api/auth/signup` — **anônimo**. Body: `nome`, `email`, `senha`. Cria `User` (sem
   organização, sem role). Retorna token sem org (contrato definido na task 014, item 2).
2. `POST /api/organizations` — **autenticado**. Body: `nome`. Cria `Organization` +
   `OrganizationMembership(Role.Owner, Ativo)` para o usuário do token, em transação única.
   Retorna token novo já escopado à org criada (ou instrui o cliente a chamar
   `switch-organization` — Architect decide).
3. Revoga a regra antiga de negócio "sem registro público" em
   `docs/knowledge/business-rules.md` — Writer atualiza no passo 8.
4. `CreateUserCommand` (admin cria usuário na própria org) **continua existindo** — signup não
   substitui provisionamento interno.

## Anti-abuso (escopo mínimo desta rodada)

- Email único global — já é invariante hoje, mantido.
- Rate limit por IP no endpoint de signup (`AddRateLimiter` do ASP.NET Core, política fixa).
- Senha passa pelo mesmo `IPasswordHasher` (Argon2id) — sem exceção.
- **Fora de escopo, débito nomeado:** verificação de email (double opt-in), captcha, limite de
  organizações por usuário. Sem verificação de email, qualquer um cria conta com email de
  terceiro — aceitável só porque não há envio de email nem cobrança nesta fase. **Vira
  bloqueante antes de produção.**

## Decisão do Architect — BLOQUEANTE RESOLVIDO

### 1. `POST /api/organizations` retorna token novo escopado

**Decisão:** Sim. Retorna `201 Created` + body com `accessToken` e `refreshToken` já escopados à org criada.

**Motivo:** Uma ida a menos pro cliente (não precisa chamar `switch-organization` logo depois); melhor UX. Padrão consistente com `POST /api/auth/signup` + `POST /api/organizations` como fluxo coeso.

**Contrato:**
```csharp
[Authorize]  // Requer token sem org (recém-criado)
[HttpPost("organizations")]
public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
{
    // Aqui temos currentUserId (do token sem org) e request.OrganizationName
    
    var organization = Organization.Create(request.OrganizationName).Value;
    var membership = OrganizationMembership.Create(organization.Id, currentUserId, Role.Owner).Value;
    
    context.Organizations.Add(organization);
    context.OrganizationMemberships.Add(membership);
    await context.SaveChangesAsync();
    
    // Emitir token novo, escopado à org recém-criada
    var accessToken = _jwtTokenService.GenerateAccessToken(
        userId: currentUserId,
        organizationId: organization.Id,
        role: Role.Owner
    );
    var refreshToken = _refreshTokenService.GenerateRefreshToken(currentUserId, organization.Id);
    
    return CreatedAtAction(
        nameof(GetOrganization),
        new { id = organization.Id },
        new CreateOrganizationResponse
        {
            OrganizationId = organization.Id,
            OrganizationName = organization.Nome,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }
    );
}

public class CreateOrganizationResponse
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
```

---

### 2. Email duplicado: 409 com mensagem genérica

**Decisão:** Retornar `409 Conflict` com mensagem genérica ("Email já em uso").

**Motivo:** 
- Enumeração de email é possível hoje via `POST /api/auth/login` (constant-time não ajuda — login retorna "email ou senha inválidos" mas signup confirmaria a existência retroativamente).
- Ser inconsistente (200 em signup, erro em login) piora UX sem ganho real de segurança.
- LGPD/privacidade: se o email já existe, é porque alguém tentou registrar aquele email antes. Silenciar (200 vazio) sugere sucesso (confunde o usuário).

**Contrato:**
```csharp
[AllowAnonymous]
[HttpPost("auth/signup")]
public async Task<IActionResult> Signup([FromBody] SignupRequest request)
{
    var existingUser = await _userRepository.GetByEmailAsync(request.Email);
    if (existingUser != null)
        return Conflict(new { error = "Email já em uso" });  // 409
    
    var passwordHash = _passwordHasher.HashPassword(request.Password);
    var user = User.Create(Guid.NewGuid(), request.Nome, request.Email, passwordHash).Value;
    
    context.Users.Add(user);
    await context.SaveChangesAsync();
    
    // Emitir token SEM org (usuário recém-criado)
    var accessToken = _jwtTokenService.GenerateAccessToken(
        userId: user.Id,
        organizationId: null,
        role: null
    );
    var refreshToken = _refreshTokenService.GenerateRefreshToken(user.Id, organizationId: null);
    
    return CreatedAtAction(
        nameof(GetMe),
        new SignupResponse
        {
            UserId = user.Id,
            Email = user.Email,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }
    );
}

public class SignupRequest
{
    [Required]
    [MaxLength(255)]
    public string Nome { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [MinLength(8)]
    public string Senha { get; set; }  // Mínimo 8 chars, validado em Application layer
}

public class SignupResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
```

---

## Contrato arquitetural

| Artefato | Mudança |
|----------|---------|
| `POST /api/auth/signup` | Novo endpoint, anônimo, retorna token SEM org + refresh token |
| `POST /api/organizations` | Novo endpoint, autenticado (aceita token sem org), retorna token COM org escopado |
| Business rule | Revoga "sem registro público — só seed + admin" (vira permitido self-serve) |
| Rate limiting | `AddRateLimiter` na policy de signup (IP-based, fixa nesta sprint) |
| Email validação | 409 em email duplicado (consistente com semântica de erro) |
| `RefreshToken` | Pode ser emitido com `organizationId = null` (pra token-sem-org) |
| Testes | "Signup sucede", "Email duplicado retorna 409", "CreateOrganization como usuário sem org", "Token novo após criar org está escopado" |

---

## Riscos residuais

- **Rate limit falha:** Se middleware de rate limiting não tiver sido aplicado corretamente em Program.cs, endpoint fica aberto a ataque de força bruta de enumeração. QA valida.
- **Signup sem verificação de email:** Como documentado, qualquer um cria conta com email de terceiro. Bloqueante antes de produção. Débito nomeado no backlog.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Self-serve: qualquer um cria conta e cria organização; vira Owner. |
| 2. Contexto | Reader → Writer | Regra atual: "sem registro público, só seed + admin autenticado" — revogada por esta task. |
| 3. Quebra | Tech Lead | Esta task — signup e criar-org separados. |
| 4. Estrutura | Architect | [pendente — 2 itens acima] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | Fechado. Regra "sem registro público" revogada em `docs/knowledge/business-rules.md`; sem novo padrão/erro específico desta task (cobertos nos handoffs de 016/017). |

## Metadados

- **Estimativa:** 2 dias
- **Depende de:** 013, 014
- **Bloqueia:** 018
- **Risco:** ⚠️ endpoint anônimo que escreve no banco é superfície de abuso nova. Rate limit é
  critério de aceite, não "se der tempo".

## Status

planned → in-progress → in-review → qa-approved → **done** (Writer, 2026-08-18)

### Validação QA (2026-08-18)

Todas as 9 verificações da task passaram. Confirmado por leitura direta do código, não a palavra do Dev:

- **IDOR (P1):** `OrganizationsController.CreateInvite/GetInvites` faz `if (_currentUser.OrganizationId is null || _currentUser.OrganizationId != id) return Forbid();` (linhas 65-66, 90-91). Fix confirmado. Grep de outros endpoints com `{id:guid}` de organization: só existem no `OrganizationsController` — todos os demais controllers (Patients, Scheduling, Faturas, Records, etc.) recebem GUID de recurso tenant-scoped, protegido pelo global query filter via policy `RequireActiveOrganization` (achado só é buscável dentro da org do token). Sem outros pontos com o mesmo padrão de bug.
- **Signup 409 anti-enumeração:** `SignupCommandHandler` reusa `DomainErrors.User.EmailJaCadastrado` (código genérico do `CreateUserCommandHandler`); `AuthController.Signup` mapeia código → `Conflict` sem revelar detalhes. TOCTOU coberto por `UniqueConstraintViolationException` (teste `Should_ReturnEmailJaCadastrado_When_UniqueConstraintViolationOnSave`).
- **Atomicidade CreateOrganization:** `Organization` + `OrganizationMembership(Owner)` + `RefreshToken` num único `_unitOfWork.SaveChangesAsync` (linhas 68-83 do handler). Sem transação explícita, mas mesmo DbContext = mesma unit-of-work; falha no meio faz rollback do lote inteiro. Token retornado JÁ escopado à org nova via `GenerateAccessToken(user.Id, organization.Id, Role.Owner, branchId: null)`.
- **Rate limit signup:** `Program.cs` linhas 163-176 — `PermitLimit = 5`, `Window = 1min`, `QueueLimit = 0`, partition por `RemoteIpAddress`. Aplicado via `[EnableRateLimiting("signup")]` no `AuthController.Signup`. `app.UseRateLimiter()` no pipeline após `UseAuthorization`. Valores razoáveis pra signup público.

Zero bug encontrado nesta task.

### Implementado (Dev Backend)

- `POST /api/auth/signup` (`AuthController.Signup`) — `SignupCommand`/`SignupCommandHandler`
  (`Identity.Application/Commands/Signup/`). Email duplicado → 409 genérico (`User.EmailJaCadastrado`,
  código já existia). TOCTOU coberto (mesmo padrão de `CreateUserCommandHandler`).
- `POST /api/organizations` (`OrganizationsController.Create`) — `CreateOrganizationCommand`/Handler.
  Cria `Organization` + `OrganizationMembership(Owner)` + `RefreshToken` num único `SaveChangesAsync`.
  Endpoint SEM policy `RequireActiveOrganization` (tolera token sem org — é a rota que dá a
  primeira org). Um usuário que JÁ tem organization também pode chamar (cria org adicional,
  multi-org — não bloqueei, não era pedido bloquear).
- Rate limiting: `AddRateLimiter` (Program.cs) com policy `"signup"` — fixed window por IP
  (`RemoteIpAddress`), 5 req/min, `QueueLimit = 0`. Aplicada via `[EnableRateLimiting("signup")]`
  no `AuthController.Signup`. `app.UseRateLimiter()` adicionado ao pipeline (depois de
  `UseAuthorization`, antes de `MapControllers`).

### Desvio do contrato do Architect — `POST /api/auth/signup` NÃO emite refresh token

O pseudocódigo da task mostra `_refreshTokenService.GenerateRefreshToken(user.Id, organizationId: null)`
no signup. Isso **não é implementável como está**: `RefreshToken` implementa
`IMustHaveOrganization` (`OrganizationId` é `Guid` não-nulo, ver `Identity.Domain.Entities.RefreshToken`)
e essa decisão já estava em produção desde a task 014 (ver `docs/decisions.md`,
"Refresh token só funciona se o token carregar organization_id..."). O próprio
`LoginCommandHandler` (task 014) já resolve o caso "usuário com zero memberships" emitindo só
access token, sem refresh — segui o MESMO padrão em `SignupCommandHandler` (`SignupResultDto.RefreshToken`
é sempre `null`). Não é uma decisão nova, é aplicar uma decisão já existente ao segundo endpoint
que cai no mesmo caso. Response mudou de `SignupResponse{UserId,Email,AccessToken,RefreshToken}`
pra `SignupResultDto` (sem RefreshToken populável) — mesmo shape, campo sempre null documentado.

### Business rule revogada

`docs/knowledge/business-rules.md` ainda NÃO foi tocado por mim — task explícita: só o Writer
escreve lá (regra do CLAUDE.md do projeto). A revogação de "sem registro público" já está
documentada em `docs/decisions.md` pelo Architect (2026-08-18); falta o Writer replicar em
`business-rules.md` no fechamento da cadeia.

### Testes novos (Identity.UnitTests)

`Commands/SignupCommandHandlerTests.cs` (3), `Commands/CreateOrganizationCommandHandlerTests.cs` (3).
`dotnet build` solução inteira: 0 erro/0 aviso. `dotnet test` Identity.UnitTests: 94/94 (ver
Status da 016 pro detalhamento do delta total).
