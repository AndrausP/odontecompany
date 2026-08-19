---
task: "020"
sprint: "a definir"
status: planned
---

# 020 — Reativar `OrganizationMembership` ao aceitar convite novo pra membership inativa

**Sprint:** a definir (backlog de docs/sprints/sprint-6.md — não é sprint-7 ainda)
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

## ⚠️ NOTA BLOQUEANTE

**Não implementar nenhum endpoint de desativar/remover membro (`DELETE
/api/organizations/{id}/members/{userId}` ou equivalente) antes desta task estar concluída.** No
dia em que esse endpoint existir sem o fix desta task, o fluxo "Owner desativa alguém → convida de
volta → pessoa 'aceita' e nunca mais consegue entrar" vira alcançável em produção
imediatamente — silent failure, sem exception, sem log de erro.

## Escopo técnico (rascunho — Tech Lead confirma ao puxar da fila)

1. `OrganizationMembership.Reativar()` — novo método no Domain
   (`Identity.Domain.Entities.OrganizationMembership`), espelhando o padrão já usado por
   `Desativar()` (soft-state, sem novo registro, sem quebrar o índice único `(OrganizationId,
   UserId)`). Rejeitar reativação se a membership já estiver `Ativo` (idempotência local — não é
   erro, mas não deveria mudar nada; Tech Lead decide se é no-op silencioso ou `Result.Failure`
   informativo).
2. `AcceptInviteCommandHandler` — no branch de idempotência (linhas 69-76 hoje), quando
   `existingMembership is not null`, checar `existingMembership.IsAtivo`:
   - Se `true`: comportamento atual mantido (marca convite Aceito, retorna sucesso com o Role
     existente).
   - Se `false`: chamar `existingMembership.Reativar()` (atualizar `Role` pro role do convite
     novo, se o convite tiver papel diferente — Tech Lead/Architect decide se troca de role no
     reativar ou preserva o role anterior), persistir, marcar convite Aceito, retornar sucesso.
3. Teste de regressão explícito: `Should_ReactivateMembership_When_AcceptingInviteForInactiveMembership`
   — cobre o cenário completo (membership inativa → convite → accept → membership fica Ativo,
   role atualizado ou preservado conforme decisão do item 2).
4. Revalidar `Should_AllowInvite_When_InvitedEmailHasInactiveMembership` (teste já existente da
   task 016) continua passando sem alteração de comportamento no `CreateInviteCommandHandler`.

## Dependências

- Não bloqueia nem é bloqueada por nenhuma task hoje em andamento — é follow-up isolado.
- **Bloqueia implicitamente** qualquer task futura que implemente endpoint de
  desativar/remover membro (ver nota bloqueante acima).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | [pendente — puxar da fila de backlog] |
| 2. Contexto | Reader → Writer | Bug latente descrito em `docs/knowledge/errors-aprendidos.md`; código-fonte em `AcceptInviteCommandHandler.cs` linhas 69-76. |
| 3. Quebra | Tech Lead | [pendente] |
| 4. Estrutura | Architect | [pendente] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Backend | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | [pendente] |

## Status

planned → in-progress → in-review (QA) → done

## Notas

Criada pelo Writer (2026-08-18) como fechamento formal da ressalva registrada em
`docs/tasks/016-invite-afiliacao.md` — status `qa-approved-with-caveat` fechado como `done` só
depois desta task existir com nota bloqueante explícita.
