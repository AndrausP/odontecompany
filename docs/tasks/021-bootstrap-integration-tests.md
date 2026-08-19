---
task: "021"
sprint: "10"
status: done
---

# 021 — Bootstrap de suíte de integração com `WebApplicationFactory`

**Sprint:** docs/sprints/sprint-10.md (puxada do backlog de docs/sprints/sprint-6.md)
**Critério de aceite:** Projeto de testes de integração novo (`Microsoft.AspNetCore.Mvc.Testing`)
capaz de subir a API in-memory (HTTP real, pipeline de middleware real — auth, policies, rate
limiting, routing) e rodar pelo menos os 5 casos hoje cobertos só por inspeção estática (ver
Origem). Não precisa cobrir o resto da superfície de API nesta task — o objetivo é o
**bootstrap** (infraestrutura de teste reutilizável), não cobertura completa.

## Origem

Recomendação do QA na task `docs/tasks/019-qa-regressao-multi-org.md`: 5 dos 13 casos
obrigatórios de regressão multi-organização só puderam ser verificados por **inspeção estática de
código** (grep de atributos/policies/config), não por teste runtime, porque o projeto não tem
`Microsoft.AspNetCore.Mvc.Testing` instalado — nenhum teste hoje sobe a API real e faz uma
requisição HTTP de ponta a ponta contra o pipeline de middleware. Os 5 casos:

1. Token sem organização ativa é rejeitado (403) em endpoint de negócio e aceito em `/api/me`,
   criar organização e aceitar convite (policy `RequireActiveOrganization` — `Program.cs:157`).
2. Rate limit do signup dispara (`Program.cs:163-176`, política `signup`, PermitLimit=5/1min).
3. Convite pra email sem conta: criar conta com esse email → convite aparece em `/api/me` (fluxo
   E2E signup → login → `/api/me`, hoje só coberto em partes por testes de handler isolados).
4. `Owner` pode convidar; `Dentista`/`Recepcao` não (`[Authorize(Roles = "Owner,Admin")]` em
   `OrganizationsController.CreateInvite`).
5. Papéis existentes (Admin/Dentista/Recepcao) continuam se comportando como antes — nenhuma
   regressão de autorização introduzida pelo `Owner` (aditivo).

Nenhum desses é um bug conhecido — é lacuna de cobertura. Config declarativa (`[Authorize]`,
`[EnableRateLimiting]`, políticas em `Program.cs`) é fácil de quebrar silenciosamente numa
refatoração futura (ex: alguém remove um atributo sem querer) sem que nenhum teste unitário de
handler detecte, porque teste de handler nunca passa pelo pipeline de middleware/routing/auth.

## Escopo técnico (implementado)

1. Projeto novo `tests/Api.IntegrationTests`, referenciando `Microsoft.AspNetCore.Mvc.Testing` +
   só o `OdontoPlatform.Api.csproj` como `ProjectReference` (ele já referencia todos os módulos,
   transitivo). `Program.cs` ganhou `public partial class Program {}` no fim — sem isso a classe
   gerada pelos top-level statements é `internal`, invisível pro `WebApplicationFactory<Program>`
   de outro assembly. Zero mudança de comportamento do host.
2. **Banco de teste: InMemory** (decisão do Architect, não Postgres efêmero) — objetivo é
   exercitar o pipeline (auth/policy/rate limit/routing), não fidelidade de SQL; Postgres efêmero
   pediria infraestrutura de CI que o repo não tem, sem ganho pros 5 casos-alvo.
   `ApiWebApplicationFactory` troca os 7 `DbContext` (1 por módulo) de `UseNpgsql` pra
   `UseInMemoryDatabase`, 1 banco isolado por instância de factory (nome sufixado com
   `Guid.NewGuid()`). Ambiente forçado `"Testing"` — pula o seed automático do admin padrão
   (só roda em `"Development"`), cada teste constrói seu próprio dado via HTTP real.
