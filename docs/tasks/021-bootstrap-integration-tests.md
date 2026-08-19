---
task: "021"
sprint: "a definir"
status: planned
---

# 021 — Bootstrap de suíte de integração com `WebApplicationFactory`

**Sprint:** a definir (backlog, achado durante docs/sprints/sprint-6.md — não é sprint-7 ainda)
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

## Escopo técnico (rascunho — Tech Lead confirma ao puxar da fila)

1. Projeto novo `tests/Api.IntegrationTests` referenciando `Microsoft.AspNetCore.Mvc.Testing` +
   `WebApplicationFactory<Program>` (ou `Bootstrap.OdontoPlatform.Api` — confirmar entry point
   público/`partial class Program` se necessário).
2. Banco de teste: decidir entre `UseInMemoryDatabase` sobrescrito via `IHostBuilder` custom (mais
   rápido, mas não exercita SQL/constraints reais) ou um Postgres real efêmero (mais fiel, custo de
   infraestrutura de CI) — **decisão do Architect**, não do Dev que implementa.
3. Helper de autenticação de teste (emitir JWT válido de teste com claims controladas — inclusive
   o cenário "sem `organization_id`") pra exercitar os 5 casos da Origem sem precisar re-implementar
   login em cada teste.
4. Os 5 casos da Origem viram testes de integração reais nesta task; os demais 8 casos já cobertos
   por teste de handler (task 019) **não precisam ser duplicados** aqui — trocar cobertura unitária
   por integração só onde o unitário não alcança (config declarativa de pipeline).
5. Reexecutar toda a suíte (unitária + integração) e confirmar que o número total de testes só
   cresce, sem quebrar nenhum dos 293 existentes.

## Dependências

- Não bloqueia nenhuma task hoje em andamento — é melhoria de infraestrutura de teste, backlog.
- Não depende de nenhuma task aberta.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | [pendente — puxar da fila de backlog] |
| 2. Contexto | Reader → Writer | Gap descrito em `docs/tasks/019-qa-regressao-multi-org.md`, casos 5/9/11/12/13. |
| 3. Quebra | Tech Lead | [pendente] |
| 4. Estrutura | Architect | [pendente — decidir InMemory vs Postgres efêmero pra integração] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | [pendente] |

## Status

planned → in-progress → in-review (QA) → done

## Notas

Criada pelo Writer (2026-08-18) a partir da recomendação explícita do QA no handoff da task 019:
`"Recomendação: abrir task nova em sprint 7 pra bootstrap de suíte de integração end-to-end."`
Sprint de destino em aberto — não é bloqueante pro fechamento da sprint 6, mas fica registrada
como pendência de backlog visível.
