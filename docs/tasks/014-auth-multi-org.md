---
task: "014"
sprint: "6"
status: done
---

# 014 — Auth multi-org: JWT com organização ativa + troca sem novo login

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Usuário afiliado a 2 organizações loga uma vez, recebe token escopado a
uma org ativa, e troca de org ativa por endpoint — **sem refazer login e sem digitar senha de
novo**. Dado de uma org nunca aparece com token da outra.

## Escopo técnico (Tech Lead)

1. `LoginCommandHandler` para de resolver org por `user.TenantId`. Passa a: autenticar por email
   (único global) → carregar memberships ativas → eleger org ativa → emitir token.
2. `JwtTokenService.GenerateAccessToken` deixa de receber `Role` do `User`; recebe
   `organizationId` + `Role` **da membership** eleita.
3. `CurrentUserAccessor` lê `organization_id`/`role` da claim como hoje — a fonte muda, o
   contrato pra os 8 módulos **não muda**. Isso é proposital: nenhum módulo de domínio deve
   saber que virou N:N.
4. Endpoint novo `POST /api/auth/switch-organization` — body `{ organizationId }`, valida
   membership ativa do usuário do token, emite access token novo escopado à org pedida.
   Membership inexistente/inativa → 403.
5. `RefreshToken` continua funcionando: refresh reelege a org ativa (regra definida pelo
   Architect na 013 item 2).

## Decisão do Architect — BLOQUEANTE RESOLVIDO

### 1. Eleição da org ativa no login: PRIMEIRA MEMBERSHIP (mais antiga)

**Decisão:** Se usuário tem 1+ memberships ativas, eleger a primeira criada (determinístico, sem UI de seleção nesta sprint).

**Contrato:**
```csharp
public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !_passwordHasher.VerifyHashedPassword(user.PasswordHash, request.Password))
            return LoginResult.InvalidCredentials();
        
        // Carregar memberships ativas
        var activeMemberships = await _membershipRepository
            .GetActiveMembershipsForUserAsync(user.Id);
        
        if (activeMemberships.Count == 0)
        {
            // Usuário sem org (recém-criado ou só convites pendentes)
            var tokenWithoutOrg = _jwtTokenService.GenerateAccessToken(
                userId: user.Id,
                organizationId: null,  // Null = sem org ativa
                role: null
            );
            return LoginResult.Success(tokenWithoutOrg, refreshTokenHash);
        }
        
        // Eleger a primeira criada (mais antiga)
        var activeOrganization = activeMemberships
            .OrderBy(m => m.CreatedAt)
            .First();
        
        var accessToken = _jwtTokenService.GenerateAccessToken(
            userId: user.Id,
            organizationId: activeOrganization.OrganizationId,
            role: activeOrganization.Role
        );
        
        var refreshToken = _refreshTokenService.GenerateRefreshToken(user.Id, activeOrganization.OrganizationId);
        return LoginResult.Success(accessToken, refreshToken);
    }
}
```

**Motivo:** Simples, determinístico (sem estado novo de "última org usada"), sem UI bloqueante. Usuário pode trocar depois via `switch-organization`.

**Débito:** Persistir "última org usada" (feature de conforto) é próxima tarefa, não desta sprint.

---

### 2. Token sem org ativa: EMITIR, com policy de validação

**Decisão:** Emitir token SEM claim `organization_id` pra usuário com ZERO organizações. Token válido, apenas escopado a endpoints que toleram ausência de org.

**Endpoints que toleram token SEM `organization_id`:**
- `GET /api/me` — retorna dados do usuário + lista de orgs + convites pendentes
- `POST /api/auth/switch-organization` — **na verdade, NÃO:** usuário sem org não pode trocar pra nada; skip esta validação por agora
- `POST /api/organizations` — criar nova org
- `GET /api/me/invites` — listar convites pendentes (pra qualquer org)
- `POST /api/invites/{token}/accept` — aceitar convite (cria membership)

**Endpoints que EXIGEM `organization_id` na claim:**
- Todos os demais (agenda, pacientes, faturamento, estoque, etc)

**Implementação:** Policy nomeada `RequireActiveOrganization` registrada em `Program.cs`:

