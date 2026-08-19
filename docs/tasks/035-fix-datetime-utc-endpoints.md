---
task: "035"
sprint: "11"
status: done
---

# 035 — Fix: DateTime Kind=Unspecified em endpoints de create (achado validando endpoints)

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** BUGFIX (backend) + fix pequeno (frontend, dark mode)
**Depende de:** nenhuma
**Critério de aceite:**
1. `POST /api/patients` (e qualquer create com campo `DateTime` no body, em qualquer módulo) não
   500a mais por `Cannot write DateTime with Kind=Unspecified`.
2. Ícone do date picker nativo visível no tema dark.
3. `dotnet build` e `npm run build`/`npm run lint` limpos.

## Contexto

Usuário reportou "valide os endpoints, não to conseguindo criar nada". Investigação (browser real
+ DevTools Network, log do backend colado pelo usuário) achou stack trace real:
`Microsoft.EntityFrameworkCore.DbUpdateException` → `System.ArgumentException: Cannot write
DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone'` no
`CreatePatientCommandHandler` (`DataNascimento` vindo do JSON do request). Frontend só mostrava
"Erro inesperado. Tente novamente." — resposta 500 sem corpo `{ error }`, `getApiErrorMessage`
caindo no fallback genérico.

Em paralelo, usuário reportou ícone do calendário (input `type="date"`) invisível no tema dark.

## Execução

**Causa raiz (DateTime):** `System.Text.Json` nunca marca `Kind` de uma string de data sem offset
— todo `DateTime` de request body chega `Kind=Unspecified`. Npgsql 6+ recusa gravar isso em
`timestamp with time zone`. Risco não era só de `Patients` — qualquer módulo com campo de data no
body tinha o mesmo bug latente (ex.: `Scheduling.DataHoraInicio/Fim`).

**Correção:** `UtcDateTimeConverter`/`UtcNullableDateTimeConverter` novos em
`src/Shared/Infrastructure.Common/Persistence/UtcDateTimeConventionExtensions.cs`, registrados via
`ConfigureConventions(ModelConfigurationBuilder)` nos 7 `DbContext`s do monólito
(Patients/Scheduling/Billing/Estoque/Identity/Records/Tenancy) — normaliza TODO `DateTime`/
`DateTime?` do modelo pra `Kind=Utc` na escrita, sem tocar em nenhuma `IEntityTypeConfiguration`
individual nem em nenhum `CommandHandler`.

**Causa raiz (ícone dark):** `color-scheme` nunca foi declarado em `index.css` — browser sempre
assumia esquema claro pros controles nativos (date/time picker, scrollbar), então o SVG do
calendário (escuro) ficava invisível em cima do fundo escuro do tema dark.

**Correção:** `:root { color-scheme: light; } .dark { color-scheme: dark; }` em
`frontend/src/index.css`.

## Verificação

- `dotnet build src/Bootstrap/OdontoPlatform.Api` — limpo.
- Backend restartado (processo antigo travava as DLLs — matado e resubido).
- Migration EF Core (035 é backend-only nesse ponto) não se aplica aqui — mudança é convenção de
  mapeamento, não schema; nenhum `dotnet ef migrations add` necessário pra este fix específico.
- Frontend: `npm run build`/`npm run lint` limpos (verificados junto das tasks 036-038 nessa mesma
  sessão, mesmo commit lógico).
- Validado via claude-in-chrome: landing/onboarding testados end-to-end nas tasks seguintes sem
  reincidência do 500 original.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Endpoints de create funcionando de novo — usuário não conseguia criar nada |
| 2. Contexto | Reader → Writer | Stack trace real do backend colado pelo usuário; DevTools Network confirmou 500 puro |
| 3. Quebra | Tech Lead | 1 task — bugfix, sem grill-me (causa já identificada via evidência direta) |
| 4. Estrutura | Architect | Convenção EF Core global via `ConfigureConventions`, não fix pontual por handler |
| 5. Aprovação | Product Owner | Aprovado |
| 6. Implementação | Dev Backend / Dev Frontend | 7 DbContexts + `index.css` |
| 7. Teste | QA | `dotnet build` limpo; repro original (browser + DevTools) não reproduz mais |
| 8. Documentação | Writer | `docs/knowledge/errors-aprendidos.md`, `patterns.md`, `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.
