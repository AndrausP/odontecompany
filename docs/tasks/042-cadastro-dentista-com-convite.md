---
task: "042"
sprint: "11"
status: done
---

# 042 — Cadastro de Profissional com convite opcional (Tipo de Contrato/Comissão/Email)

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BACKEND (Scheduling + Identity, composição no Bootstrap) + FRONTEND
**Depende de:** task 041 (que criou a tela de cadastro de Profissional sem estes campos).
**Critério de aceite:**
1. Cadastro de profissional ganha Tipo de Contrato (CLT/PJ/Autônomo, obrigatório), Comissão %
   padrão (opcional) e Email (opcional).
2. Com email, dispara convite Role.Dentista pro email informado — o profissional já existe como
   recurso mesmo antes do convite ser aceito.
3. Sem email, comportamento idêntico ao de antes (recurso puro, sem login).
4. Quando o convite é aceito, o usuário resultante é vinculado automaticamente ao profissional que
   estava esperando aquele email — sem passo manual.
5. `dotnet build`/`dotnet test`/`npm run build`/`npm run lint` limpos.

## Contexto

Pedido do usuário: cadastro de dentista com nome, tipo de contrato, comissão opcional e email —
"vai enviar um link pra esse email caso o dentista queira registrar, caso não o pessoal pode usar
o dentista na plataforma da mesma forma pra marcar consultas". Ou seja: o Profissional (recurso da
Agenda) sempre existe; o login é um extra opcional por cima dele.

## Execução

### Domain (Scheduling)

- `TipoContrato` enum novo (Clt/Pj/Autonomo).
- `Profissional` ganha `TipoContrato` (obrigatório), `PercentualComissaoDefault` (opcional — só
  informativo nesta rodada, `Billing` não lê automaticamente), `Email` (opcional, normalizado
  lowercase). Novo método `VincularUsuario(userId)` — idempotente, não sobrescreve um vínculo já
  existente (primeiro aceite vence).
- Migration `AddProfissionalEmailTipoContratoComissao` aplicada.

### Application (Scheduling)

- `CreateProfissionalCommand` estendido com os 3 campos novos.
- `LinkProfissionalUserCommand` novo — recebe (OrganizationId, Email, UserId), vincula TODOS os
  profissionais pendentes (sem UserId) com aquele email na organização. Sempre sucesso, mesmo sem
  nenhum profissional pendente (best-effort, não é requisito do aceite do convite).
- `IProfissionalRepository.ListPendentesPorEmailAcrossOrganizationsAsync` novo — `IgnoreQueryFilters`
  porque quem está aceitando o convite pode não ter esta organização como ativa no token ainda
  (mesmo raciocínio dos outros repositórios "AcrossOrganizations" do projeto).

### Composição (Bootstrap — decisão de arquitetura central desta task)

Em vez de um módulo (Scheduling ou Identity) chamar o `IMediator`/Application do outro por dentro
— o que quebraria a regra "comunicação só por `*.Contracts`" —, a orquestração dos dois passos
(criar profissional + criar convite; aceitar convite + vincular profissional) mora nos
CONTROLLERS (Bootstrap), que já é o composition root do monólito:

- `ProfissionaisController.Create`: chama `CreateProfissionalCommand` (Scheduling); se `Email`
  veio, chama `CreateInviteCommand` (Identity) na sequência. Convite é best-effort — se falhar
  (ex.: email já é membro), o profissional continua criado, o aviso vai no corpo da resposta
  (`CreateProfissionalResponse.ConviteAviso`), não vira 400.
- `InvitesController.Accept`: depois do `AcceptInviteCommand` (Identity) ter sucesso, chama
  `LinkProfissionalUserCommand` (Scheduling) com o email do convite (`AcceptInviteResultDto.Email`,
  campo novo) — best-effort, nunca falha o aceite por causa disso.

Nenhum Application layer passa a depender do outro módulo por causa desta task — só o Bootstrap
sequencia dois comandos independentes, que é exatamente o papel dele.

### Frontend

- `types/scheduling.ts`: `TipoContrato` novo, `Profissional` ganha os 3 campos.
- `features/scheduling/api.ts`: `createProfissional` manda os campos novos, devolve
  `CreateProfissionalResult` (profissional + `conviteEnviado` + `conviteAviso`).
- `SettingsPage.tsx` (`ResourcesSection`): formulário ganha Select de Tipo de Contrato, input de
  Comissão % opcional, input de Email opcional. Mensagem de feedback pós-criação diferencia
  "convite enviado" de "convite não enviado" (aviso, sem bloquear o cadastro). Lista de
  profissionais ganha badge de status: **Acesso ativo** (tem `userId`) / **Aguardando registro**
  (tem `email`, sem `userId`) / nada (recurso puro, sem email).

## Fora de escopo

- Comissão default não é lida automaticamente por `Billing` ao criar uma Fatura — continua manual
  por fatura, como já era. Campo é só informativo nesta rodada.
- Editar Tipo de Contrato/Comissão/Email depois de cadastrado — mesma limitação já herdada (task
  041): Profissional não tem tela de edição, só criação.
- Reenviar convite manualmente pela tela (se expirar, hoje só reenviando via `Convites` da
  organização, fora desta tela).

## Verificação

- `dotnet build OdontoPlatform.sln` — limpo.
- `dotnet test OdontoPlatform.sln` — 372 testes, 0 falhas (+8 novos: `CreateProfissionalCommandHandlerTests`
  ampliado, `LinkProfissionalUserCommandHandlerTests` novo, `ProfissionalTests` novo — domínio).
- `npm run build`/`npm run lint` — limpos.
- Validado AO VIVO via claude-in-chrome, fluxo completo ponta a ponta: Owner cadastra "Dr. Marcelo
  Vieira" (Endodontia, PJ, 40% comissão, com email) → feedback "convite enviado" → badge
  "Aguardando registro" → token pego do log (`LoggingInviteNotifier`, sem provider real ainda) →
  signup com o mesmo email → tela de onboarding já mostra o convite pendente → aceita colando o
  token → badge do profissional vira "Acesso ativo" automaticamente, sem passo manual.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Cadastro de dentista com convite opcional, recurso sempre existe independente do login |
| 2. Contexto | Reader → Writer | Padrão `Invite`/`RefreshToken` (task 041) e fronteira `*.Contracts` mapeados |
| 3. Quebra | Tech Lead | Domain (Scheduling) + orquestração no Bootstrap (não módulo-a-módulo) + frontend |
| 4. Estrutura | Architect | Composição nos controllers em vez de porta cross-module nova — evita acoplar Scheduling.Application↔Identity.Application nos dois sentidos só pra este fluxo |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Backend / Dev Frontend | Domain/Application/Infrastructure/Bootstrap + tela de cadastro + badges de status |
| 7. Teste | QA | `dotnet test` (372/372), `npm run build`/`lint`, validação end-to-end real via browser (cadastro → convite → aceite → link automático) |
| 8. Documentação | Writer | `docs/decisions.md`, `docs/knowledge/patterns.md`, `docs/knowledge/business-rules.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
