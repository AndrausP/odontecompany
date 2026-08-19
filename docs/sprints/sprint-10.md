---
sprint: "10"
status: done
---

# Sprint 10

**Período:** 2026-08-19 → (aberto)
**Objetivo:** Backlog de bugs/dívidas herdadas de sprints anteriores — usuário pediu pra "corrigir
bugs e melhorar o produto, deixar redondo com o que já tem" depois de subir o repo pro GitHub
(https://github.com/AndrausP/odontecompany). Não é feature nova de escopo — é fechar débito já
nomeado em `docs/sprints/sprint-6.md`/`sprint-7.md` e achados de QA ainda `planned`.
**Aprovado por (PO):** Product Owner (decisão do usuário + broker) — 2026-08-19

## Tasks

| ID | Título | Status | Owner |
|----|--------|--------|-------|
| 020 | Reativar `OrganizationMembership` ao aceitar convite novo pra membership inativa | done | Dev Backend |
| 021 | Suíte de integração real (`WebApplicationFactory`) | done | Dev Backend |

<!-- Status possíveis: planned | in-progress | blocked | done -->

## Notas do Tech Lead

Sprint aberta pra ir fechando o backlog de dívidas já nomeadas nas retrospectivas anteriores, não
tudo de uma vez — cada task some da tabela quando fecha, sprint fica aberta até o backlog relevante
esvaziar ou o usuário redirecionar prioridade.

## Retrospectiva

2 tasks fechadas, backlog de sprint-6 zerado (só restava 020/021, ambas puxadas nesta sprint).
Suíte de testes cresceu de 322 pra 340 (18 testes novos: 1 regressão da 020 + 9 de integração da
021 — o número "18" inclui os 9 que já existiam em `OdontoPlatform.Api.UnitTests` e não tinham
sido contados na sprint-6 por causa do lock de build, revalidados aqui pela 1ª vez desde então).

Achado lateral relevante: bug de `JsonSerializerOptions` (case-sensitivity silenciosa) descoberto
DURANTE a implementação da 021, não era o objetivo da task — registrado em
`docs/knowledge/errors-aprendidos.md` porque é o tipo de erro que reaparece em qualquer client
HTTP C# customizado, não só nesta suíte.

Fricção de ambiente registrada, não é dívida do produto: `dotnet build`/`dotnet test` na raiz do
repo falha com `MSB3027` (arquivo travado) sempre que o backend está rodando via `dotnet run`
(Visual Studio ou terminal) ao mesmo tempo — o processo prende os `.dll` de cada módulo no
`bin/OdontoPlatform.Api`. Não tem fix de código; é preciso parar o processo rodando antes de
buildar a solução inteira (rodar só 1 projeto de teste isolado, sem o Api.UnitTests/
Api.IntegrationTests, não sofre disso).
