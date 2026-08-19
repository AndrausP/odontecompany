# Modules

> Maintained by Nirvana. Populated by the Writer as the chain works through each module.
> Run /reflect to synthesize from docs/ + graphify.

<!-- Each module entry follows this format:

## [Module Name]

**Status:** planned | in-progress | done
**Owner:** Dev Backend | Dev Frontend | Both
**Description:** [what this module does]
**Key files:** [main files]
**Notes:** [anything worth remembering]

-->

## Identity (Identity & Access)

**Status:** done
**Owner:** Dev Backend
**Description:** Autenticação (login/refresh JWT rotativo), RBAC (Admin/Dentista/Recepcao),
criação de usuário por admin dentro do próprio tenant, seed do primeiro tenant+admin. Sem
registro público.
**Key files:** `src/Modules/Identity/{Identity.Domain,Identity.Application,Identity.Infrastructure,Identity.Contracts}`
**Notes:** Email único globalmente (não por tenant). `ICurrentUserAccessor` (Identity.Contracts)
é consumido por todo módulo que precisa saber quem é o usuário autenticado. Task docs/tasks/002.

## Patients (Pacientes / CRM)

**Status:** done
**Owner:** Dev Backend
**Description:** CRUD de paciente com CPF (VO, mod-11), consentimento LGPD obrigatório, soft
delete, RBAC (Admin+Recepcao administram, Dentista só lê).
**Key files:** `src/Modules/Patients/{Patients.Domain,Patients.Application,Patients.Infrastructure,Patients.Contracts}`
**Notes:** CPF único por `(TenantId, Cpf)`, não global. `IPatientLookup` (Patients.Contracts) é
consumido por Scheduling e Records pra validar existência de paciente sem cross-module domain
reference. Task docs/tasks/003.

## Scheduling (Agenda)

**Status:** done
**Owner:** Dev Backend
**Description:** Agendamento com máquina de estados (Agendado→Confirmado→Concluido/Cancelado),
concorrência otimista (`xmin`), lock Redis curto + revalidação de query pra evitar sobreposição
de horário, cache de disponibilidade, Outbox pro `ConsultaConcluidaEvent`.
**Key files:** `src/Modules/Scheduling/{Scheduling.Domain,Scheduling.Application,Scheduling.Infrastructure,Scheduling.Contracts}`
**Notes:** Módulo de maior tráfego (doc de arquitetura). RBAC com ownership (Dentista só gerencia
a própria agenda, exceto em Create). Worker de publicação do Outbox ainda não implementado — fica
pra task 006 (Financeiro) definir o consumidor. Task docs/tasks/004.

## Records (Prontuário eletrônico)

**Status:** done
**Owner:** Broker (sem subagent Dev Backend/QA — limite de gasto atingido na sessão, ver docs/sprints/sprint-2.md)
**Description:** Prontuário 1:1 por paciente, odontograma (JSONB), evolução clínica imutável com
criptografia de campo em repouso (AES-256-GCM), anexos (metadado relacional + object storage
abstraído), trilha de auditoria append-only cobrindo toda leitura/escrita.
**Key files:** `src/Modules/Records/{Records.Domain,Records.Application,Records.Infrastructure,Records.Contracts}`
**Notes:** RBAC restrito a Admin+Dentista (Recepcao sem acesso a dado clínico). Visualização da
auditoria é Admin-only. `IEncryptionService`/`AesEncryptionService` e `IObjectStorageService`/
`LocalDiskObjectStorageService` (fallback dev) são ports-and-adapters — produção troca a chave
por KMS e o storage por S3/Blob sem tocar Application. Task docs/tasks/005.

## Billing (Financeiro — particular + convênios)

**Status:** done
**Owner:** Broker (sem subagent Dev Backend/QA — limite de gasto atingido, ver docs/sprints/sprint-3.md)
**Description:** Fatura particular (parcelamento até 12x, sem perda de centavo) ou de convênio
(ACL via `IConvenioAdapter`), status sempre derivado das parcelas, comissão de dentista, geração
idempotente de fatura a partir de consulta concluída (`ConsultaConcluidaEventHandler`, tradutor
pronto pra worker de fila que ainda não existe).
**Key files:** `src/Modules/Billing/{Billing.Domain,Billing.Application,Billing.Infrastructure,Billing.Contracts}`
**Notes:** RBAC Admin+Recepcao (Dentista sem acesso, mesmo raciocínio de Pacientes). Índice único
parcial em `AgendamentoId` garante idempotência real (não só check de Application). Convênio
usa adaptador de referência sem integração externa de verdade — troca isolada no DI quando
houver operadora real. Tasks docs/tasks/006 e docs/tasks/007.

## Reporting (Inteligência / BI)

