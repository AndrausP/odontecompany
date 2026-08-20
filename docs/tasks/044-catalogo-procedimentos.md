---
task: "044"
sprint: "11"
status: done
---

# 044 — Catálogo de Procedimentos (serviços com valor/duração padrão)

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BACKEND (Scheduling, novo agregado) + FRONTEND
**Critério de aceite:** Owner/Admin cadastra procedimentos (nome, valor padrão opcional, duração
padrão opcional) pela tela de Configurações; ao criar um agendamento, pode escolher um
procedimento do catálogo — o valor e a duração padrão pré-preenchem, mas continuam editáveis.

## Contexto

Feature nova, dentro da diretriz "trabalhe o resto da noite, inove". A Agenda até aqui só tinha
paciente/profissional/sala/horário — nenhum jeito de padronizar "isso é uma Limpeza, custa X,
dura Y minutos" entre agendamentos. Sem catálogo, cada agendamento tinha `valorConsulta` só na
conclusão (`MarcarAgendamentoConcluido`), digitado do zero toda vez.

## Execução

- `Procedimento` novo agregado (Domain, Scheduling) — `Nome`, `ValorPadrao` (nullable),
  `DuracaoPadraoMinutos` (nullable), `Ativo`. `Criar`/`AtualizarDados`/`Desativar`, mesma forma de
  `Profissional`/`Sala`.
- `Agendamento.ProcedimentoId` novo (nullable, trailing param em `Criar` — não quebra os ~380
  call sites existentes, mesmo padrão já usado pra `BranchId`).
- `CreateAgendamentoCommandHandler` valida `ProcedimentoId` quando informado: busca no
  `IProcedimentoRepository`, rejeita (`Agendamento.ProcedimentoNaoEncontrado`) se não existe ou
  está inativo — mesma defesa de referência que já existia pra paciente/profissional/sala.
- CRUD completo (Create/List/Update/Deactivate) em `Scheduling.Application`, `ProcedimentoRepository`
  (Infrastructure), `ProcedimentosController` (Owner/Admin escreve, qualquer autenticado lê) — cópia
  fiel do padrão Profissional/Sala, incluindo defesa de IDOR (`OrganizationId` do token).
- Migration `AddProcedimentoCatalog`: coluna `Agendamentos.ProcedimentoId` (nullable) + tabela
  `Procedimentos` com índice `(OrganizationId, Ativo)`.
- Frontend:
  - Card "Procedimentos" em `SettingsPage.tsx` (`ResourcesSection`, agora `xl:grid-cols-3`) —
    mesmo padrão create-form + lista + modal de edição de Profissionais/Salas.
  - `NovoAgendamentoModal.tsx`: select "Procedimento (opcional)" — ao escolher um procedimento
    com duração padrão, a hora de fim é recalculada automaticamente a partir da hora de início
    (`handleProcedimentoChange`); continua editável depois.

## Verificação

- `dotnet test` — 393/393 (+13 novos: domínio Procedimento, CRUD handlers, 2 casos novos em
  `CreateAgendamentoCommandHandlerTests` para `ProcedimentoId` válido/inválido).
- `npm run build`/`npm run lint` — limpos.
- Validado ao vivo (signup → onboarding → Configurações): criei procedimento "Limpeza" (R$ 150,00
  · 30 min), editei pelo modal, confirmei que aparece no select de "Novo agendamento" com o valor
  formatado. Submissão do agendamento completo bateu num requisito de infra à parte — Redis não
  rodando neste ambiente de dev (lock distribuído de `CreateAgendamentoCommandHandler`, já
  documentado em `errors-aprendidos.md`) — mas o log de queries confirma que o `ProcedimentoId`
  chega certo no handler e passa pela validação antes do lock.

## Status

planned → in-progress → in-review (QA) → **done**.
