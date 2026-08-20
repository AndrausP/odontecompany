---
sprint: "11"
status: done
---

# Sprint 11

**Período:** 2026-08-19 → 2026-08-19
**Objetivo:** Continuação do polimento visual pedido pelo usuário ("foque agora no polimento" /
escopo "tudo") — regra de negócio travada com o usuário: zero cor hardcoded remanescente em
`frontend/src`, toda tela usando só os tokens do design system (024/025). Reaberta 3x same-day:
validação de endpoints de create (bugfix real encontrado), depois landing pública + onboarding
guiado (feature grande, grill-me próprio).
**Aprovado por (PO):** Product Owner (decisão do usuário + broker) — 2026-08-19

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 033 | Sweep de cor hardcoded — verificação completa (8 features) | done | QA |
| 034 | Code-splitting por rota (lazy loading das páginas) | done | Dev Frontend |
| 035 | Fix: DateTime Kind=Unspecified em endpoints de create | done | Broker |
| 036 | Backend: `User.OnboardingSkipped` + skip-onboarding | done | Dev Backend |
| 037 | Frontend: onboarding guiado (Empresa → Unidade) + skip | done | Dev Frontend |
| 038 | Landing page pública ("/") com pricing vitrine | done | Dev Frontend |
| 039 | Sistema de planos + onboarding completo + esqueleto Stripe | done | Broker |

<!-- Status possíveis: planned | in-progress | blocked | done -->

## Notas do Tech Lead

Escopo inicial (grill-me com usuário) era varredura + correção. Reader mapeou antes de abrir task
de implementação: `frontend/src/**/*.{tsx,ts,css}` já está 100% migrado pros tokens desde a
024/025 (sprint-7) — as 93 ocorrências mapeadas em `docs/design/design-system.md` §6 já foram
todas resolvidas em sprints anteriores, nenhuma reintroduzida nas sprints 8-9 (Agenda redesign,
dark reskin). Task 033 virou verificação, não implementação — sem handoff pro Dev Frontend.

## Retrospectiva

Sweep automatizado (`grep -rnE '\b(bg|text|border|divide|outline|ring)-(slate|red|amber|emerald|
blue|gray|zinc|neutral|green|yellow|orange)-\d{2,3}\b'` sobre `.tsx`/`.ts`/`.css`) — **zero
matches** em todo `frontend/src`. `npm run lint` (oxlint) e `npm run build` limpos. Design system
confirmado 100% aderente nas 8 features (auth, billing, estoque, patients, reports, scheduling,
organizations, invites) + `ui/`/`AppLayout`.

Achado lateral fora de escopo desta task (registrado, não corrigido): `vite build` avisa chunk
`index-*.js` de 792.90 kB (>500 kB) — nenhum `dynamic import()`/code-splitting configurado. Não é
cor/token, é performance de bundle; vira item de backlog se o usuário quiser puxar.

Usuário pediu pra puxar o achado lateral na sequência (task 034) — sprint reaberta same-day pra
fechar o item já nomeado acima em vez de abrir sprint nova pra 1 task de continuação direta.

`App.tsx`: as 10 páginas de rota (`LoginPage`, `SignupPage`, `OnboardingPage`, `InvitesPage`,
`AgendaPage`, `PatientsPage`, `FinanceiroPage`, `EstoquePage`, `ReportsPage`, `ComprovantePage`)
viraram `React.lazy()`, `<Routes>` envolto em `<Suspense fallback={<RouteFallback />}>`. Shell
(`AppLayout`/`ProtectedRoute`/`RequireOrganization`) continua eager — nav aparece antes da página
carregar, evita layout shift. `RouteFallback` usa o mesmo idioma de loading já usado dentro das
páginas (`text-sm text-ink-muted`), sem componente novo.

`npm run build` depois: bundle principal caiu de 792.90 kB → 250.79 kB (chunk `index-*.js`); maior
chunk agora é `AgendaPage-*.js` (285.02 kB, FullCalendar) — nenhum chunk passa dos 500 kB, aviso
de bundle size sumiu. `npm run lint` limpo.

## Reabertura 2 (035) — usuário reportou "não consigo criar nada"

Investigação real (browser + DevTools Network + log do backend colado pelo usuário) achou 500 em
`POST /api/patients`: `DbUpdateException` → `Cannot write DateTime with Kind=Unspecified to
PostgreSQL type 'timestamp with time zone'`. Causa raiz e correção completas em
docs/tasks/035-fix-datetime-utc-endpoints.md e docs/knowledge/errors-aprendidos.md — convenção EF
Core global (`ConfigureConventions`) nos 7 `DbContext`s, não fix pontual. Aproveitado pra corrigir
achado lateral relatado junto: ícone do date picker invisível no dark (`color-scheme` ausente).

## Reabertura 3 (036-038) — landing pública + onboarding guiado

Usuário pediu 2 mudanças grandes na mesma mensagem: landing pública antes do login (nav + hero +
pricing) e fluxo pós-login perguntando se quer criar empresa agora ou depois, com criação guiada
Empresa→Unidade. Grill-me (`/bigtask`) resolveu 3 decisões em aberto: (1) "franquia"/"filiado" são
a hierarquia Organization/Branch já existente, sem camada nova; (2) pular a criação de empresa
persiste no backend, não expira por sessão; (3) pricing é vitrine estática, sem checkout real
nessa rodada. Detalhes por task em docs/tasks/036, 037, 038; decisões consolidadas em
docs/decisions.md, regras em docs/knowledge/business-rules.md.

