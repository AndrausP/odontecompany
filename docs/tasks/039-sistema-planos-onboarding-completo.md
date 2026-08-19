---
task: "039"
sprint: "11"
status: done
---

# 039 — Sistema de planos (bloqueio por assinatura) + onboarding completo + esqueleto Stripe

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BACKEND (módulo novo) + FRONTEND
**Depende de:** 036/037 (onboarding guiado Empresa→Unidade, sprint-11) — este task estende o
fluxo com um passo novo (Plano) entre os dois já existentes.
**Critério de aceite:**
1. Organização sem plano ativo não acessa o app (mesmo bloqueio que organização inexistente).
2. Onboarding vira 3 passos: Empresa → Plano → Unidade → app.
3. Criar unidade além do limite do plano ativo é bloqueado com mensagem clara.
4. Esqueleto de checkout Stripe existe (endpoint + porta + implementação) — não cobra ninguém,
   mas o contrato está pronto pra virar real trocando só uma classe.
5. Empresa (Organization) e Unidade (Branch) ganham campos extras opcionais, configuráveis
   depois.
6. `dotnet build`/`dotnet test` e `npm run build`/`npm run lint` limpos.

## Contexto

Pedido do usuário, mesma sessão da landing/onboarding: "da mais infos na criação da empresa e na
criação da filial, de opções pra configurar ela depois e bloqueie quem nao tem os planos faça o
sistema dos planos e conexao com o stripe ainda nao precisa funcionar mas deve ter o esqueleto
já". Grill-me resolveu 2 decisões: (1) "bloquear quem não tem plano" = organização escolhe um
plano no onboarding, sem cobrança real ainda — sem plano selecionado, bloqueia igual a sem
organização; (2) Plano/Assinatura mora em módulo novo `Subscriptions`, não dentro de `Billing`
(que é faturamento do PACIENTE, domínio irmão diferente).

## Execução

### Backend — módulo `Subscriptions` novo (Domain/Application/Infrastructure/Contracts)

- `PlanTier` (enum: Starter/Profissional/Rede) + `PlanCatalog` (fonte única de nome/limite de
  filial/preço — sem tabela/seed no banco, é conteúdo fixo do produto).
- `Subscription` (entidade, 1:1 com Organization via índice único) — `Tier`, `Status`
  (Ativa/Cancelada), `StripeCustomerId`/`StripeSubscriptionId` (null até o Stripe existir de
  verdade).
- `SelectPlanCommand` (upsert — primeira escolha cria, escolha seguinte troca o Tier, sem cobrar).
- `StartCheckoutCommand` + `IPaymentGatewayService` (porta ACL, mesmo padrão de
  `IConvenioAdapter`/`ManualConvenioAdapter` do Billing) — implementação de referência
  `StripePaymentGatewayService` é ESQUELETO: não chama Stripe.net (não instalado), só loga e
  devolve URL de placeholder.
- `ISubscriptionLookup` (Contracts) — porta cross-module: `ObterAtivaAsync`/`LimiteDeFiliaisAsync`,
  consumida por `Identity.Application` (GetMe) e `Tenancy.Application` (CreateBranch).
- `SubscriptionsController`: `GET /plans`, `GET /me`, `POST /` (escolher/trocar plano), `POST
  /checkout` (esqueleto).
- Migration `InitialCreate` (Subscriptions) — aplicada.

### Backend — limite de filial por plano

- `Tenancy.Application.CreateBranchCommandHandler` ganhou `ISubscriptionLookup` — conta branches
  ativas, compara com `LimiteDeFiliaisAsync` (0 se organização sem plano ativo), retorna
  `Branch.LimiteDoPlanoAtingido` (400) se estourar.

### Backend — campos extras (Organization/Branch)

- `Organization`: `Cnpj`/`Telefone`/`Endereco` opcionais (migration
  `AddOrganizationCnpjTelefoneEndereco`).
- `Branch`: `Telefone` opcional (migration `AddBranchTelefone`).
- Ambos "configurar depois" — só `Nome` continua obrigatório na criação.

