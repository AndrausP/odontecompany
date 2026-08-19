---
sprint: "8"
status: done
---

# Sprint 8

**Período:** 2026-08-19 → (aberto)
**Objetivo:** Redesign visual da tela de Agenda a partir de imagens de referência do usuário
(`docs/images/`), reaproveitando 100% do design system da sprint-7 (tokens verde-pinho, dark/light)
e sem endpoint novo.
**Aprovado por (PO):** Product Owner (decisão do usuário + broker, ver docs/decisions.md) — 2026-08-19

## Contexto herdado da sprint-7

Design system (task 024) e sua implementação (025-029) estão `done` — tokens, `AppLayout`
redesenhado, dashboard de comissão, comprovante, tudo já tokenizado. Esta sprint não mexe em
tokens novos, só consome os existentes numa tela específica (Agenda).

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 031 | Redesign visual da Agenda (referência do usuário) | done | Dev Frontend |

<!-- Status possíveis: planned | in-progress | blocked | done -->

## Notas do Tech Lead

Usuário forneceu 3 prints (`docs/images/`) — os 2 primeiros (`09_45_06`, `10_17_17`) são iguais
entre si; o terceiro (`10_19_59`) é mais completo (busca no header, calendário rápido, próximos
agendamentos, resumo do dia, barra de filtros, legenda) e foi tratado como alvo final pelo PO.

Duas divergências conscientes entre a imagem e o que foi implementado, decididas em grill-me com
o usuário — ver task 031 §Notas:
1. Sem busca duplicada — só a barra de filtros abaixo do calendário, sem a busca solta no header.
2. Legenda por **status** (já tokenizado, já é o que colore o evento hoje), não por **procedimento**
   — o domínio (`Agendamento`) não tem campo de tipo de procedimento; inventar essa dimensão exigia
   endpoint/campo novo, fora da regra de negócio desta sprint ("só visual, dado já existe").

## Retrospectiva

Sprint fechada com 1 task, escopo restrito a re-skin de tela existente (zero mudança de contrato
de API, zero migration). Risco identificado e aceito: chip de evento agora depende de 2 lookups
client-side (pacientes, profissionais) além do próprio `listAgendamentos` — 3 requests em vez de 1
na carga inicial da Agenda; aceitável porque os 3 já são cacheados/reusados em outras telas
(`['profissionais']`, `['patients','select']` já existiam antes desta sprint).
