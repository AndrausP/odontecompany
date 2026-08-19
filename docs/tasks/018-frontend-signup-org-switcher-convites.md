---
task: "018"
sprint: "6"
status: done
---

# 018 — Frontend: signup, criar organização, seletor de org ativa, convites

**Sprint:** docs/sprints/sprint-6.md
**Critério de aceite:** Do zero até dentro da agenda sem nenhum passo manual: criar conta → criar
organização → usar o sistema. Usuário com 2 organizações troca entre elas pelo cabeçalho **sem
refazer login**, e a tela recarrega com os dados da nova organização.

## Escopo técnico (Tech Lead)

1. **`/signup`** — página pública (nome, email, senha, confirmação). Rota fora do
   `ProtectedRoute`. Link cruzado com `/login`.
2. **Onboarding pós-signup** — usuário sem organização e sem convite cai numa tela única:
   "criar organização". Com convite pendente: lista de convites + botão aceitar, e a opção de
   criar org mesmo assim.
3. **Seletor de organização ativa** no `AppLayout` (cabeçalho). Chama
   `POST /api/auth/switch-organization`, troca o token no `auth-store`, e **invalida todo o cache
   do React Query** (`queryClient.clear()`) — senão a tela mostra dado da org anterior. Esse ponto
   é o bug mais provável desta task.
4. **Tela/menu de convites** — lista `pendingInvites` do `/api/me`, aceitar convite, feedback de
   expirado.
5. `auth-store` passa a guardar `activeOrganizationId` + lista de organizações vinda do `/api/me`,
   não só o que dá pra decodificar do JWT.

## Decisão pendente do Designer

- Onde vive o seletor de organização (dropdown no header vs menu de perfil) e o visual da tela de
  onboarding. **Não bloqueia o Dev Frontend** — pode começar pelos componentes `ui/` existentes
  (`Card`, `Modal`, `Select`, `Button`) e ajustar depois.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Self-serve fim-a-fim + troca de org sem novo login. |
| 2. Contexto | Reader → Writer | Frontend React 18 + TS + Tailwind + React Query; `ProtectedRoute`/`AppLayout`/`auth-store` já existem. |
| 3. Quebra | Tech Lead | Esta task. |
| 4. Estrutura | Architect / Designer | [pendente] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Frontend + Designer | [pendente] |
| 7. Teste | QA | [pendente] |
| 8. Documentação | Writer | [pendente] |

## Metadados

- **Estimativa:** 3 dias
- **Depende de:** 012, 014, 015, 016, 017 — é a última da cadeia funcional
- **Bloqueia:** nada
- **Risco:** ⚠️ cache do React Query não invalidado na troca de org = usuário vê paciente da
  outra clínica na tela. Não é vazamento de servidor, mas o usuário não sabe a diferença. QA
  testa explicitamente. **Segunda candidata a corte** se a sprint estourar (depois da 011) — mas
  cortar aqui significa entregar a sprint sem valor visível pro usuário final; nesse caso é
  decisão do PO, não minha.

## Status

planned → in-progress → in-review (QA) → **done**

Build TS (`npm run build` / `tsc --noEmit`) **não foi executado** por este agente (sem Bash
disponível) — QA/broker precisa rodar antes de aprovar.

### QA sign-off (Jubileu)

- Build frontend `npm run build` (tsc -b + vite build): 0 erros (confirmado pelo broker).
- Backend `dotnet build` 0/0 e `dotnet test` 293/293 (baseline 281 + 12 novos da task 019).
- Revisão de lógica React/TS: **PASS**, sem bug bloqueador.

Checklist revisada:
- [x] `OrgSwitcher.onSuccess` ordena `setSession(newTokens)` → `queryClient.clear()`. Como o
      `apiClient` lê `useAuthStore.getState().accessToken` dinamicamente no interceptor
      (`api-client.ts:14`), refetch pós-clear já usa token novo. Sem janela de token velho +
      cache limpo (nem o oposto).
- [x] `RequireOrganization` redireciona só quando `organizations.length === 0`
      (`RequireOrganization.tsx:39`). Convite pendente vs sem convite não muda a rota — mas
      `OnboardingPage` mostra `InvitesList` acima do `CreateOrganizationForm` quando
      `pendingInvites.length > 0` (`OnboardingPage.tsx:54-79`), atendendo o requisito de
      "convite tem prioridade visual".
- [x] `/signup` sem `ProtectedRoute` nem `RequireOrganization` (`App.tsx:19`). `/onboarding`
      dentro de `ProtectedRoute` mas fora de `RequireOrganization` (`App.tsx:23-30`) — sem
      loop de redirecionamento para usuário recém-criado sem org.
- [x] `ProtectedRoute` trata `claims.role` opcional com short-circuit `!claims?.role`
      (`ProtectedRoute.tsx:22`) — não passa `undefined` pra `Array.includes`.
- [x] `AppLayout.visibleItems` filtra com `!item.roles || (claims?.role && item.roles.includes(claims.role))`
      (`AppLayout.tsx:30`) — usuário sem role não vê itens role-gated (Financeiro/Estoque/
      Relatórios). Não quebra.
