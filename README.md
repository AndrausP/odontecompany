# OdontoPlatform

Plataforma de gestão para clínicas odontológicas — agenda, prontuário eletrônico, financeiro
(particular + convênios), estoque, relatórios e comissão de dentista. Multi-tenant desde o
dia zero (uma instância atende várias clínicas, isolamento lógico por `tenant_id`).

Monólito modular em Clean Architecture, backend .NET 8 + frontend React 18. Construído
incrementalmente com um fluxo de trabalho assistido por IA ([Nirvana](CLAUDE.md)) — decisões de
arquitetura, tarefas e histórico completo ficam documentados em [`docs/`](docs/).

## Stack

**Backend** — .NET 8 / C# 12, ASP.NET Core, EF Core 8, PostgreSQL 16, Redis, RabbitMQ +
MassTransit, MediatR (CQRS), FluentValidation, JWT + refresh token rotativo, Argon2id.

**Frontend** — React 18 + TypeScript, Vite, Tailwind CSS v4, TanStack Query, React Hook Form +
Zod, Zustand, FullCalendar, `lucide-react`.

**Testes** — NUnit + Moq (backend).

## Arquitetura

Monólito modular: cada módulo de domínio tem suas próprias camadas
(`Domain` / `Application` / `Infrastructure` / `Contracts`), hospedadas num único processo API
(`src/Bootstrap/OdontoPlatform.Api`). Módulos nunca se referenciam diretamente — só via
`*.Contracts` (interfaces de leitura) e domain events. Multi-tenant via *shared schema*
(`tenant_id` + EF Core global query filter).

```
src/
  Bootstrap/OdontoPlatform.Api/     # host único — DI, auth, middleware de tenant
  Modules/
    Identity/          # login, RBAC, refresh token
    Tenancy/            # organizations, unidades/filiais
    Patients/           # cadastro de paciente (CRM)
    Scheduling/          # agenda — máquina de estados, lock otimista + Redis
    Records/             # prontuário eletrônico — odontograma, evolução clínica cifrada
    Billing/             # financeiro — particular, convênios, comissão
    Reporting/           # dashboards compostos em tempo real
    Estoque/             # materiais/insumos
  Shared/
    Contracts.Abstractions/   # PagedResult, PageRequest, IModuleContract
    Application.Common/       # ValidationBehavior (MediatR pipeline)
frontend/
  src/
    features/           # 1 pasta por módulo de UI (scheduling, patients, billing, ...)
    components/ui/      # design system (Button, Card, Input, Select, Modal, StatusBadge, ...)
    lib/                 # api-client, auth-store, theme-store, query-client
tests/                  # NUnit + Moq, 1 projeto de teste por módulo
docs/                   # arquitetura, decisões, sprints, tasks, design system — fonte de verdade
```

Detalhes: [`docs/architecture.md`](docs/architecture.md) · [`docs/modules.md`](docs/modules.md) ·
histórico de decisões técnicas em [`docs/decisions.md`](docs/decisions.md).

## Status

Núcleo (Identity, Tenancy, Pacientes, Agenda) e módulos clínico/financeiro (Records, Billing,
Reporting, Estoque) implementados. Frontend cobre as telas principais (Agenda, Pacientes,
Financeiro, Estoque, Relatórios, Comprovante de pagamento) com design system próprio (tokens de
cor, dark/light mode). Roadmap completo e progresso por task: [`docs/sprints/`](docs/sprints/) e
[`docs/tasks/`](docs/tasks/).

Dívidas técnicas conhecidas (worker de Outbox real, integração de convênio real, object storage
S3/Blob, KMS, envio real de e-mail) estão nomeadas em cada sprint — ver
[`docs/sprints/`](docs/sprints/).

## Rodando localmente

### Pré-requisitos

- .NET SDK 8
- Node.js 20+
- PostgreSQL 16 e Redis rodando localmente (ou apontar as connection strings pra uma instância
  remota)

### Backend

```bash
# 1ª vez: copie o template e preencha sua senha local do Postgres
cp src/Bootstrap/OdontoPlatform.Api/appsettings.Development.json.example \
   src/Bootstrap/OdontoPlatform.Api/appsettings.Development.json

dotnet build OdontoPlatform.sln
dotnet run --project src/Bootstrap/OdontoPlatform.Api --urls http://localhost:5262
```

Swagger sobe em `http://localhost:5262/swagger`. O seed cria uma organization + admin padrão
(`admin@clinicademo.local` / `Admin@123456`, ver `Auth:DefaultAdminPassword` em
`appsettings.json`) — **troque antes de qualquer deploy real**.

### Frontend

```bash
cd frontend
npm install
npm run dev       # http://localhost:5173
npm run build     # tsc -b && vite build
npm run lint       # oxlint
```

`frontend/.env.development` já aponta `VITE_API_BASE_URL` pro backend local (`:5262`).

### Testes

```bash
dotnet test
```

## Documentação

Todo o histórico de decisões, tarefas e convenções do projeto vive em [`docs/`](docs/) — não é
boilerplate, é a fonte de verdade usada pelo próprio fluxo de desenvolvimento:

- [`docs/architecture.md`](docs/architecture.md) — stack, camadas, fluxo de request
- [`docs/modules.md`](docs/modules.md) — o que cada módulo faz, status, arquivos-chave
- [`docs/decisions.md`](docs/decisions.md) — por que cada decisão técnica foi tomada
- [`docs/design/design-system.md`](docs/design/design-system.md) — tokens de cor, tipografia,
  espaçamento (light/dark)
- [`docs/knowledge/`](docs/knowledge/) — padrões recorrentes e erros já corrigidos (pra não
  repetir)
- [`docs/sprints/`](docs/sprints/) e [`docs/tasks/`](docs/tasks/) — progresso sprint a sprint,
  task a task

## Licença

Projeto privado — sem licença de código aberto definida.