Verificação: `dotnet build` (backend) e `npm run build`/`npm run lint` (frontend) limpos em todas
as 4 tasks desta reabertura; landing e onboarding validados ao vivo via claude-in-chrome
(navegação real no browser, sem mock).

## Reabertura 4 (039) — planos + onboarding completo + esqueleto Stripe

Usuário pediu, na sequência: mais dados na criação de empresa/unidade (configuráveis depois),
bloquear quem não tem plano, e esqueleto de conexão com Stripe. Grill-me: módulo novo
`Subscriptions` (não dentro de `Billing`, que é faturamento do paciente); "bloquear" = organização
escolhe um plano no onboarding (sem cobrança real ainda), sem plano = mesmo bloqueio de sem
organização. Onboarding virou 3 passos (Empresa→Plano→Unidade). 2 bugs de state real encontrados
e corrigidos testando ao vivo — detalhes em docs/knowledge/errors-aprendidos.md e
docs/tasks/039-sistema-planos-onboarding-completo.md. `dotnet test` completo: 341/341 passando.

## Reabertura 5 (040) — tela de configurações + módulos + paleta verde+preto

Usuário pediu a tela de configurações que a 039 tinha deixado como "fora de escopo... item
futuro" (editar Empresa/Unidade depois de criados) + plano ativo/trocar plano no mesmo lugar,
"melhorar os módulos" (cards de plano + itens de nav) e trocar a paleta dark de teal-ciano/roxo/
azul pra verde+preto puro (light já era verde+branco). Grill-me (3 perguntas) fechou as 3
ambiguidades antes de implementar. Detalhes em docs/tasks/040-tela-configuracoes-paleta-verde-preto.md.
`dotnet test` completo: 349/349 passando (+8 novos).

## Reabertura 6 (040, 2 fixes de IDOR) — `PUT`/`DELETE /api/branches/{id}` fecham gap de IDOR

Usuário pediu, em duas mensagens seguidas, pra fechar o gap de IDOR documentado como risco aceito
na 040: primeiro `PUT` (edição), depois `DELETE` (desativação) — mesmo tratamento nos dois
(`OrganizationId` do token, nunca da rota; `Branch.NaoEncontrada` idêntico pra branch inexistente
OU de outra organização). `dotnet test`: 353/353 (+3 testes).

## Reabertura 7 (041) — auditoria pré-venda: 8 achados fechados

Sessão pediu validação de "prontidão pra vender" — achado como Artifact (2 bloqueadores: sem
cadastro de Profissional/Sala trava a Agenda inteira; nenhum plano cobra de verdade). Usuário pediu
"separa as sprints e executa todas". Decisão de negócio antes de começar: cobrança manual por fora
(sem chave Stripe disponível), viraria integração real numa rodada futura. 8 itens fechados —
detalhes completos em docs/tasks/041-remediacao-auditoria-pre-venda.md. Achado lateral relevante:
o gate de limite de usuário por plano quebrou 3 testes de integração que criavam organization sem
nunca escolher plano — corrigido na fixture de teste (`AuthFlow.SelectPlanAsync`), não no gate
(comportamento correto, teste que não refletia o funil real). `dotnet test` completo: 364/364
passando (módulo `Subscriptions.UnitTests` criado do zero).

## Reabertura 8 (042) — cadastro de dentista com convite opcional

Usuário pediu: cadastro de profissional com nome/sobrenome, tipo de contrato, comissão opcional e
email — se informar email, manda convite pro dentista se auto-registrar; sem email, o dentista
continua usável na Agenda como recurso puro. Decisão de arquitetura central: orquestração dos dois
módulos (Scheduling cria o profissional, Identity cria/aceita o convite) mora nos CONTROLLERS
(Bootstrap), não em um módulo chamando o `IMediator` do outro — preserva a regra "comunicação só
por `*.Contracts`" sem inventar uma porta cross-module só pra isso. Ao aceitar o convite, o usuário
é vinculado automaticamente de volta ao profissional que esperava aquele email. Detalhes em
docs/tasks/042-cadastro-dentista-com-convite.md. `dotnet test` completo: 372/372 passando.
Validado ao vivo, fluxo completo: cadastro→convite→signup→aceite→link automático confirmado no
browser.

## Reabertura 9 (043) — editar Profissional e Sala

Usuário pediu pra continuar trabalhando em melhorias/novas features. Achado ao revisar o que
ficou pra trás nas tasks 041/042: cadastro de Profissional/Sala era create-only, sem conserto de
digitação pela UI. `AtualizarDados` novo no domínio dos dois, endpoints `PUT`, modais de edição na
tela de Configurações (mesmo padrão de `EditBranchModal`). Detalhes em
docs/tasks/043-editar-profissional-sala.md. `dotnet test`: 380/380. Validado ao vivo (corrigiu um
cadastro legado de tipo de contrato inválido pela própria tela).

## Reabertura 10 (044) — catálogo de Procedimentos

Feature nova (continuação da mesma diretriz de inovar). Agenda não tinha padronização de
serviço/valor/duração — cada agendamento partia do zero. `Procedimento` novo agregado (Scheduling)
com `Nome`/`ValorPadrao`/`DuracaoPadraoMinutos` opcionais; `Agendamento.ProcedimentoId` novo
(nullable, trailing param); `CreateAgendamentoCommandHandler` valida referência igual já fazia com
paciente/profissional/sala. CRUD completo + card na tela de Configurações + select no modal de
"Novo agendamento" (auto-preenche hora de fim a partir da duração padrão). Detalhes em
docs/tasks/044-catalogo-procedimentos.md. `dotnet test`: 393/393. Validado ao vivo (criei/editei
procedimento, confirmei que aparece no select do agendamento com valor formatado).
