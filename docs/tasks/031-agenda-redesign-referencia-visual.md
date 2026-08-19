---
task: "031"
sprint: "8"
status: done
---

# 031 — Redesign visual da Agenda (referência do usuário)

**Sprint:** docs/sprints/sprint-8.md
**Tipo:** FRONTEND
**Estimativa:** 1 dia
**Depende de:** 024/025 (tokens), 026 (nav — só leitura, não alterado nesta task)
**Critério de aceite:**
1. Layout bate com a estrutura da imagem de referência mais completa (`docs/images/ChatGPT Image
   Aug 19, 2026, 10_19_59 AM.png`, tratada como alvo pelo PO): header com título/subtítulo + sino
   de convites pendentes + botão "Novo agendamento"; calendário principal (FullCalendar) + sidebar
   direita com **Calendário rápido**, **Próximos agendamentos**, **Resumo do dia**; barra de
   filtros abaixo do calendário; legenda de cor.
2. **Zero endpoint novo.** Todo dado vem de `listAgendamentos`, `listProfissionais`,
   `listPatients` — já existentes, já usados em `NovoAgendamentoModal`.
3. Chip de evento no calendário mostra nome do paciente (via lookup client-side `pacienteId` →
   `Patient.nomeCompleto`) + status (cor + label), não procedimento — ver divergência #2 abaixo.
4. Clique no mini calendário navega o calendário principal (`FullCalendar.getApi().gotoDate`) pra
   data clicada; dia ativo destacado nos dois (mini calendário + resumo do dia).
5. "Resumo do dia" recalcula ao trocar o dia selecionado (clique em outro dia da grade semanal OU
   no mini calendário) — não fica fixo em "hoje".
6. Barra de filtros (busca por nome / profissional / status) filtra de verdade os eventos do
   calendário principal (client-side, sobre o array já carregado); "Limpar filtros" reseta os 3.
   **Sidebar direita não é afetada pelo filtro** (evita "resumo do dia" sumir enquanto o usuário
   só está buscando um paciente).
7. Responsivo: sidebar direita empilha abaixo do calendário em `<lg` (mesmo breakpoint do
   `AppLayout`).
8. Zero cor hardcoded nova — só tokens já existentes (`grep` por `slate-`, `emerald-`, `amber-`,
   `red-`, `blue-`, `#[0-9a-fA-F]{3,8}` em `AgendaPage.tsx` e nos componentes novos → zero match).
9. `npm run build` limpo (`tsc -b && vite build`).

## Divergências conscientes vs. imagem de referência (decididas em grill-me com o usuário)

1. **Sem busca duplicada.** A imagem tem 2 campos de busca (header "⌘K" + barra de filtros). Só
   implementada a da barra de filtros — evita 2 inputs fazendo a mesma coisa. Header da Agenda não
   ganhou busca.
2. **Legenda por status, não por procedimento.** A imagem colore por tipo de procedimento
   (Consulta/Avaliação/Limpeza/Restauração/Clareamento/Canal, 6 cores). `Agendamento` (domínio) não
   tem campo de procedimento — só `status` (`Agendado/Confirmado/Concluido/Cancelado`), que já é o
   que colore o evento hoje (`eventColorByStatus`, tokenizado). Mudar pra procedimento exigia campo
   novo no domínio + migration, fora da regra de negócio da sprint ("só visual, dado já existe").
   Mantido: 4 cores de status, já tokenizadas (`StatusBadge`/`toneClasses`).
3. **Toggle de tema não migrou pro header.** Continua no rodapé da sidebar (spec 024 §7.6, já
   implementado em toda a nav) — mover só na Agenda quebraria consistência entre telas.

## Escopo técnico

- `AgendaPage.tsx` reescrito: grid 2 colunas (`lg:grid-cols-[1fr_320px]`, 1 coluna em `<lg`).
- 3 componentes novos em `features/scheduling/`: `MiniCalendar.tsx`, `UpcomingAppointments.tsx`,
  `DaySummary.tsx` — todos client components puros, sem request próprio (recebem `Agendamento[]`
  já carregado pela página via prop).
- Lookups client-side: `useQuery(['profissionais'], listProfissionais)` e
  `useQuery(['patients','select'], () => listPatients({ pageSize: 200 }))` — mesmas `queryKey`s já
  usadas em `NovoAgendamentoModal`, cache compartilhado, zero request duplicado quando as duas
  telas são visitadas na mesma sessão.
- `eventContent` custom no `FullCalendar` (troca o `title` string simples por render function) pra
  bater com o chip da imagem (nome em negrito + linha de status com dot colorido).
- Sino de convites pendentes: `useMe()` já existente (mesma `queryKey ['me']` do `AppLayout`,
  zero request extra), leva pra `/convites` ao clicar — rota já existe na nav.
- KPI numérico do "Resumo do dia" **não** usa a classe `.text-kpi` (reservada por regra explícita
  do design-system.md §2.3 ao dashboard de comissões — "se aparecer em outro lugar, é quebra de
  padrão"). Usa `text-lg font-semibold` (escala normal do §2.2).

## Fora de escopo

- Endpoint/campo novo de procedimento no domínio `Scheduling`.
- Busca no header (ver divergência #1).
- Mudar posição do toggle de tema (ver divergência #3).
- Filtro de data/range diferente do que o `FullCalendar` já oferece (Mês/Semana/Dia).

## Riscos

- **R9 (baixo):** 3 requests na carga da Agenda em vez de 1 (`agendamentos` + `profissionais` +
  `patients`) — aceito pelo Tech Lead (ver Notas do Tech Lead, sprint-8), os 2 lookups já eram
  requests existentes em outra tela, não é I/O novo pro backend, só passa a disparar mais cedo.
- **R10 (baixo):** se um `agendamento.pacienteId` não estiver no primeiro `pageSize: 200` de
  pacientes (clínica com >200 pacientes ativos), o chip cai num fallback `"Paciente"` genérico em
  vez do nome — mesma limitação de paginação que já existe em outros seletores do app
  (`NovoAgendamentoModal` usa `pageSize: 100`), não é regressão introduzida por esta task.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | "Só visual, dado já existe" — grill-me com o usuário, 2026-08-19 |
| 2. Contexto | Reader → Writer | `AgendaPage.tsx` atual (FullCalendar puro, sem sidebar/filtro), design-system.md (024, done), `AppLayout.tsx` (sem header de conteúdo no desktop), `Agendamento` sem campo de procedimento |
| 3. Quebra | Tech Lead | Escopo técnico acima, 1 task só (re-skin de 1 tela, sem dependência cruzada) |
| 4. Estrutura | Architect | Sem contrato novo — 100% client-side sobre dado já exposto; decisão registrada em docs/decisions.md |
| 5. Aprovação | Product Owner | Aprovado — divergências #1/#2/#3 confirmadas em grill-me |
| 6. Implementação | Dev Frontend | `AgendaPage.tsx` + `MiniCalendar.tsx` + `UpcomingAppointments.tsx` + `DaySummary.tsx` |
| 7. Teste | QA | `npm run build` limpo; grep de cor hardcoded (critério 8) sem match; revisão manual do critério 1-7 |
| 8. Documentação | Writer | `docs/decisions.md` (divergências vs. imagem) + `docs/knowledge/patterns.md` (padrão "widget client-side sem request próprio, dado vem de cima") |

## Status

planned → in-progress → in-review (QA) → **done**.

## Notas

Imagens de referência ficam em `docs/images/` — não movidas/renomeadas (nome original do export
do ChatGPT preservado pra rastreabilidade de qual print gerou qual decisão).
