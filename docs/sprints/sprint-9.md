---
sprint: "9"
status: done
---

# Sprint 9

**Período:** 2026-08-19 → 2026-08-19
**Objetivo:** Reskin de cor do modo dark (tokens + botão primário + item de nav ativo + flourish
decorativo da sidebar) pra bater com as fotos dark de referência do usuário — 2ª rodada de pedido
de fidelidade visual (1ª rodada, sprint-8, foi layout/estrutura da Agenda a partir da foto light).
**Aprovado por (PO):** Product Owner (decisão do usuário + broker, ver docs/decisions.md) — 2026-08-19

## Contexto herdado da sprint-8

Layout da Agenda (mini calendário, próximos agendamentos, resumo do dia, filtros) já `done` —
esta sprint não mexe em layout, só em cor/tema do `.dark` + branding (botão primário, nav ativo).

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 032 | Reskin dark mode (fidelidade às fotos de referência) | done | Dev Frontend |

<!-- Status possíveis: planned | in-progress | blocked | done -->

## Notas do Tech Lead

Usuário escolheu as 2 fotos DARK (não a light já usada na sprint-8) como alvo desta rodada, e
confirmou que a fidelidade de cor tem prioridade sobre a regra "não pareça vibe codado" da
024/design-system.md — decisão nova que **substitui** aquela preferência só pro modo dark; o modo
light continua exatamente como a 024/025 deixaram (não foi tocado nesta sprint).

Escopo tratado como "design system inteiro" (confirmado pelo usuário): `--color-brand` e afins no
`.dark` mudaram nos tokens globais, então toda tela herda automaticamente (Financeiro, Estoque,
Pacientes, Relatórios, nav) — não só a Agenda.

## Retrospectiva

Decisão consciente de NÃO recolorir os chips de evento do calendário (que hoje são por status,
não por cor arbitrária por agendamento como nas fotos) — essa é uma decisão de dado/semântica já
fechada na sprint-8 (task 031), não de tema visual; misturar as duas reabriria uma decisão já
aprovada sem necessidade. Se o usuário quiser eventos com cores mais variadas tipo a foto, é pedido
novo, não reskin de tema.