### Backend — GetMe

- `MeResultDto.ActivePlanTier` (string?) — plano da organização ATIVA do token, resolvido via
  `ISubscriptionLookup`, null = sem plano (onboarding incompleto).

### Frontend

- `features/subscriptions/` novo: `api.ts` (listPlans/getMySubscription/selectPlan/startCheckout),
  `SelectPlanStep.tsx` (passo 2 do onboarding — 3 cards, sem checkout, ativa na hora).
- `OnboardingPage.tsx`: 3 passos (`'organizacao' | 'plano' | 'unidade'`). "Por enquanto não" só
  no passo 1. Resume pós-reload: organization existente pula direto pro passo `plano`; se a
  organization já existia ANTES desta visita (`hadOrganizationOnArrival`, travado num `ref` na
  entrada — ver docs/knowledge/errors-aprendidos.md pros 2 bugs achados e corrigidos aqui),
  escolher o plano vai direto pro app, sem forçar criar outra unidade.
- `RequireOrganization.tsx`: gate novo — organization sem `activePlanTier` também manda pro
  `/onboarding` (sem skip a partir daqui, só existe pular ANTES de criar organization).
- `CreateOrganizationForm.tsx`/`CreateBranchForm.tsx`: campos novos (Cnpj/Telefone/Endereco;
  Telefone), todos opcionais.

## Fora de escopo

- Checkout real (Stripe.net, webhook de confirmação de pagamento, cobrança recorrente) — esqueleto
  intencional, dívida técnica nomeada.
- Tela de configurações pra editar Cnpj/Telefone/Endereco depois de criado (hoje só entra na
  criação) — "configurar depois" é aspiracional, tela dedicada é item futuro.
- Downgrade de plano com validação de limite (organização com 3 filiais tentando trocar pro
  Starter, limite 1) — `SelectPlanCommand` troca o Tier sem checar filiais existentes; risco
  aceito nesta rodada (edge case raro, sem cobrança real ainda pra justificar a complexidade).

## Verificação

- `dotnet build OdontoPlatform.sln` — limpo.
- `dotnet test OdontoPlatform.sln` — 341 testes, 0 falhas (inclui teste novo de gate de plano em
  `CreateBranchCommandHandlerTests` e teste de `GetMeQueryHandler` ajustado pro `ISubscriptionLookup`
  novo).
- `npm run build`/`npm run lint` — limpos.
- Fluxo completo validado AO VIVO via claude-in-chrome (usuários reais criados via
  `/api/auth/signup`, sem mock): (1) usuário novo — Empresa→Plano→Unidade→`/agenda`; (2) usuário
  com organização mas sem plano (resume) — Plano→`/agenda` direto, sem forçar unidade; (3) limite
  de filial por plano confirmado via curl (2ª filial no Starter bloqueada com
  `Branch.LimiteDoPlanoAtingido`, 400); (4) esqueleto de checkout confirmado (URL de placeholder,
  nenhuma chamada externa).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Bloquear quem não tem plano; onboarding com mais dados; esqueleto Stripe |
| 2. Contexto | Reader → Writer | `Organization`/`Branch`/`BranchesController`/padrão `IConvenioAdapter` mapeados |
| 3. Quebra | Tech Lead | 1 task grande — módulo novo + 2 entidades estendidas + onboarding |
| 4. Estrutura | Architect | Módulo `Subscriptions` separado de `Billing`; `PlanTier` enum + `PlanCatalog` estático (sem tabela); `ISubscriptionLookup` cross-module via Contracts |
| 5. Aprovação | Product Owner | Aprovado (grill-me) |
| 6. Implementação | Dev Backend / Dev Frontend | Módulo Subscriptions completo, integração Tenancy/Identity, onboarding 3 passos, 2 bugs de state achados e corrigidos ao vivo |
| 7. Teste | QA | `dotnet test` (341/341), `npm run build`/`lint`, validação end-to-end real via browser + curl |
| 8. Documentação | Writer | `docs/knowledge/errors-aprendidos.md`, `patterns.md`, `business-rules.md`, `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
