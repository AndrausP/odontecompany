---
sprint: "6"
status: done
---

# Sprint 6 — Pivot: plataforma self-serve multi-organização

**Período:** 2026-08-18 → a definir
**Objetivo:** Sair de "plataforma provisionada por seed/admin, 1 usuário = 1 tenant" para
"qualquer um cria conta, cria organização, e pode ser afiliado a várias organizações" — com o
vocabulário renomeado (`Tenant`→`Organization`, `Unidade`→`Branch`) em todo o stack.
**Aprovado por (PO):** pendente

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 010 | Rename `Tenant`→`Organization` (backend full) | done | Dev Backend |
| 011 | Rename `Unidade`→`Branch` | done | Dev Backend |
| 012 | Rename no frontend (claims, tipos, api clients) | done | Dev Frontend |
| 013 | `OrganizationMembership` N:N + papel `Owner` | done | Dev Backend |
| 014 | Auth multi-org: JWT com org ativa + troca sem login | done | Dev Backend |
| 015 | Signup público + criar organização (Owner) | done | Dev Backend |
| 016 | Convite de afiliação (modelo + criar/aceitar) | done (ressalva não-bloqueante — ver 020) | Dev Backend |
| 017 | `GET /api/me` — perfil escopado por membership | done | Dev Backend |
| 018 | Frontend: signup, org switcher, convites | done | Dev Frontend + Designer |
| 019 | QA: regressão de isolamento e RBAC multi-org | done | QA |

<!-- Status possíveis: planned | in-progress | blocked | done -->

## Ordem de execução (obrigatória)

```
010 ──┬── 012 ──────────────────────────────────┐
      │                                          │
      ├── 011 (destacável, cortável)             │
      │                                          │
      └── 013 ── 014 ──┬── 015 ─────────────────┤
                       │                         ├── 018 ── 019
                       ├── 016 ── 017 ───────────┘
                       │
                       └────────────────────────►
```

- **010 é a raiz.** Nada começa antes dela.
- **012 sai no mesmo deploy da 010** — o frontend quebra no instante em que a claim `tenant_id`
  vira `organization_id`. Não é "depois", é "junto".
- **013 → 014 é sequencial e indivisível na prática.** O modelo N:N sem a auth correspondente
  deixa o sistema sem login funcional. Não fechar a 013 sem a 014 na fila imediata.
- **011 é paralelizável** e não bloqueia o pivot.

## Veredito de viabilidade (Tech Lead)

```
Sprint 6 completa (010-019)
Veredito: ⚠️ viável com risco
Motivo: 22 dias estimados contra ~10 dias úteis de capacidade real (um júnior, meio período).
        O escopo está tecnicamente coerente e bem sequenciado, mas não cabe numa sprint padrão
        no papel.
Pra virar ✅: uma das três — (a) estender a sprint pra 4 semanas; (b) cortar 011 e empurrar
        016/017/018 pra sprint 7, entregando "rename + multi-org + signup" (010,012,013,014,015,
        019 ≈ 13 dias); (c) manter tudo, assumindo que a execução assistida por agentes deste
        repo tem entregue uma fase por sessão (sprints 1-5) — nesse caso a estimativa em
        dias-júnior é conservadora e a sprint fecha.
```

**Linha de corte pré-aprovada** (se estourar, corta nesta ordem, sem nova reunião):
1. **011** (rename `Unidade`→`Branch`) — não bloqueia nada do pivot.
2. **018** (frontend de signup/switcher) — mas cortar aqui entrega a sprint sem valor visível ao
   usuário; **isso é decisão do PO, não minha**.
3. **019 (QA) não é cortável.** Sprint que mexe no invariante de isolamento multi-tenant não
   fecha sem regressão de isolamento.

## Notas do Tech Lead

**Por que o rename ficou barato (e por que só agora):** as 7 migrations `InitialCreate`
(2026-08-18) **nunca foram aplicadas contra Postgres real** — está documentado na retro da sprint
5. Isso significa que o rename de schema é "apagar e regerar", não `ALTER TABLE RENAME COLUMN`
com backfill. Decisão tomada: **regerar as `InitialCreate`, não escrever migration de rename.**
Se algum ambiente já tiver aplicado o schema, é drop/recreate. Essa janela fecha no dia em que
houver Postgres com dado real — o rename tinha que ser agora ou custaria 3x.

**Decisões que tomei sem escalar** (ordem/camada é minha alçada):
- **Módulo `Tenancy` mantém o nome** (csproj, namespace, .sln). É o nome do conceito de
  isolamento, não da entidade. Renomear projeto no meio de um rename de 228 arquivos multiplica
  risco por zero ganho funcional. Débito nomeado, registrado.
