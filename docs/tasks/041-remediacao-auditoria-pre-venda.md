---
task: "041"
sprint: "11"
status: done
---

# 041 — Remediação da auditoria pré-venda (8 itens: 2 bloqueadores, 4 alto risco, 2 polimento)

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BACKEND + FRONTEND (multi-módulo: Scheduling, Identity, Subscriptions, Tenancy)
**Depende de:** auditoria pré-venda (sessão anterior, artifact "Auditoria Pré-Venda") — este task
fecha os 8 achados, do mais crítico ao mais cosmético.
**Critério de aceite:** cada achado da auditoria tem um fix funcionando e testado; `dotnet build`/
`dotnet test`/`npm run build`/`npm run lint` limpos; validação ao vivo via browser dos fluxos
críticos (cadastro de recurso → agenda, esqueci senha → redefinir → login).

## Contexto

Auditoria anterior (mesma sessão) encontrou 2 bloqueadores reais pra vender o produto, 4 riscos
altos e 2 itens de polimento. Usuário pediu pra separar em sprint e executar tudo. Uma decisão de
negócio foi necessária antes de começar (cobrança): sem chave real do Stripe disponível, usuário
escolheu "cobrança manual por fora" em vez de integrar o Stripe de verdade — isso virou um aviso
explícito na UI (`PlanCards`) em vez de bloqueio de funcionalidade.

## Execução

### 1. Cadastro de Profissionais e Salas (bloqueador — Scheduling)

Endpoints `POST /api/profissionais`/`POST /api/salas` já existiam, sem UI nenhuma. `ResourcesSection`
nova dentro de `SettingsPage.tsx` (padrão inline de `ConveniosCard`, sem edição/remoção — backend
também não expõe isso ainda pra estes dois recursos). `features/scheduling/api.ts` ganhou
`createProfissional`/`createSala`.

### 2. Aviso de cobrança manual (bloqueador — Subscriptions, decisão do usuário)

`PlanCards.tsx` ganhou um aviso fixo acima do grid: "escolher um plano libera o acesso na hora — a
cobrança ainda é combinada diretamente com nossa equipe". Aparece no onboarding E na tela de
configurações (componente compartilhado). Sem mudança de backend — o esqueleto do Stripe continua
esqueleto, dívida nomeada desde a task 039.

### 3. Recuperação de senha (alto risco — Identity, módulo novo dentro do módulo)

Não existia NADA disso. Espelha o padrão de `Invite` ponta a ponta: `PasswordResetToken` (entidade
global, sem `IMustHaveOrganization` — mesmo raciocínio de `User`), `IPasswordResetTokenGenerator`/
`Sha256PasswordResetTokenGenerator`, `IPasswordResetNotifier`/`LoggingPasswordResetNotifier`
(esqueleto — mesma dívida de `LoggingInviteNotifier`, provider de email real ainda não existe).
`ForgotPasswordCommand`/`ResetPasswordCommand` (+handlers, validators). Erro único
`PasswordReset.TokenInvalido` cobre não-encontrado/expirado/já-usado (anti-enumeração, mesmo padrão
de `Invite.NaoEncontrado`). `ResetPasswordCommandHandler` revoga todas as sessões ativas do usuário
ao concluir. `AuthController`: `POST /api/auth/forgot-password` (rate limit próprio, mesma política
do signup) e `POST /api/auth/reset-password`. Frontend: `ForgotPasswordPage`/`ResetPasswordPage`,
rotas `/esqueci-senha`/`/redefinir-senha`, link "Esqueci minha senha" no `LoginPage`. Migration
`AddPasswordResetTokens` aplicada.

### 4. Comprovante usa o nome da clínica (alto risco — trivial)

`ComprovantePage.tsx:266` trocou `<CardTitle>OdontoPlatform</CardTitle>` fixo por
`{organizationName ?? 'OdontoPlatform'}` — o dado já existia na mesma tela (linha logo abaixo).

### 5. Termos de Uso + Política de Privacidade (alto risco — conteúdo)

`features/marketing/LegalPages.tsx` novo (`TermsPage`/`PrivacyPage`), rotas públicas `/termos` e
`/privacidade`. Linkado no rodapé da landing e no cadastro (`SignupPage`, texto de consentimento
abaixo do botão). Conteúdo redigido pelo time — rodapé de cada página avisa que não substitui
parecer jurídico, revisar com advogado antes de cobrar de verdade.

### 6. Limite de usuário por plano (alto risco — Subscriptions + Identity + Tenancy)

