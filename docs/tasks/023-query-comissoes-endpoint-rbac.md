---
task: "023"
sprint: "7"
status: done
---

# 023 — Query de comissões em Reporting + endpoint + RBAC de ownership

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** BACKEND
**Estimativa:** 3 dias
**Depende de:** 022 (contratos de leitura)
**Critério de aceite:**
1. `GET /api/reports/comissoes?dataInicio&dataFim&branchId?&classe?&profissionalId?` responde 200
   com totais do período **e média diária** calculada pelo backend.
2. Usuário com role `Dentista` recebe **apenas a própria comissão**, mesmo que passe
   `profissionalId` de outro na query string (parâmetro ignorado/sobrescrito, não erro 500).
3. `Admin`/`Owner` recebem o agregado da organization, com recorte opcional por `branchId` e por
   `classe` (Role).
4. `Recepcao` recebe 403 (não vê remuneração de terceiro).
5. Período inválido (`dataFim < dataInicio`) → 400 com mensagem, não exceção.
6. Testes de handler cobrindo os 4 cenários de RBAC + média diária.

## Decisão de escopo (Tech Lead — já tomada)

**D3 — uma query só, não duas.** `GetComissoesPorPeriodoQuery` serve as duas visões. A diferença
entre "visão colaborador" e "visão empresa" é **quem preenche o filtro**, não a query. Duas
queries duplicariam a agregação e a média diária pro júnior manter em dois lugares.

**D4 — controller novo, não método novo no `ReportsController`.** `ReportsController` é
`[Authorize(Roles = "Admin")]` **na classe**. Em ASP.NET Core múltiplos `[Authorize]` são
cumulativos (AND) — um `[Authorize(Roles="Dentista")]` no método **não afrouxa** o da classe, o
Dentista continuaria bloqueado. Criar `ComissoesController` (`api/reports/comissoes`) com sua
própria policy evita mexer nos 3 endpoints Admin-only existentes e evita regressão.

**D5 — escopo resolvido no controller, nunca no cliente.** Mesmo padrão já usado em
`SchedulingController.ListAgendamentos` (`branchId = Role == "Recepcao" ? _currentUser.BranchId : null`):
o controller lê `ICurrentUserAccessor` e força os filtros. A query recebe valores já sanitizados.

## Escopo técnico

### 1. `Reporting.Application/Queries/GetComissoesPorPeriodo/`

`GetComissoesPorPeriodoQuery(Guid OrganizationId, DateTime DataInicio, DateTime DataFim, Guid? BranchId, string? Classe, Guid? ProfissionalId)`
+ `Handler` + `Validator` (mesmo trio dos 3 queries existentes).

Handler compõe **em memória**, sem JOIN cruzado:
1. `IComissaoSummaryProvider.ObterComissoesAsync(org, inicio, fim)` → comissão por `ProfissionalId`.
2. `IProfissionalLookup.ListarPorOrganizationAsync(org)` → `Nome`, `UserId`, `BranchId`.
3. `IMembershipLookup.ListarPorOrganizationAsync(org)` → `Role` por `UserId`.
4. Junta em memória por `ProfissionalId` → `UserId`; aplica filtros `BranchId`/`Classe`/`ProfissionalId`.
5. Agrega totais + média diária.

### 2. DTO de resposta (`Reporting.Contracts.ComissaoResumoDto`)

```
record ComissaoResumoDto(
    Guid OrganizationId,
    DateTime DataInicio,
    DateTime DataFim,
    int DiasNoPeriodo,                 // inclusivo nas duas pontas
    decimal ValorComissaoTotal,
    decimal ValorPagoTotal,
    int QuantidadeFaturas,
    decimal MediaDiariaComissao,       // ValorComissaoTotal / DiasNoPeriodo
    decimal MediaDiariaValorPago,
    IReadOnlyList<ComissaoLinhaDto> Linhas);

record ComissaoLinhaDto(
    Guid? ProfissionalId,
    string ProfissionalNome,           // "Não atribuído" quando ProfissionalId == null
    Guid? BranchId,
    string? BranchNome,
    string? Classe,                    // Role da membership; null quando não há membership
    decimal ValorPago,
    decimal ValorComissao,
    int QuantidadeFaturas,
    decimal MediaDiariaComissao);
```