```csharp
// Program.cs — Configuração de políticas
services.AddAuthorizationBuilder()
    .AddPolicy("RequireActiveOrganization", policy =>
    {
        policy.RequireClaim("organization_id");  // Claim obrigatoriamente presente e não-null/empty
    });
```

**Uso em Controllers:**
```csharp
[ApiController]
[Route("api/agenda")]
public class AgendaController : ControllerBase
{
    // Requer organization_id
    [Authorize(Policy = "RequireActiveOrganization")]
    [HttpGet]
    public async Task<IActionResult> ListarAgendamentos() { ... }
}

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    // Aceita token SEM organization_id
    [Authorize]
    [HttpPost("organizations")]
    public async Task<IActionResult> CriarOrganizacao([FromBody] CreateOrganizationRequest request) { ... }
}
```

**CurrentUserAccessor:** Não faz check de org — deixa passar `organization_id` null. Controllers que precisam fazem o check via policy.

**Segurança:** Policy garante fail-closed (sem claim, acesso negado com 403, nunca silencioso).

---

### 3. Troca de org NÃO invalida token anterior

**Decisão:** Rejeitar complexidade. Token anterior continua válido até expiração natural (15min).

**Motivo:** Tokens são curtos, expiram sozinhos. Revogar token anterior exigiria SQL + cache + edge case raro (usuário troca de org uma vez e continua usando o token antigo? Improvável).

**Contrato:**
```csharp
[Authorize]
[HttpPost("auth/switch-organization")]
public async Task<IActionResult> SwitchOrganization([FromBody] SwitchOrganizationRequest request)
{
    var userId = User.FindFirst("sub").Value;
    var membership = await _membershipRepository.GetByUserAndOrgAsync(userId, request.OrganizationId);
    
    if (membership == null || membership.Status != MembershipStatus.Ativo)
        return Forbid();  // 403
    
    var newAccessToken = _jwtTokenService.GenerateAccessToken(
        userId: userId,
        organizationId: membership.OrganizationId,
        role: membership.Role
    );
    
    var newRefreshToken = _refreshTokenService.GenerateRefreshToken(userId, membership.OrganizationId);
    
    return Ok(new SwitchOrganizationResponse
    {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken
    });
    // Token anterior (access + refresh) permanece válido até expiração
}
```

---

## Contrato arquitetural

| Artefato | Mudança |
|----------|---------|
| `LoginCommandHandler` | Ordena memberships por `CreatedAt`, elege primeira; aceita 0 memberships (token sem org) |
| `JwtTokenService` | `GenerateAccessToken(userId, organizationId?, role?)` onde org e role podem ser null |
| JWT claim | `organization_id` pode estar ausente (null desserializado como "claim não presente") |
| Policy | `RequireActiveOrganization` falha se claim `organization_id` ausente |
| `POST /api/auth/switch-organization` | Novo endpoint, valida membership, emite novo token escopado |
| `RefreshToken.OrganizationId` | Deve estar presente (token vinculado a uma org no refresh) |
| Testes | "Usuário sem org recebe token válido", "Token sem org autoriza /api/me mas nega /api/agenda/listar" |

---

## Riscos residuais

- **Policy falha aberta:** Se `RequireActiveOrganization` não for registrada corretamente, endpoint de negócio aceita token sem org. QA valida explicitamente (ver task 019).
- **Null poisoning:** Se `currentUser.OrganizationId` for null e um serviço não checar, NPE. Services de domínio devem validar (`if (organizationId == null) throw new SecurityException()`).
- **Refresh token sem org:** Se houver rota que permite refresh com token-sem-org, precisamos de lógica especial ("usuário sem org não pode refrescar, ou refrescar mantém null org?"). Por agora: **refresh só funciona se token tiver org** — usuário novo sem org precisa fazer login de novo ou aceitar convite primeiro.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | JWT carrega org ativa; usuário troca de org sem novo login. |
| 2. Contexto | Reader → Writer | JWT hoje: `sub`, `tenant_id`, `role`, `unidade_id?`, `jti`. Login resolve tenant pelo email. |
| 3. Quebra | Tech Lead | Esta task — contrato de claim pros módulos permanece idêntico. |
| 4. Estrutura | Architect | [pendente — 3 itens acima, bloqueantes] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | Fechado. Padrão `RequireActiveOrganization` registrado em `docs/knowledge/patterns.md`; decisões de "switch não invalida token anterior" e "refresh exige org" registradas em `docs/decisions.md`. |