Landing prometia "Até 3 usuários"/"Usuários ilimitados" sem nenhuma checagem real. `PlanCatalog`
ganhou `LimiteUsuarios` (Starter=3, Profissional/Rede=ilimitado). `ISubscriptionLookup.LimiteDeUsuariosAsync`
novo (mesmo padrão de `LimiteDeFiliaisAsync`). Checado em `AcceptInviteCommandHandler` (Identity) —
não em `CreateInvite`, porque a membership só é criada de fato no ACCEPT (convite pendente não
ocupa vaga; checar na criação do convite não pegaria N convites pendentes todos aceitos depois).
`IOrganizationMembershipRepository.CountActiveByOrganizationAcrossOrganizationsAsync` novo. Erro
`Membership.LimiteDoPlanoAtingido`.

**Achado durante a implementação:** os testes de integração (`Api.IntegrationTests`) criam
organization sem nunca escolher plano — com o gate novo, toda organization sem plano tem limite 0,
então NENHUM convite conseguia mais ser aceito nesses testes (3 quebraram). Corrigido adicionando
`AuthFlow.SelectPlanAsync` e chamando antes de qualquer fluxo de convite nos 2 arquivos de teste
afetados — não uma falha no gate, um gap real na fixture de teste (organizations de teste nunca
passavam pelo funil completo). Ver docs/knowledge/errors-aprendidos.md.

### 7. Downgrade de plano valida limite de filial (polimento — Subscriptions + Tenancy)

Dívida nomeada desde a task 039 (`docs/decisions.md`), fechada aqui. `IBranchLookup.CountAtivasAsync`
novo (Tenancy.Contracts). `SelectPlanCommandHandler`: no caminho de TROCA de plano (subscription já
existe), confere filiais ativas contra o limite do tier novo antes de trocar — bloqueia com
`Subscription.DowngradeExcedeFiliaisAtivas` se estourar. Não roda na primeira escolha (onboarding —
organização recém-criada não tem filial nenhuma ainda). `Subscriptions.UnitTests` criado do zero
(módulo não tinha nenhum teste de handler até aqui).

### 8. Estados vazios com CTA (polimento — frontend)

`AgendaPage`: banner acima do calendário quando a organização não tem NENHUM agendamento ainda,
apontando pra Pacientes e Configurações → Profissionais (some assim que o primeiro agendamento
existe). `ReportsPage`: banner acima do "Resumo geral" quando pacientes ativos E agendamentos no
período são ambos zero. Estoque não mudou — já tinha mensagem clara ao lado do botão "+ Novo item".

## Fora de escopo

- Stripe real (decisão do usuário: cobrança manual por fora nesta rodada).
- Provider de email real (mesma dívida nomeada em `LoggingInviteNotifier`/`LoggingPasswordResetNotifier`
  — trocar as duas implementações juntas quando existir).
- Editar/remover Profissional e Sala (backend não expõe ainda — mesma limitação herdada).

## Verificação

- `dotnet build OdontoPlatform.sln` — limpo.
- `dotnet test OdontoPlatform.sln` — 364 testes, 0 falhas (projeto novo `Subscriptions.UnitTests`
  criado; +21 testes novos no total entre os módulos tocados).
- `npm run build`/`npm run lint` — limpos.
- Validado AO VIVO via claude-in-chrome, viewport desktop real (sidebar completa, não só mobile
  desta vez): signup → onboarding (3 passos, aviso de cobrança visível) → Configurações → cadastro
  de Profissional → confirmado no select de "Novo agendamento" da Agenda (fecha o bloqueador #1
  ponta a ponta) → esqueci senha → token pego do log → redefinir → login com a senha nova
  funcionando → sessões antigas revogadas.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Fechar os 8 achados da auditoria; decisão de cobrança manual (usuário, sem chave Stripe) |
| 2. Contexto | Reader → Writer | Artifact da auditoria + padrões existentes (`Invite`, `CreateBranch` limite) mapeados |
| 3. Quebra | Tech Lead | 8 itens, ordem bloqueador→alto→polimento |
| 4. Estrutura | Architect | `PasswordResetToken` espelha `Invite`; limite de usuário espelha limite de filial; downgrade usa a MESMA direção de dependência cross-module (Contracts↔Contracts, nunca Domain/Infrastructure) |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Backend / Dev Frontend | 8 itens implementados, migration aplicada |
| 7. Teste | QA | `dotnet test` (364/364, achou e corrigiu regressão real nos testes de integração), `npm run build`/`lint`, validação end-to-end real via browser |
| 8. Documentação | Writer | `docs/knowledge/errors-aprendidos.md`, `patterns.md`, `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