- **`Organization` continua em `Identity.Domain`**, onde `Tenant` mora hoje. Mover agregado entre
  módulos não é parte de um rename.
- **Rename só no código; label visível em pt-BR não muda** ("Organização", "Unidade"). Trocar
  vocabulário de tela é retrabalho de Designer e reaprendizado do usuário — vira task própria se
  o PO quiser.
- **Signup e criar-organização são endpoints separados.** O fluxo de convite exige usuário sem
  organização; um endpoint só impediria isso.

**O que escalo pro PO** (não decido sozinho):
1. **Capacidade** — os três caminhos do veredito acima. Precisa de escolha antes de a cadeia
   liberar Dev.
2. **Sem verificação de email no signup** (task 015). Nesta fase é aceitável (não há envio de
   email nem cobrança), mas significa que qualquer um cria conta com email de terceiro. **Vira
   bloqueante antes de qualquer usuário real.** Débito nomeado, não escondido.
3. **Convite sem envio de email** (task 016, escopo travado pelo PO) — o token só existe no log
   do servidor. O fluxo é testável, mas inutilizável na prática até existir provider de email.
   Task futura obrigatória.

**Riscos técnicos principais desta sprint:**
- `User` perde `OrganizationId` (task 013) → deixa de cair no global query filter. Se o
  substituto (`OrganizationMembership` como entidade tenant-scoped) não for implementado
  corretamente, abre vazamento entre organizações. Maior risco da sprint.
- `GET /api/me/invites` precisa ler convites de organizações onde o usuário **ainda não entrou** —
  ou seja, furar o query filter global de propósito. Tem que ser um método único, nomeado e
  testado (padrão já existe: `GetByEmailAcrossTenantsAsync`), nunca `IgnoreQueryFilters()` solto.
- Token sem organização ativa (task 014) é superfície de autorização nova. Se a policy falhar
  aberta, endpoint de negócio aceita token sem escopo.
- Frontend: troca de org sem limpar o cache do React Query mostra dado da org anterior.

**Dívidas das sprints 1-5 continuam abertas** (worker de Outbox, convênio real, object storage
real, KMS real, migrations nunca aplicadas em Postgres real, RBAC de unidade só em Scheduling).
Nenhuma é tocada nesta sprint.

## Débito leve (achado do QA, não-bloqueante — 010/011/012)

Artefatos MSBuild stale em `obj/` (`src/Bootstrap/OdontoPlatform.Api/obj/Debug/net8.0/EndpointInfo/OdontoPlatform.Api.json`,
`ApiEndpoints.json`) ainda citam `CreateUnidadeRequest`/`unidadeId` — não é código-fonte, o
Swagger real em runtime já reflete `CreateBranchRequest`/`BranchId` via introspecção viva dos
controllers. `dotnet clean && dotnet build` regenera limpo. Risco só se algum cliente externo
consumir esses JSONs stale via swagger-codegen antes de um clean. Sugestão: rodar `dotnet clean`
antes do próximo push/deploy que envolva geração de client a partir do swagger.

## Backlog / pendências (achadas durante a sprint, sprint de destino em aberto)

| ID | Título | Status | Origem | Nota |
|----|--------|--------|--------|------|
| 020 | Reativar `OrganizationMembership` ao aceitar convite novo pra membership inativa | planned | QA, task 016 | **Bloqueante:** não implementar endpoint de desativar/remover membro antes desta task — ver `docs/tasks/020-membership-reativar-em-invite.md` |
| 021 | Bootstrap de suíte de integração com `WebApplicationFactory` | planned | QA, task 019 | Não-bloqueante: 5/13 casos obrigatórios de 019 cobertos só por inspeção estática — ver `docs/tasks/021-bootstrap-integration-tests.md` |

## Retrospectiva