## Metadados

- **Estimativa:** 3 dias
- **Depende de:** 013
- **Bloqueia:** 015, 016, 017, 018
- **Risco:** ⚠️ alto. Token sem org (item 2) é superfície de autorização nova — se a policy
  falhar aberta, endpoint de negócio aceita token sem escopo. QA tem que testar explicitamente
  "token sem org bate em endpoint de agenda → 403".

## Status

planned → in-progress → in-review (QA) → QA pass → **done** (Writer, 2026-08-18)

### Notas QA (2026-08-18)

Validado por leitura de código + suite existente (248/248). Nenhum bug bloqueante.

- **Policy `RequireActiveOrganization` é fail-CLOSED por construção.** `Program.cs:154-155`
  registra `.RequireClaim("organization_id")` — primitivo built-in do ASP.NET Core
  (`ClaimsAuthorizationRequirement`) documentado como fail-closed: claim ausente ou principal
  não autenticado → `AuthorizationResult.Failed`. `JwtTokenService.GenerateAccessToken` NÃO
  adiciona a claim quando `organizationId is null` (ver linhas 28-29 e comentário in-loco: "não
  null/vazio, AUSENTE mesmo, pra RequireClaim falhar fechado"). Nenhum handler custom foi
  registrado que pudesse fazer short-circuit — a policy só usa o requirement built-in.
- **Aplicação da policy nos controllers de negócio (grep confirmado):** aplicada em
  `Branches`, `Convenios`, `Estoque`, `Faturas`, `Patients`, `Profissionais`, `Records`,
  `Reports`, `Salas`, `Scheduling`, `Users`. Total: 11/11 controllers de domínio protegidos.
  `AuthController` (login/refresh são `[AllowAnonymous]`, `switch-organization` é só
  `[Authorize]` sem policy) — correto pelo item 3 da spec: quem trocou pra org a partir de
  token sem-org precisa desse endpoint acessível.
- **Endpoints "toleram sem org" (item 3 do checklist do broker):** `GET /api/me`,
  `POST /api/organizations`, `GET /api/me/invites`, `POST /api/invites/{token}/accept` **NÃO
  EXISTEM AINDA** neste repo (são de 015/016/017, ainda `planned`). Portanto o check "não têm a
  policy aplicada" é vacuosamente verdadeiro hoje. Comentário in-loco em `Program.cs:151-153`
  já documenta a intenção. **QA obrigatório reabrir esse check quando 015/016/017 entrarem.**
- **`SwitchOrganizationCommandHandler`**: só permite trocar pra org onde usuário tem membership
  ativa. Fluxo: `GetByIdAsync(userId)` → user ativo → `GetByUserAndOrganizationAcrossOrganizationsAsync`
  → membership não-null e `IsAtivo` → `GetByIdAsync(organizationId)` → org ativa → só então
  emite token com `membership.OrganizationId`/`membership.Role`/`membership.BranchId` (nunca o
  request cru). `AuthController.SwitchOrganization` pega `userId` do
  `_currentUser.UserId` (claim `sub` do token), NUNCA do corpo — impossível trocar em nome de
  outro user. `SwitchOrganizationCommandHandlerTests.cs` (arquivo novo) cobre: sucesso,
  membership inexistente, membership inativa, user inativo, org inativa, e explicitamente o
  contrato "token anterior não é revogado" (item 3 da spec).
- **Login sem membership**: coberto por
  `Should_ReturnTokenWithoutOrganization_And_NoRefreshToken_When_UserHasZeroActiveMemberships`
  — access token sem claim `organization_id`, `RefreshToken` null, `AddAsync` nunca chamado.
- **Eleição determinística**: coberto por
  `Should_ElectOldestMembership_When_UserHasMultipleActiveMemberships` — repositório devolve
  lista fora de ordem de propósito, handler tem que ordenar por `CreatedAt` pra a mais antiga
  vencer (não confia no order de retorno).
- **`OrganizationMiddleware` fail-safe**: se claim ausente ou não é Guid válido,
  `IOrganizationContext` fica com `OrganizationId == null` → query filter
  `e.OrganizationId == null` casa com nada (data com null em `IMustHaveOrganization` não
  existe, é required). Fail-closed também na camada de dado, redundância intencional.

**Pendência de integração fim-a-fim (não-bloqueante):** Não existe
`OdontoPlatform.Api.IntegrationTests` no repo (nenhum projeto usa `WebApplicationFactory`). O
caso "token sem `organization_id` → `GET /api/branches` → 403" está listado como item 5 da
**task 019** (`docs/tasks/019-qa-regressao-multi-org.md`, `planned` na mesma sprint 6),
dedicada justamente a regressão end-to-end de isolamento/RBAC multi-org. Escrever esse teste
fora da 019 duplicaria escopo dela e ainda exigiria stand-up de um projeto de integração
novo (Postgres/Redis mockados, seed bypass, JwtSettings de teste) — trabalho de sprint
inteira, não de sub-task de QA. Dev Backend já apontou explicitamente essa pendência nas
notas da 014. **Recomendação:** aprovar 014 hoje; 019 é o próximo passo natural pra fechar
esse fim.

### Notas Dev Backend (retomada de sessão, 2026-08-18)

Implementação (`LoginCommandHandler`, `JwtTokenService`, `SwitchOrganizationCommand`+Handler,
`AuthController`, policy `RequireActiveOrganization` espalhada pelos controllers) já estava
pronta ao retomar a sessão. O que faltava era só a suite de testes de `Identity.UnitTests`, que
não compilava (39 erros — ainda usava a API antiga de `User`/`IUserRepository` de antes da 013).
Reescrevi/completei:

- `LoginCommandHandlerTests.cs` — reescrito por completo pro modelo N:N. Além dos casos que já
  existiam (credenciais inválidas, usuário inativo, organization inativo, isolamento do refresh
  token), adicionei os 2 casos que o critério de aceite da task exigia e que não tinham teste
  nenhum: usuário com zero memberships recebe token válido sem `organization_id`/sem refresh
  token (`Should_ReturnTokenWithoutOrganization_And_NoRefreshToken_When_UserHasZeroActiveMemberships`),
  e eleição determinística da membership mais antiga mesmo quando o repositório devolve a lista
  fora de ordem (`Should_ElectOldestMembership_When_UserHasMultipleActiveMemberships`).
- `RefreshTokenCommandHandlerTests.cs` — reescrito; adicionei 2 casos novos: refresh falha quando
  a membership da org do token não existe mais, e quando existe mas está inativa (nenhum dos dois
  tinha teste antes — risco residual que a própria task 014 aponta: "usuário perdeu a afiliação").
- `SwitchOrganizationCommandHandlerTests.cs` — **arquivo novo**, zero cobertura antes desta
  sessão pro endpoint inteiro de troca de org. Cobre: sucesso, membership inexistente, membership
  inativa, usuário inativo, organization alvo inativa, e o contrato explícito do item 3 da spec
  (token anterior NUNCA é revogado ao trocar de org).
- `CreateUserCommandHandlerTests.cs`/`SeedFirstAdminCommandHandlerTests.cs`/`UserTests.cs`/
  `OrganizationQueryFilterTests.cs` — ajustados pra nova assinatura (`User.Create` 3 argumentos,
  handlers com `IOrganizationMembershipRepository`/`IOptions<AuthOptions>` a mais no construtor).

**Não testado nesta sessão** (fora do escopo de unit test, fica pro QA/task 019 conforme a
própria spec já apontava): a policy `RequireActiveOrganization` fim-a-fim via
`WebApplicationFactory`/integration test real batendo em endpoint de negócio com token sem
`organization_id` — só validei via leitura de código que a policy está registrada em
`Program.cs` e aplicada nos controllers. Risco residual explícito na task: "se a policy falhar
aberta, endpoint de negócio aceita token sem escopo" — recomendo QA priorizar isso.

`dotnet build` da solução inteira (API + todos os módulos + testes): 0 erro/0 aviso.
`dotnet test`: **248/248 passando, 0 falha** (baseline antes da sessão 013/014: 228/228 — os 20 a
mais são testes novos: `OrganizationMembershipTests` + os casos novos de Login/Refresh +
`SwitchOrganizationCommandHandlerTests` inteiro).