- [x] `InvitesList` valida token vazio via zod (`InvitesList.tsx:14-16`) e mostra
      `getApiErrorMessage(mutation.error)` em falha (`InvitesList.tsx:101`), que extrai
      `{ error: string }` do backend (404 anti-enumeração) ou cai pra "Erro inesperado.
      Tente novamente." (`query-client.ts:15-20`). Nenhum crash silencioso.
- [x] `OnboardingPage` faz early-`Navigate` pra `/agenda` quando `data.organizations.length > 0`
      (`OnboardingPage.tsx:45-47`) — cobre o caso de usuário aceitar convite em outra aba/voltar
      manualmente pra URL.
- [x] Cadeia signup → onboarding → criar org → agenda: sem redirect em loop. Signup faz
      `navigate('/onboarding')` (`SignupPage.tsx:49`); OnboardingPage cria org via
      `CreateOrganizationForm` que faz `setSession` + `queryClient.clear()` + `navigate('/agenda')`
      (`CreateOrganizationForm.tsx:42-44`); agenda passa por `RequireOrganization` (agora com
      organizations não-vazio) e renderiza.
- [x] Aceite de convite no onboarding: `acceptInvite` NÃO emite token novo, então
      `OnboardingPage.switchMutation` chama `switchOrganization(organizationId)` em seguida
      pra trocar de fato (`OnboardingPage.tsx:26-33`). Documentado no comentário do próprio
      componente. Correto.

Riscos residuais (não bloqueadores, ficam pra sprint futura):
- `RequireOrganization` em `isError && !data` retorna `null` (tela em branco) sem redirecionar.
  Comentário no código assume que erro será 401 e o interceptor limpa a sessão — mas 500/timeout
  de rede deixaria a UI em branco até re-fetch. Piscada de UX, não bug funcional.
- `queryClient.clear()` no OrgSwitcher não cancela requests in-flight com token antigo — se
  algum resolver DEPOIS do clear e ANTES do refetch novo, escreve no cache. Padrão recomendado
  pela tanstack pra troca de contexto; caso raro na prática.
- `PendingInviteDto` sem token em claro exige colar o token manualmente na modal de aceitar —
  débito já documentado pelo Dev Frontend, decisão de produto pra próxima sprint.

## Notas de implementação (Dev Frontend)

Arquivos novos:
- `frontend/src/features/auth/useMe.ts` — hook `useMe()` (React Query, queryKey `['me']`, `enabled: isAuthenticated`).
- `frontend/src/components/layout/RequireOrganization.tsx` — gate: `organizations.length === 0` → `/onboarding`; senão sincroniza `auth-store.setMe` e renderiza `children`.
- `frontend/src/features/organizations/CreateOrganizationForm.tsx`, `OnboardingPage.tsx`, `OrgSwitcher.tsx`.
- `frontend/src/features/invites/InvitesList.tsx`, `InvitesPage.tsx`.

Arquivos editados:
- `frontend/src/App.tsx` — rota `/signup` pública; `/onboarding` protegida (sem `RequireOrganization`, de propósito); bloco protegido principal agora é `ProtectedRoute > RequireOrganization > AppLayout`; nova rota `/convites`.
- `frontend/src/components/layout/AppLayout.tsx` — `OrgSwitcher` no topo da sidebar (via `useMe()`, mesma queryKey do `RequireOrganization`, sem request duplicada); item de nav "Convites" com badge de `pendingInvites.length`; filtro de `visibleItems` agora tolera `claims.role` ausente.
- `frontend/src/components/layout/ProtectedRoute.tsx` — mesma correção de `claims.role` opcional.

### Decisão que tomei sem checar com Architect (flag pro QA/Tech Lead)

`PendingInviteDto` (`GET /api/me/invites` e embutido em `GET /api/me`) **não contém o token em
claro** do convite — só existe hash no banco (`Identity.Domain.Entities.Invite.TokenHash`), e o
valor em claro só é revelado no momento da criação do convite (resposta HTTP de quem convida +
log do `LoggingInviteNotifier`, sem provider de email real — débito nomeado já documentado na
task 016). Ou seja: **não dá pra "aceitar com 1 clique" a partir da lista de convites** como o
texto original da task sugeria.

Implementei o único fluxo possível com o contrato atual: o botão "Aceitar" na lista abre um modal
pedindo o token colado manualmente (quem convidou repassa por fora). Não editei nada no backend.
Se o produto quiser aceite de 1 clique de verdade, é decisão de Architect/Tech Lead — provavelmente
um endpoint novo `POST /api/invites/{inviteId}/accept-by-id` que valida só por `InviteId` +
email do usuário autenticado (sem precisar do token secreto), já que o usuário já está
autenticado e é o dono comprovado do convite (mesmo email).

### Risco herdado (já era conhecido, não é bug meu)

Cache do React Query na troca de organization — `OrgSwitcher.onSuccess` chama
`queryClient.clear()` (não só invalidate) exatamente pelo motivo listado no risco da task.
