---
task: "017"
sprint: "6"
status: done
---

# 017 — `GET /api/me`: perfil escopado por membership

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Uma chamada devolve tudo que o frontend precisa pra montar cabeçalho e
seletor de organização: dados do usuário, organizações a que ele pertence (com papel em cada),
qual é a ativa no token atual, e convites pendentes. Organizações a que o usuário **não**
pertence nunca aparecem.

## Escopo técnico (Tech Lead)

`GET /api/me` — autenticado, **aceita token sem org ativa** (usuário recém-criado).

Payload:
- `user`: id, nome, email, ativo.
- `organizations[]`: id, nome, role (do membership), status, `branchId?`.
- `activeOrganizationId`: da claim do token; `null` se o token não tiver org.
- `pendingInvites[]`: id, organizationName, role, expiraEm (vem da task 016).

**Fora de escopo (decisão do PO):** demais campos de perfil (telefone, avatar, preferências,
troca de senha). Fica pra depois. Não inventar campo.

## Decisão do Architect — BLOQUEANTE RESOLVIDO

### `pendingInvites` embutido em `GET /api/me`

**Decisão:** EMBUTIR `pendingInvites[]` no payload de `/api/me`, além de manter endpoint separado `GET /api/me/invites`.

**Motivo:**
- Frontend no boot da app faz uma chamada: `GET /api/me` → recebe tudo que precisa (estado do usuário, orgs, convites).
- Uma ida é melhor que duas (reduz latência, simplifica fluxo de init).
- Endpoint separado `GET /api/me/invites` continua existindo: pra refresh da lista (sem trazer todo /me), ou pra uso em outros contextos.
- Padrão com precedente na plataforma: `/api/me` como "snapshot do estado global de um usuário autenticado".

**Contrato:**
```csharp
[HttpGet("me")]
[Authorize]  // Aceita token COM org ou SEM org
public async Task<IActionResult> GetMe()
{
    var userId = User.FindFirst("sub").Value;
    var organizationId = User.FindFirst("organization_id")?.Value;  // Pode ser null
    
    var user = await _userRepository.GetByIdAsync(userId);
    var memberships = await _membershipRepository.GetByUserAsync(userId);
    var pendingInvites = await _inviteRepository.GetPendingByEmailAcrossOrganizationsAsync(user.Email);
    
    return Ok(new MeResponse
    {
        User = new UserDto
        {
            Id = user.Id,
            Nome = user.Nome,
            Email = user.Email,
            Ativo = user.Ativo
        },
        
        Organizations = memberships.Select(m => new OrganizationMembershipDto
        {
            OrganizationId = m.OrganizationId,
            OrganizationName = m.Organization.Nome,
            Role = m.Role.ToString(),
            Status = m.Status.ToString(),
            BranchId = m.BranchId
        }).ToList(),
        
        ActiveOrganizationId = organizationId,  // Null se token sem org
        
        PendingInvites = pendingInvites.Select(i => new PendingInviteDto
        {
            InviteId = i.Id,
            OrganizationId = i.OrganizationId,
            OrganizationName = i.Organization.Nome,
            Role = i.Role.ToString(),
            ExpiresAt = i.ExpiresAt
        }).ToList()
    });
}

public class MeResponse
{
    public UserDto User { get; set; }
    public List<OrganizationMembershipDto> Organizations { get; set; }
    public string? ActiveOrganizationId { get; set; }  // Null se token sem org
    public List<PendingInviteDto> PendingInvites { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public bool Ativo { get; set; }
}

public class OrganizationMembershipDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public string Role { get; set; }  // "Owner", "Admin", "Dentista", "Recepcao"
    public string Status { get; set; }  // "Ativo", "Inativo"
    public Guid? BranchId { get; set; }  // Opcional
}

public class PendingInviteDto
{
    public Guid InviteId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public string Role { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

**Frontend (esboço):**
```typescript
// App boot
const { data: me } = useSuspenseQuery({
  queryKey: ['me'],
  queryFn: async () => (await api.get('/api/me')).data
});