**Média diária é calculada no backend**, não no front: é regra de negócio (definição de "dias do
período"), não formatação. Período é **inclusivo nas duas pontas** (`(fim - inicio).Days + 1`),
mínimo 1 dia — nunca dividir por zero.

**Architect — A3:** `BranchNome` vem de `Tenancy.Contracts.IBranchLookup.ListarNomesAsync()` novo.

Adicionar à interface (Tenancy.Contracts):
```csharp
Task<IReadOnlyDictionary<Guid, string>> ListarNomesAsync(
    Guid organizationId, CancellationToken ct = default);
```

Retorna dicionário `BranchId → BranchNome`. Implementação em `Tenancy.Infrastructure` com `AsNoTracking()` + `IgnoreQueryFilters()`.

Motivo: IBranchLookup hoje só tem `ExistsAsync`; estender é mais limpo que carregar branches no Profissional — Reporting fica desacoplado de Scheduling, chama os 4 lookups (Comissao + Profissional + Membership + BranchNomes) pra compor em memória.

### 3. `ComissoesController` (Bootstrap/OdontoPlatform.Api)

- `[Authorize(Roles = "Owner,Admin,Dentista", Policy = "RequireActiveOrganization")]`.
- `Recepcao` fica de fora da lista de roles → 403 automático.
- Se `_currentUser.Role == "Dentista"`: resolver o `ProfissionalId` **do próprio usuário** a partir
  de `UserId` e forçá-lo na query; `branchId` e `classe` recebidos do cliente são descartados.
- Se `Admin`/`Owner`: repassa os filtros como vieram.
- `OrganizationId` sempre do token, nunca do cliente.

**Architect — A4:** Controller resolve `UserId → ProfissionalId` via sobrecarga `IProfissionalLookup.ObterPorUserIdAsync()`.

Adicionar à interface (Scheduling.Contracts):
```csharp
Task<ProfissionalResumoDto?> ObterPorUserIdAsync(
    Guid organizationId, Guid userId, CancellationToken ct = default);
```

Retorna o resumo do profissional ou `null` se não vinculado (tratado no controller como resposta 200 com totais zerados). Implementação em `Scheduling.Infrastructure` — lookup único, batch-ready, sem N+1.

Motivo: permite que `ComissoesController` resolva `UserId → ProfissionalId` sem carregar lista inteira de profissionais; desacoplado de Scheduling.Domain/Infrastructure (só usa contrato).
**Caso de borda a tratar:** Dentista **sem** `Profissional` vinculado (`UserId` não bate com nenhum)
→ resposta 200 com totais zerados e lista vazia, **não** 404 e **não** dados de outra pessoa.

### 4. Testes (NUnit + Moq, mocks dos 3 lookups)

- `Should_RetornarApenasProprioProfissional_When_UsuarioEhDentista`
- `Should_IgnorarProfissionalIdDoCliente_When_UsuarioEhDentista`
- `Should_RetornarAgregadoDaOrganization_When_UsuarioEhAdmin`
- `Should_FiltrarPorBranch_When_BranchIdInformado`
- `Should_FiltrarPorClasse_When_ClasseInformada`
- `Should_CalcularMediaDiariaInclusiva_When_PeriodoDeUmDia` (média == total)
- `Should_RetornarZerado_When_DentistaSemProfissionalVinculado`
- `Should_RetornarErro_When_DataFimAnteriorADataInicio`

## Riscos

- **R3 (médio):** composição em memória — a junção carrega todos os profissionais e memberships da
  organization por request. Aceitável na escala atual (dezenas), não em milhares. Dívida nomeada,
  mesma classe de dívida já registrada em docs/tasks/008-inteligencia-bi.md.
- **R4 (baixo):** filtro por "classe" sobre comissão vai devolver praticamente só `Dentista`/`Owner`
  — recepção não gera comissão. Não é bug, é o dado. Frontend precisa mostrar estado vazio decente.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Visão colaborador vs empresa, média diária (sprint-7) |
| 2. Contexto | Reader → Writer | Padrão de escopo no controller (ListAgendamentos/BranchId) |
| 3. Quebra | Tech Lead | D3/D4/D5 decididas acima |
| 4. Estrutura | Architect | A3: `IBranchLookup.ListarNomesAsync()` novo; A4: `IProfissionalLookup.ObterPorUserIdAsync()` novo |
| 5. Aprovação | Product Owner | aprovado |
| 6. Implementação | Dev Backend | handler/validator/DTOs/controller implementados (sessão anterior); testes cobrindo os 8 cenários do critério de aceite adicionados nesta sessão |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | documentado — ver docs/knowledge/patterns.md (controller separado por Authorize cumulativo, regime de caixa, RBAC de ownership em memória) e docs/knowledge/errors-aprendidos.md (nota sobre handoff x spec fechada) |

## Status

planned → in-progress → in-review (QA) → qa-approved → **done**

### QA (2026-08-18, Jubileu — pass)

**RBAC de ownership (superfície de segurança #1 da sprint) — auditado linha a linha em `ComissoesController.cs`:**

- Linha 21: `[Authorize(Roles = "Owner,Admin,Dentista", Policy = "RequireActiveOrganization")]` — Recepcao NÃO figura na lista → 403 automático pelo framework. **Confirmado.**
- Linhas 62-70: se `Role == "Dentista"`, o controller busca o próprio Profissional via UserId, força `profissionalId = proprioProfissional?.Id ?? ProfissionalNaoVinculadoSentinel` e ZERA `branchId` e `classe` — o parâmetro vindo da query string é IGNORADO. Coberto pelo teste `Should_IgnoreClientFiltersAndForceOwnProfissional_When_UsuarioEhDentista`, que envia `outroProfissionalId + outroBranchId + "Owner"` como Dentista e valida via `Callback` que a query enviada ao MediatR tem `ProfissionalId==proprioProfissionalId`, `BranchId==null`, `Classe==null`. **Confirmado — Dentista NÃO consegue ver comissão de terceiro mesmo forjando query string.**
- Linhas 58-60: `OrganizationId` sempre lido de `_currentUser.OrganizationId` (token), nunca do cliente. Se null → 401 sem chamar mediator (teste `Should_ReturnUnauthorized_When_OrganizationIdIsNull` com `Verify(Times.Never)`).
- Dentista sem Profissional vinculado: sentinel `Guid.Empty` no `profissionalId` → handler filtra `linhas.Where(l => l.ProfissionalId == Guid.Empty)` → vazio; `profissionalPorId.TryGetValue(Guid.Empty, ...)` falha porque nenhum Profissional real usa `Guid.Empty` (BaseEntity gera via `Guid.NewGuid()`) → resposta 200 com `Linhas` vazia e totais zerados, sem exception, sem 404, sem dado de terceiro. Coberto por `Should_UseSentinel_When_DentistaHasNoProfissionalVinculado` (controller) + `Should_ReturnEmptyListAndZeroedTotals_When_ProfissionalIdIsSentinelAndNotFound` (handler).
- Admin/Owner: filtros passam direto sem sobrescrita. Coberto por `Should_PassThroughClientFilters_When_UsuarioEhAdmin`, que ainda faz `Verify(Times.Never)` no ProfissionalLookup — confirma que Admin NÃO paga o custo de resolver o próprio profissional.

**Demais verificações:**

- Média diária (Handler linha 161-162): `Math.Max((dataFim.Date - dataInicio.Date).Days + 1, 1)` — inclusivo nas 2 pontas + mínimo 1 dia. Sem divisão por zero em período de 1 dia (`DiasNoPeriodo == 1`). Coberto por `Should_CalculateOneDayPeriod_When_DataInicioEqualsDataFim` (dia único → dias=1) e `Should_CalculateMediaDiaria_When_MultiplosDiasNoPeriodo` (10 dias inclusivos → 500/10=50).
- Arredondamento `MidpointRounding.ToEven` (banker's rounding, linha 164) coberto por `Should_RoundMediaDiariaToEven_When_ResultIsExactMidpoint` com ponto médio exato 100.25/2=50.125 → 50.12 (não 50.13). Teste real, não mecânico.
- Validator (`GetComissoesPorPeriodoQueryValidator`): `DataFim >= DataInicio` e `OrganizationId.NotEmpty()`. Handler retorna `Result.Failure` que o controller mapeia pra 400 (`Should_ReturnBadRequest_When_HandlerReturnsFailure`). Coberto no `Validator` e no controller.

**Cobertura dos testes contra o critério de aceite (linhas 13-22):**
- (1) endpoint 200 com totais e média diária — coberto (happy path do handler).
- (2) Dentista com `profissionalId` de outro ignorado — coberto (`Should_IgnoreClientFiltersAndForceOwnProfissional_When_UsuarioEhDentista`).
- (3) Admin/Owner com recorte por branchId/classe — coberto (`Should_ReturnOnlyProfissionaisFromBranch_When_BranchIdInformado`, `Should_ReturnOnlyRoleInformado_When_ClasseInformada`, `Should_PassThroughClientFilters_When_UsuarioEhAdmin`).
- (4) Recepcao 403 — coberto declarativamente pelo `[Authorize(Roles=...)]` (não há Recepcao na lista); teste de unidade direto não aplica (comportamento do framework, controller nem é instanciado).
- (5) Período inválido → 400 — coberto (Validator + `Should_ReturnBadRequest_When_HandlerReturnsFailure`).
- (6) 4 cenários RBAC + média diária no handler — coberto (9 testes de handler + 5 de controller).

**Risco residual (não-bloqueante):**

1. Recepcao 403 não tem teste automatizado (é declarativo no atributo `[Authorize]`). Nada crítico — se alguém acidentalmente adicionar `Recepcao` à lista, o gate cai. Sugestão pra próxima sprint: teste de integração com `WebApplicationFactory` cobrindo a matriz de roles.
2. `IProfissionalLookup.ObterPorUserIdAsync` (A4 do Architect) NÃO foi criado — controller usa `ListarPorOrganizationAsync().FirstOrDefault(UserId==...)`. Funcionalmente correto (mesmo resultado, mesmo isolamento cross-org, sem N+1 porque `ListarPorOrganization` é 1 round-trip). Só discrepância formal com a spec da A4, já sinalizada pelo Dev na 022. Precisa ratificação do Architect/PO se quiserem alinhamento formal.
3. Dev não implementou `IComissaoSummaryProvider.ObterComissoesAsync(profissionalIds)` (filtro opcional que o handoff pediu mas A2 não incluiu). O filtro por `profissionalId` acontece em memória no handler (linha 71) — funcionalmente equivalente na escala atual (dezenas de profissionais por org, R3 já reconhecido).

## Nota de teste (Dev Backend, sessão de retomada)

Implementação (handler/validator/DTO/controller) já estava pronta de uma sessão anterior — só
faltavam os testes. Cobertura adicionada:

- `tests/Reporting.UnitTests/Queries/GetComissoesPorPeriodoQueryHandlerTests.cs` (9 testes) —
  filtro por `branchId`, filtro por `classe`, filtro por `profissionalId`, sentinel `Guid.Empty`
  (Dentista sem Profissional vinculado → `Linhas` vazia + totais 0, `MontarLinhaZerada` NUNCA
  dispara pro sentinel — caso distinto do profissional real sem comissão no período, que gera 1
  linha zerada), período de 1 dia sem divisão por zero, média diária (total ÷ dias) e
  arredondamento `MidpointRounding.ToEven` (banker's rounding, testado com ponto médio exato
  100.25/2 = 50.125 → 50.12, não 50.13).
- `tests/Reporting.UnitTests/Queries/GetComissoesPorPeriodoQueryValidatorTests.cs` (3 testes) —
  `dataFim < dataInicio` falha, `dataFim == dataInicio` passa, `organizationId` vazio falha.
- `tests/OdontoPlatform.Api.UnitTests/Controllers/ComissoesControllerTests.cs` (5 testes, **projeto
  novo**) — não havia nenhum `*ControllerTests` em todo o repo (nem projeto de teste referenciando
  `Bootstrap/OdontoPlatform.Api`); como a RBAC de ownership do Dentista é lógica de verdade (não
  delegação pura pro MediatR), criei `tests/OdontoPlatform.Api.UnitTests` (SDK padrão +
  `FrameworkReference Microsoft.AspNetCore.App`, sem WebApplicationFactory — instancia o
  controller direto com mocks) e adicionei ao `.sln`. Cobre: Dentista com filtros de terceiro
  ignorados/forçados pro próprio `ProfissionalId`; Dentista sem Profissional vinculado → sentinel;
  Admin com filtros passando direto (sem consultar `IProfissionalLookup`); `OrganizationId` nulo →
  401 sem chamar o mediator; handler retornando falha → 400.

`dotnet build` limpo (0 erros/0 avisos). `dotnet test`: baseline 309/309 → **326/326** depois dos
testes novos (Reporting.UnitTests 2→14, OdontoPlatform.Api.UnitTests 0→5 projeto novo).

## Notas

Bloqueia 027 e 028 (frontend). Não bloqueia 024/025/026 — design e fundação visual podem rodar em
paralelo com o backend.