**Status:** done
**Owner:** Broker (sem subagent Dev Backend/QA — limite de gasto atingido, ver docs/sprints/sprint-4.md)
**Description:** Dashboard e relatórios (financeiro, ocupação de agenda, pacientes ativos) por
composição EM TEMPO REAL de portas de leitura agregada expostas pelos módulos Patients/
Scheduling/Billing — sem projeção persistida nem read replica real (ambos indisponíveis nesta
fase, dívida documentada).
**Key files:** `src/Modules/Reporting/{Reporting.Application,Reporting.Contracts}` — só 2
projetos, sem Domain/Infrastructure (não há dado próprio nem invariante de negócio a proteger).
**Notes:** Único módulo com só Application+Contracts. Referencia `*.Contracts` de 3 módulos
diferentes (Patients+Scheduling+Billing) — correto, é o próprio propósito do módulo. RBAC
Admin-only. Task docs/tasks/008.

## Tenancy (Rede / Unidades)

**Status:** done
**Owner:** Broker (sem subagent — ver docs/sprints/sprint-5.md)
**Description:** Hierarquia `Tenant → Unidade` — segundo nível opcional do multi-tenant.
`Unidade` (clínica física dentro de uma rede), `IUnidadeLookup` (porta cross-module consumida
por Scheduling, Identity, Estoque).
**Key files:** `src/Modules/Tenancy/{Tenancy.Domain,Tenancy.Application,Tenancy.Infrastructure,Tenancy.Contracts}`
**Notes:** Formaliza o módulo transversal "Tenancy" que o doc de arquitetura sempre previu — até
a task 009, só existia o `ITenantContext`/filtro de query (task 001), sem agregado de negócio.
Admin-only. Task docs/tasks/009.

## Estoque (Materiais / Insumos)

**Status:** done
**Owner:** Broker (sem subagent — ver docs/sprints/sprint-5.md)
**Description:** Controle de estoque de materiais/insumos odontológicos — `ItemEstoque` com
entrada/saída (nunca fica negativo), `UnidadeId` opcional (estoque por unidade ou compartilhado
do tenant).
**Key files:** `src/Modules/Estoque/{Estoque.Domain,Estoque.Application,Estoque.Infrastructure,Estoque.Contracts}`
**Notes:** Módulo transversal citado no doc de arquitetura como "fase posterior". Admin+Recepcao
administram (operação do dia a dia). Task docs/tasks/009.

## Frontend (React + TypeScript)

**Status:** done
**Owner:** Broker (sem subagent — ver docs/decisions.md, 2026-08-18)
**Description:** SPA React 18 + TypeScript + Vite, conforme stack do doc de arquitetura (nunca
implementada até 2026-08-18 — só backend existia). Todas as 5 telas de negócio entregues: login
(JWT, refresh automático), **Agenda/Calendário** (FullCalendar, mês/semana/dia, criar/confirmar/
cancelar/concluir agendamento), **Pacientes** (CRUD com CPF mascarado/validado mod-11
client-side, consentimento LGPD, busca por nome, paginação, RBAC Admin+Recepcao administram/
Dentista só lê), **Financeiro** (faturas particular/convênio, registrar pagamento de parcela,
cancelar fatura, gestão de convênios inline — Admin-only), **Estoque** (lista com indicador de
estoque baixo, cadastro de item, registrar entrada/saída inline por linha, RBAC Admin+Recepcao),
**Relatórios** (date range picker + 3 cards: resumo geral/dashboard, faturamento, ocupação de
agenda — Admin-only, `GET /api/reports/{dashboard,faturamento,agenda}`).
**Key files:** `frontend/src/{features/{auth,scheduling,patients,billing,estoque,reports},components/{layout,ui},lib,types}`
**Notes:** Auth via JWT decodificado no client (`jwt-decode`, sem endpoint `/me`), refresh
automático em 401 (coalescido, uma rotação por vez), sessão persistida em `localStorage`. CORS
liberado no backend só pra `localhost:5173` (dev). `npm run dev` (frontend) + `dotnet run`
(backend) — precisa de Postgres real rodando pro login funcionar de verdade (testado em toda a
sessão só com JWT fake injetado no localStorage, sem backend ativo, pra validar renderização e
RBAC de navegação). Badges de status são componentes dedicados por feature
(`FaturaStatusBadge`, span inline no Estoque), não um componente `StatusBadge` genérico — o
existente só cobre `AgendamentoStatus`. `ComingSoonPage` ficou órfão no repo (sem rota
apontando pra ele) após Estoque/Relatórios saírem de placeholder — não removido, sem custo de
manter.

## Roadmap original completo

Todas as 5 fases do doc de arquitetura (`Arquitetura-Plataforma-Odontologica.pdf`) estão
entregues — Núcleo, Clínico, Financeiro, Inteligência, Rede — 8 módulos de domínio, 224 testes,
build limpo. Dívidas técnicas conhecidas (migrations reais, worker de Outbox, integrações
externas reais) documentadas em cada `docs/tasks/*.md` e nos retros de `docs/sprints/*.md` —
nenhuma bloqueia o funcionamento do sistema em dev, todas relevantes antes de produção real.