// Acesso direto
<div>Olá, {me.user.nome}</div>
<OrgSwitcher organizations={me.organizations} activeOrgId={me.activeOrganizationId} />
{me.pendingInvites.length > 0 && <InviteNotifications invites={me.pendingInvites} />}
```

**Refresh de invites:** Frontend pode chamar `GET /api/me/invites` sem refazer `/me` inteiro (otimização).

---

## Contrato arquitetural

| Artefato | Mudança |
|----------|---------|
| `GET /api/me` | Retorna `MeResponse` com user, organizations[], activeOrganizationId, pendingInvites[] |
| Autorização | Aceita token COM org ou SEM org (não usa policy) |
| Dados | User global, Organizations tenant-scoped, PendingInvites cross-org (via `GetPendingByEmailAcrossOrganizationsAsync`) |
| `GET /api/me/invites` | Mantido (retorna só invites, pra refresh rápido) |
| Testes | "Usuário sem org recebe /me com PendingInvites vazio ou com convites", "Organizações retornadas são só aquelas onde o usuário é membro", "ActiveOrganizationId é null se token sem org" |

---

## Riscos residuais

- **Payload grande:** Se usuário tem 50 orgs + 20 convites, resposta é pesada. Otimização (paginação de orgs, lazy-load de convites) é feature futura, não desta sprint.
- **Vazamento de dados de convites:** `/api/me/invites` deve usar `GetPendingByEmailAcrossOrganizationsAsync` (explícito), não gerador de query genérico.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Perfil mostra só dados da(s) organização(ões) a que o usuário está afiliado. |
| 2. Contexto | Reader → Writer | Não existe `/api/me` hoje — frontend monta estado só decodificando o JWT (`lib/auth-store.ts`). |
| 3. Quebra | Tech Lead | Esta task. |
| 4. Estrutura | Architect | [pendente — 1 item] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | Fechado. Zero bug/padrão novo desta task (reusa `GetActiveMembershipsForUserAcrossOrganizationsAsync` e `GetPendingByEmailAcrossOrganizationsAsync`, já documentados nas tasks 013/016). |

## Metadados

- **Estimativa:** 1 dia
- **Depende de:** 013, 014, 016 (pra `pendingInvites`; sem a 016, entrega sem esse campo)
- **Bloqueia:** 018
- **Risco:** baixo. Único cuidado: não vazar organização em que o usuário não tem membership.

## Status

planned → in-progress → in-review → qa-approved → **done** (Writer, 2026-08-18)

### Validação QA (2026-08-18)

- **`GET /api/me` tolera token SEM org:** `MeController` só usa `[Authorize]` (sem `RequireActiveOrganization`); handler aceita `ActiveOrganizationId = null` e devolve `Organizations = []` sem quebrar. Teste `Should_ReturnNullActiveOrganizationId_When_TokenHasNoOrganization` cobre.
- **Isolamento de organizations:** a fonte é SEMPRE `GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id)` (linha 43 do handler) — nunca uma listagem geral de organizations; org a que o usuário não pertence não entra estruturalmente no pipeline (não é só "não deveria aparecer", o dado nem chega). Teste `Should_ReturnOnlyOrganizationsFromUserMemberships_When_UserBelongsToMultipleOrganizations` valida.
- **pendingInvites correto:** reusa `InvitePendingResolver` (mesma composição de `GetMyInvitesQueryHandler`), que por sua vez usa `GetPendingByEmailAcrossOrganizationsAsync` — o método NOMEADO/documentado da task 016. Sem vazamento cross-user (filtro é sempre `user.Email`, vindo do token; hash não bate pra outro usuário). Teste `Should_EmbedPendingInvites_When_UserHasPendingInvites` cobre.
- **ActiveOrganizationId vem do TOKEN:** `MeController` passa `_currentUser.OrganizationId` pro `GetMeQuery` — nunca resolvido/persistido no banco, é estado do JWT.

Zero bug encontrado nesta task.

### Implementado (Dev Backend)

- `GET /api/me` — `MeController` NOVO (rota `api/me`, `[Authorize]` sem `RequireActiveOrganization`).
  `GetMeQuery`/`GetMeQueryHandler` (`Identity.Application/Queries/GetMe/`). Retorna `MeResultDto`
  (User + Organizations[] + ActiveOrganizationId? + PendingInvites[]).
- `Organizations[]` vem SEMPRE de `GetActiveMembershipsForUserAcrossOrganizationsAsync` (mesmo
  método já usado no login/task 014 pra eleger a org) — nunca uma listagem geral de organizations,
  então organization a que o usuário não pertence estruturalmente não tem como aparecer (não é só
  "não deveria", o dado nem entra no pipeline).
- `ActiveOrganizationId` vem da claim do TOKEN atual (`ICurrentUserAccessor.OrganizationId`,
  passado pro `GetMeQuery` pelo controller) — nunca resolvido/persistido no banco, é estado do
  token, não preferência.
- `PendingInvites[]` reusa a MESMA composição de `GetMyInvitesQueryHandler` (task 016) via
  `InvitePendingResolver` (`Identity.Application/Common/`) — não duplica a lógica de "resolver
  nome de organization em lote pra convite pendente" entre os dois handlers.
- `GET /api/me/invites` (endpoint separado, já existia desde a 016) continua funcionando igual —
  não foi alterado por esta task.

### Sem desvio de contrato relevante

O único ponto pendente do Architect ("nomear query params de `Organizations[].BranchId`") já
estava resolvido no contrato — implementei como veio. Os nomes de propriedade seguem o padrão em
inglês do restante da task (`OrganizationMembershipDto`), sem repetir o padrão em português usado
em outros módulos (ex: `DreFiscalizadorDetalheDTO`) — este é o padrão local do módulo Identity
desde a task 013/014 (`OrganizationMembership`, não `AfiliacaoOrganizacao`).

### Testes novos (Identity.UnitTests)

`Queries/GetMeQueryHandlerTests.cs` (4): organizations restritas às memberships do usuário,
`ActiveOrganizationId` null quando token sem org, `pendingInvites` embutido, usuário não
encontrado. `dotnet build`/`dotnet test`: ver detalhamento consolidado no Status da task 016
(mesma sessão, mesma suíte) — 281/281, zero regressão.
