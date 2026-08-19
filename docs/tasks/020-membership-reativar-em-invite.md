---
task: "020"
sprint: "10"
status: done
---

# 020 — Reativar `OrganizationMembership` ao aceitar convite novo pra membership inativa

**Sprint:** docs/sprints/sprint-10.md (puxada do backlog de docs/sprints/sprint-6.md)
**Critério de aceite:** Quando um usuário aceita um convite pra uma organização onde já possui
`OrganizationMembership` **inativa**, a membership é reativada (não fica presa em `Inativo` com
resposta de sucesso). O caso de membership **ativa** continua exatamente como hoje (idempotência
normal — convite não deveria nem existir nesse caso, já bloqueado em `CreateInviteCommandHandler`).

## Origem

Bug latente achado pelo QA na validação da task `docs/tasks/016-invite-afiliacao.md` (ver
`docs/knowledge/errors-aprendidos.md`, entrada `[2026-08-18] Bug latente:
AcceptInviteCommandHandler não reativa OrganizationMembership inativa...`). Aprovado com ressalva
não-bloqueante porque **hoje é inalcançável via API** — nenhum endpoint dispara
`OrganizationMembership.Desativar()` ainda.

## ⚠️ NOTA BLOQUEANTE (resolvida em 2026-08-19)

~~Não implementar nenhum endpoint de desativar/remover membro antes desta task estar
concluída.~~ Fix implementado e testado — endpoint de desativar/remover membro pode ser
implementado com segurança a partir de agora, sem herdar o silent failure.

## Escopo técnico (implementado)

1. `OrganizationMembership.Reativar(Role role)` — novo método no Domain, mesmo padrão
   incondicional de `Desativar()` (soft-state, sem novo registro, sem quebrar o índice único
   `(OrganizationId, UserId)`). Sem guarda de "já ativo": o único call site já só chama isto
   quando `IsAtivo` é `false`.
2. `AcceptInviteCommandHandler` — branch de `existingMembership is not null` agora bifurca em
   `existingMembership.IsAtivo`:
   - `true`: comportamento antigo mantido (marca convite Aceito, retorna sucesso com o Role
     existente, sem tocar o papel).
   - `false`: chama `existingMembership.Reativar(invite.Role)` — **decisão: o papel do convite
     novo prevalece sobre o papel antigo** (quem re-convida escolhe o papel explicitamente; ver
     docs/decisions.md) — depois marca convite Aceito, persiste, retorna sucesso.
3. Teste de regressão: `Should_ReactivateMembership_When_AcceptingInviteForInactiveMembership`
   (`tests/Identity.UnitTests/Commands/AcceptInviteCommandHandlerTests.cs`) — membership Admin
   desativada + convite novo com Role.Dentista → membership fica Ativo com Role.Dentista.
4. `Should_AllowInvite_When_InvitedEmailHasInactiveMembership` (teste já existente da task 016)
   não foi tocado — `CreateInviteCommandHandler` não mudou, continua passando (9/9 verde em
   `Identity.UnitTests`).

## Dependências

- Não bloqueia nem é bloqueada por nenhuma task hoje em andamento — é follow-up isolado.
- **Bloqueia implicitamente** qualquer task futura que implemente endpoint de
  desativar/remover membro (ver nota bloqueante acima).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Puxada do backlog (sprint-6) a pedido do usuário — "corrigir bugs e melhorar o produto", 2026-08-19 |
| 2. Contexto | Reader → Writer | Bug latente descrito em `docs/knowledge/errors-aprendidos.md`; código-fonte em `AcceptInviteCommandHandler.cs` |
| 3. Quebra | Tech Lead | Escopo técnico acima — 1 task isolada, sem dependência |
| 4. Estrutura | Architect | Papel do convite novo prevalece na reativação (decisão registrada em docs/decisions.md) |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Backend | `OrganizationMembership.cs` + `AcceptInviteCommandHandler.cs` |
| 7. Teste | QA | `dotnet test tests/Identity.UnitTests` — 9/9 verde (8 existentes + 1 novo) |
| 8. Documentação | Writer | `docs/knowledge/errors-aprendidos.md` (entrada 2026-08-18 atualizada com a correção) + `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**. `dotnet build tests/Identity.UnitTests` limpo,
9/9 testes verdes.

## Notas

Criada pelo Writer (2026-08-18) como fechamento formal da ressalva registrada em
`docs/tasks/016-invite-afiliacao.md` — status `qa-approved-with-caveat` fechado como `done` só
depois desta task existir com nota bloqueante explícita.
