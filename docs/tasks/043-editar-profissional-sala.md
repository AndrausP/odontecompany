---
task: "043"
sprint: "11"
status: done
---

# 043 — Editar Profissional e Sala (fecha lacuna create-only da task 041/042)

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BACKEND (Scheduling) + FRONTEND
**Critério de aceite:** Owner/Admin edita Nome/Especialidade/TipoContrato/Comissão/Email de um
Profissional já cadastrado, e Nome/Capacidade de uma Sala já cadastrada, pela tela de
Configurações — sem precisar recriar o registro.

## Contexto

Sessão de melhoria contínua (usuário pediu pra trabalhar em novas features/melhorias). Achado
óbvio ao revisar o que ficou pra trás: `ProfissionaisController`/`SalasController` só tinham
`Create`+`List` — qualquer erro de digitação no cadastro (nome, especialidade, tipo de contrato)
não tinha conserto pela UI. `Profissional`/`Sala` já tinham `Desativar`/`Ativar` no domínio desde
sempre, mas nunca ganharam `AtualizarDados`.

## Execução

- `Profissional.AtualizarDados`/`Sala.AtualizarDados` novos no domínio — mesma validação de
  `Criar`/`Criar`. `AtualizarDados` do Profissional nunca mexe em `UserId` (vínculo de acesso só
  muda via `VincularUsuario`, no aceite de convite).
- `UpdateProfissionalCommand`/`DeactivateProfissionalCommand` e `UpdateSalaCommand`/
  `DeactivateSalaCommand` novos (Application) — mesma defesa de IDOR já padronizada desde a task
  040 (`OrganizationId` do token, erro `NaoEncontrado` idêntico pra "não existe" e "outra
  organization"). `Deactivate*` criados no backend por simetria mas SEM botão na UI ainda —
  desativar sem um jeito de reativar pela tela seria uma armadilha pro usuário (o backend já tem
  `Ativar()`, mas nenhum endpoint chama; ficar pra quando fizer sentido expor os dois juntos).
- `PUT /api/profissionais/{id}` e `PUT /api/salas/{id}` novos (Owner/Admin).
- Frontend: `EditProfissionalModal`/`EditSalaModal` (mesmo padrão de `EditBranchModal`, task 040),
  botão de lápis em cada linha da lista em `SettingsPage.tsx`.

## Verificação

- `dotnet build`/`dotnet test` — 380/380 (+8 novos: Update/Deactivate × Profissional/Sala).
- `npm run build`/`npm run lint` — limpos.
- Validado ao vivo: editei "Dra. Camila Rocha" (cadastro legado da sessão anterior à task 042, sem
  TipoContrato válido) pra "Ortodontia · CLT" — modal preenche, salva, lista atualiza.

## Status

planned → in-progress → in-review (QA) → **done**.