**Entregue:** as 10 tasks da sprint (010-019) fechadas com QA passando e sem bug bloqueante
conhecido — sprint completa, sem corte. Pivot de plataforma provisionada por seed/admin ("1
usuário = 1 tenant") para self-serve multi-organização: rename `Tenant`→`Organization` e
`Unidade`→`Branch` em todo o stack (010, 011, 012), `OrganizationMembership` N:N com papel
`Owner` (013), auth multi-org com JWT de organização ativa e troca sem novo login (014), signup
público + criação de organização (015), convite de afiliação com criar/aceitar (016), `GET
/api/me` escopado por membership (017), frontend completo de signup/onboarding/org
switcher/convites (018), e regressão dedicada de isolamento e RBAC multi-org (019). Solução
completa: backend `dotnet build` 0 erro/0 aviso, `dotnet test` **293/293** (baseline 281 antes da
sprint + 12 testes novos da task 019); frontend `npm run build` limpo.

**Padrão de arquitetura consolidado:** duas extensões novas ao molde CQRS + Clean Architecture
das sprints 1-5, ambas documentadas em `docs/knowledge/patterns.md` — (1) "Afiliação N:N (entidade
tenant-scoped) substitui FK direta quando a cardinalidade multi-tenant por usuário existe"
(`User` vira global, `OrganizationMembership` nasce tenant-scoped e é quem cai no filtro global de
isolamento, não o agregado original); (2) "Policy nomeada com `RequireClaim`, fail-closed by
construction" pra distinguir endpoint que exige escopo ativo (organização) de endpoint que tolera
token "incompleto" (`/api/me`, criar organização, aceitar convite). Rename mecânico em massa (010,
011) confirmou o padrão de script multi-pass por casing + proteção de falso-cognato via
placeholder, já usado como referência por outros projetos do time.

**Bugs reais evitados/corrigidos na sprint:** o principal é o **IDOR do
`OrganizationsController`** (task 016, achado em auto-revisão do Dev Backend, confirmado pelo QA)
— `[Authorize(Roles = "Owner,Admin")]` sozinho só prova que o usuário é Owner/Admin de ALGUMA
organização, não da organização específica que aparece no `{id}` da rota; sem checagem explícita
`{id da rota} == organização do token`, um Owner da organização A conseguiria criar convite/ler
convites da organização B só trocando o guid na URL. Fix + padrão completo em
`docs/knowledge/patterns.md` ("IDOR fechado"). QA desta sprint (019) não encontrou nenhum bug de
produção novo — os 12 testes que escreveu (Records + Billing) preencheram um GAP de cobertura
(query filter global desses dois módulos nunca tinha teste dedicado pós-rename), não corrigiram
um bug ativo.

**Riscos técnicos conhecidos que ficam para depois (dívida documentada, não bugs):**
- **Sem suíte de integração real (`WebApplicationFactory`)** — 5 dos 13 casos obrigatórios do QA
  (019) foram cobertos só por inspeção estática de código (policy, rate limit, RBAC de
  controller), não por teste runtime, porque o projeto não tem `Microsoft.AspNetCore.Mvc.Testing`
  instalado. Recomendação do QA virou task de backlog: `docs/tasks/021-bootstrap-integration-tests.md`.
- **Sem verificação de email no signup** (task 015) — qualquer um cria conta com email de
  terceiro; aceitável nesta fase (sem cobrança, sem envio de email), mas bloqueante antes de
  qualquer usuário real em produção. Débito nomeado pelo Tech Lead desde a abertura da sprint.
- **Sem envio real de email de convite** (task 016) — `IInviteNotifier` é no-op (só loga o token
  em claro); o fluxo é testável mas inutilizável na prática até existir provider de email. Efeito
  colateral direto no frontend (018): "aceitar com 1 clique" não foi possível implementar com o
  contrato atual — usuário precisa colar o token manualmente.
- **Débito de FK cascade em `OrganizationMembership`** (task 013) — `OnDelete(Cascade)` da spec
  original do Architect não foi implementado; aceito porque hoje só existe soft-delete no código
  (nenhum hard-delete de `User`/`Organization` em produção). Precisa ser resolvido antes de
  qualquer feature de expurgo de dado/LGPD "direito ao esquecimento". Registrado em
  `docs/knowledge/errors-aprendidos.md`.
- **Bug latente de reativação de membership em accept** (achado pelo QA na task 016) —
  `AcceptInviteCommandHandler` trata "membership já existe" como idempotência sem checar se ela
  está `Ativo`; hoje inalcançável via API (nenhum endpoint desativa membro ainda), mas vira bug de
  produção instantâneo no dia em que um endpoint de desativar/remover membro for implementado.
  Task de follow-up já criada e travada com nota bloqueante: `docs/tasks/020-membership-reativar-em-invite.md`
  (status `planned`, sprint em aberto — **não pode ser pulada** antes de qualquer feature de
  remover membro).
- Dívidas herdadas das sprints 1-5 continuam abertas e intocadas nesta sprint: worker de Outbox,
  convênio real, object storage real, KMS real, migrations `InitialCreate` nunca aplicadas contra
  Postgres real, RBAC de unidade só em Scheduling.