3. **Sem helper de JWT manual** — decisão tomada durante a implementação, melhor que o rascunho
   original: os 4 endpoints públicos (`/api/auth/signup`, `/api/auth/login`,
   `/api/organizations` `POST`, `/api/invites/{token}/accept`) já dão qualquer token/estado que os
   5 casos precisam (inclusive "sem organization_id", que é exatamente o token que `/signup`
   devolve). Reimplementar assinatura de JWT no teste seria menos fiel (testaria o teste, não a
   API) e redundante. `AuthFlow.cs` só envelopa esses 4 endpoints como chamada HTTP normal.
4. Os 5 casos da Origem viram 9 testes de integração reais (`TokenWithoutOrganizationTests`,
   `SignupRateLimitTests`, `InviteFlowTests`) — os 8 já cobertos por teste de handler (task 019)
   não foram duplicados.
5. Suíte completa (unitária + integração) revalidada: **340 testes, 0 falha** — 322 unitários (9
   projetos) + 9 de integração + 9 de `OdontoPlatform.Api.UnitTests`.

## Dependências

- Não bloqueia nenhuma task hoje em andamento — é melhoria de infraestrutura de teste, backlog.
- Não depende de nenhuma task aberta.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Puxada do backlog junto com a 020, a pedido do usuário — "corrigir bugs e melhorar o produto", 2026-08-19 |
| 2. Contexto | Reader → Writer | Gap descrito em `docs/tasks/019-qa-regressao-multi-org.md`, casos 5/9/11/12/13; `AuthController`/`MeController`/`OrganizationsController`/`InvitesController`/`PatientsController` lidos pra mapear os 4 endpoints públicos usáveis nos testes |
| 3. Quebra | Tech Lead | Escopo acima — bootstrap + os 5 casos, sem duplicar cobertura unitária existente |
| 4. Estrutura | Architect | InMemory (não Postgres efêmero); `public partial class Program` no host; sem helper de JWT manual — usa os endpoints públicos reais |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Backend | `tests/Api.IntegrationTests/` (`ApiWebApplicationFactory.cs`, `AuthFlow.cs`, `TokenWithoutOrganizationTests.cs`, `SignupRateLimitTests.cs`, `InviteFlowTests.cs`) + `Program.cs` (marcador) |
| 7. Teste | QA | `dotnet test` em todos os 10 projetos — 340/340 verde |
| 8. Documentação | Writer | `docs/knowledge/errors-aprendidos.md` (bug real achado DURANTE esta task, ver Notas) + `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**. 9 testes novos, todos verdes; suíte inteira
(340 testes) revalidada sem regressão.

## Notas

Criada pelo Writer (2026-08-18) a partir da recomendação explícita do QA no handoff da task 019:
`"Recomendação: abrir task nova em sprint 7 pra bootstrap de suíte de integração end-to-end."`

**Bug real achado DURANTE a implementação desta task (não é o objetivo da task, foi achado
lateral):** `AuthFlow.cs` inicialmente usava `HttpContent.ReadFromJsonAsync<T>(customOptions)` com
`JsonSerializerOptions` só com `Converters = { new JsonStringEnumConverter() }` — resultado: TODO
campo string dos DTOs vinha `null` silenciosamente (sem exceção), 8 dos 9 testes falhavam com 401
em cascata. Causa: `ReadFromJsonAsync<T>()` SEM options explícitas usa por baixo um default "web"
implícito (camelCase + case-insensitive); passar `JsonSerializerOptions` custom substitui esse
fallback por um `JsonSerializerOptions` "puro" (case-SENSITIVE, sem naming policy) — e a API
serializa em `camelCase` (`"accessToken"`), então nenhuma propriedade batia contra os records em
PascalCase. Fix: `PropertyNameCaseInsensitive = true` + `PropertyNamingPolicy =
JsonNamingPolicy.CamelCase` junto do conversor de enum. Registrado em
`docs/knowledge/errors-aprendidos.md` — é o tipo de bug que reaparece em qualquer client C#/teste
HTTP que precise customizar `JsonSerializerOptions` além do que `ReadFromJsonAsync` já dá de
graça.
