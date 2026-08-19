# Erros Aprendidos

> Mantido pelo Writer. Toda entrada vem do QA (bug encontrado) ou Dev (bug corrigido) via
> handoff da cadeia — nunca escrito direto por eles. Indexado no graphify após cada escrita:
> `/graphify docs/knowledge --update`.

<!-- Cada entrada segue este formato:

## [YYYY-MM-DD] [Título curto do erro]

**Task:** docs/tasks/{id}.md
**Causa raiz:** [não o sintoma — o porquê real]
**Sintoma:** [o que foi observado]
**Correção:** [o que resolveu]
**Como evitar de novo:** [regra prática pro próximo agente]

-->

## [2026-08-17] JWT com claims mapeados perdendo `UserId` (MapInboundClaims)

**Task:** docs/tasks/002-identity-access-login.md
**Causa raiz:** Por padrão, o middleware `JwtBearer` do ASP.NET Core remapeia nomes de claims
JWT-padrão (ex: `sub`) para URIs longas de `ClaimTypes` do .NET (comportamento legado do
`JwtSecurityTokenHandler`). Sem `MapInboundClaims = false` explícito nas opções do
`JwtBearer` em `Program.cs`, o código que lê a claim pelo nome original (`sub`) não encontra
nada.
**Sintoma:** `CurrentUserAccessor.UserId` sempre retornava `null`, mesmo com token válido e
usuário autenticado — falha silenciosa, sem exception.
**Correção:** Adicionado `options.MapInboundClaims = false` na configuração do `JwtBearer` em
`Program.cs`.
**Como evitar de novo:** Todo módulo/serviço que consome JWT via `AddJwtBearer` do ASP.NET Core
deve setar `MapInboundClaims = false` explicitamente — é fácil esquecer porque o app sobe e
autentica normalmente, só a leitura de claims específicas falha silenciosamente.

## [2026-08-17] Timing side-channel no login permitia enumerar emails cadastrados

**Task:** docs/tasks/002-identity-access-login.md
**Causa raiz:** O hash Argon2id (`Verify`) só era executado se o usuário existisse no banco.
Como Argon2id é deliberadamente lento (custo de CPU alto), a ausência dessa chamada quando o
email não existe cria uma diferença de tempo de resposta mensurável entre "email existe, senha
errada" e "email não existe".
**Sintoma:** Não é um bug funcional — é uma vulnerabilidade de enumeração de usuários via
side-channel de tempo, achada em revisão de QA, não em teste automatizado.
**Correção:** O fluxo de login agora sempre chama `Verify`, mesmo quando o usuário não existe —
usando um hash Argon2id dummy pré-computado nesse caso, pra manter o tempo de resposta
constante entre os dois cenários.
**Como evitar de novo:** Em qualquer fluxo de autenticação, nunca fazer short-circuit (early
return) antes de uma operação de custo computacional dependente de segredo (hash de senha) —
sempre executar a operação de custo constante, mesmo no caminho de "usuário não existe".

## [2026-08-17] Reuso de refresh token revogado não era detectado

**Task:** docs/tasks/002-identity-access-login.md
**Causa raiz:** O campo `ReplacedByTokenHash` existia na entidade `RefreshToken` (pra registrar
qual token substituiu qual, base pra detecção de reuso — típico de rotação de refresh token),
mas nenhum código de fato verificava esse campo no fluxo de `/api/auth/refresh`. Ou seja, um
refresh token já rotacionado (revogado) continuava sendo aceito se reapresentado, o que é
exatamente o cenário de um atacante que roubou um refresh token antigo.
**Sintoma:** Reapresentar um refresh token já usado/revogado não disparava nenhuma reação de
segurança — era tratado como token inválido comum, sem revogar a cadeia de sessão comprometida.
**Correção:** Novo método `RefreshToken.RevokeForSecurityReasons()` na entidade, e novo método
de repositório `RevokeAllActiveTokensForUserAsync`. Ao detectar reuso de um token já revogado,
o sistema agora revoga TODOS os refresh tokens ativos daquele usuário — força reautenticação
completa, tratando o reuso como sinal de comprometimento da sessão.
**Como evitar de novo:** Em rotação de refresh token, a detecção de reuso não é opcional — é a
parte que de fato protege contra token roubado (a rotação sozinha não protege nada se reuso não
é vigiado). Todo módulo que implementar refresh token rotativo precisa desse par
detecção-de-reuso + revogação-em-cascata.

## [2026-08-17] Fail-open na ausência da configuração `Jwt` (signing key hardcoded de dev)

**Task:** docs/tasks/002-identity-access-login.md
**Causa raiz:** Se a seção `Jwt` sumisse do `appsettings.json`/config (ex: erro de deploy,
secret não montado), o código tinha um fallback silencioso para uma signing key hardcoded de
desenvolvimento, permitindo a aplicação subir normalmente.
**Sintoma:** Ambiente de produção poderia subir e autenticar normalmente usando uma chave de
assinatura JWT conhecida/pública (a de dev), sem qualquer erro ou alerta — vulnerabilidade
crítica de fail-open.
**Correção:** Startup agora falha rápido (`throw`) se a seção `Jwt` estiver ausente, ou se a
signing key configurada for literalmente igual ao valor default de dev.
**Como evitar de novo:** Segredos de segurança (signing keys, chaves de criptografia) nunca
devem ter fallback silencioso — a ausência ou o valor placeholder de dev deve sempre impedir o
boot da aplicação (fail-fast), não permitir que ela suba em estado inseguro.

## [2026-08-17] TOCTOU na criação de usuário gerava 500 cru em corrida de emails duplicados

**Task:** docs/tasks/002-identity-access-login.md
**Causa raiz:** A checagem de "email já existe" e a inserção do novo usuário não eram atômicas
— entre a checagem e o `SaveChangesAsync`, duas requisições concorrentes com o mesmo email
podiam passar ambas pela validação (clássico TOCTOU: time-of-check to time-of-use). A
constraint única do Postgres barrava a segunda, mas como exception não tratada.
**Sintoma:** Duas requisições simultâneas de criação de usuário com o mesmo email podiam
resultar em erro 500 genérico (exception do EF Core/Postgres vazando pra API) em vez de um erro
de negócio tipado (409/400 com mensagem clara).
**Correção:** `IdentityDbContext.SaveChangesAsync` agora traduz `DbUpdateException`/violação de
unique constraint do Postgres (código 23505) em `UniqueConstraintViolationException`, capturada
no handler e convertida em `Result.Failure(DomainErrors.User.EmailJaCadastrado)`. Ver também
padrão "Tradução de exception de infraestrutura em erro de domínio" em
`docs/knowledge/patterns.md`.
**Como evitar de novo:** Toda checagem de unicidade que depende de validação em memória antes
do save deve assumir que a constraint do banco é a fonte de verdade real (janela de corrida
sempre existe) — a camada de acesso a dados precisa traduzir a violação de constraint em erro
de negócio, não deixar a exception de infra vazar pra cima.

## [2026-08-17] Closure de serviço scoped (`ITenantContext`) no `Model` cacheado do EF Core vazava tenant entre requests

**Task:** docs/tasks/001-sharedkernel-tenancy.md
**Causa raiz:** O EF Core compila e cacheia o `IModel` (que inclui os `HasQueryFilter`) UMA VEZ
por processo, não por request. A spec original pedia
`ApplyTenantQueryFilters(ModelBuilder, ITenantContext)`, capturando `ITenantContext` (um serviço
scoped, resolvido por request) diretamente no closure da expression tree do filtro. Como a
expression é compilada e fixada na primeira vez que o `Model` é construído, ela fecharia sobre a
instância de `ITenantContext` do PRIMEIRO request que disparou esse build — não sobre a
instância do request atual.
**Sintoma:** Em produção, todo request seguinte ao primeiro que construiu o `Model` vazaria
dados do tenant do primeiro request, independente de qual tenant estivesse realmente autenticado
— vazamento de isolamento multi-tenant, silencioso (sem exception, dados errados retornados).
Achado antes de chegar em produção, durante implementação/revisão do Dev Backend.
**Correção:** Assinatura mudada para capturar o `TContext` (o próprio DbContext, via
`ITenantAwareDbContext.CurrentTenantId`) em vez do `ITenantContext` — o EF Core reavalia essa
propriedade por instância viva de DbContext a cada query, porque o DbContext (diferente do
`Model`) é recriado por request/escopo. Ver padrão completo em
`docs/knowledge/patterns.md` ("Filtro global de tenant genérico por `TContext`").
**Como evitar de novo:** Nunca capturar um serviço scoped diretamente no closure de um
`HasQueryFilter`/expression tree que vira parte do `IModel` do EF Core — o `Model` é
efetivamente singleton por processo. Sempre capturar uma propriedade do próprio `DbContext`
(que é recriado por request), nunca uma dependência externa injetada separadamente.

## [2026-08-17] Reflection genérico usando overload não-genérica de `ModelBuilder.Entity(Type)`

**Task:** docs/tasks/001-sharedkernel-tenancy.md
**Causa raiz:** Ao aplicar o filtro de tenant via reflection para cada entidade que implementa
`IMustHaveTenant`, o código inicial chamava `ModelBuilder.Entity(Type)` — que retorna
`EntityTypeBuilder` NÃO-genérico — e passava esse retorno para uma invocação via
`MethodInfo.Invoke` que esperava o tipo genérico `EntityTypeBuilder<TEntity>`. Isso compila
(reflection não é checado em tempo de compilação), mas quebra em runtime.
**Sintoma:** Exception em runtime ao montar o `OnModelCreating` (cast/invoke falhando), não
detectável em build nem em revisão de código superficial — só rodando o app ou os testes de
model.
**Correção:** Trocado para invocar a sobrecarga genérica `ModelBuilder.Entity<TEntity>()` via
`MethodInfo` construído com `MakeGenericMethod(entityType)`, retornando o
`EntityTypeBuilder<TEntity>` esperado.
**Como evitar de novo:** Ao usar reflection para invocar APIs genéricas do EF Core
(`ModelBuilder.Entity<T>()`), sempre confirmar qual overload está sendo resolvida
(`Entity(Type)` vs `Entity<TEntity>()`) — a não-genérica existe e tem assinatura de retorno
diferente; escrever um teste de integração que exercite o `OnModelCreating` real (não só
compile) é o jeito de pegar isso antes de produção.

## [2026-08-17] Race de criação sem lock permitia dois agendamentos sobrepostos no mesmo slot

**Task:** docs/tasks/004-agenda-vertical.md
**Causa raiz:** `CreateAgendamentoCommandHandler` não usava o lock Redis (só o `Confirmar` usava
até então). `xmin`/concorrência otimista protege UPDATE vs UPDATE, não INSERT vs INSERT — duas
requisições concorrentes para o mesmo profissional/horário podiam ambas passar a checagem de
sobreposição (SELECT) antes de qualquer uma das duas commitar o INSERT.
**Sintoma:** Dois agendamentos `Agendado` sobrepostos para o mesmo profissional/horário
persistiam simultaneamente no banco — o "fantasma" sobrevivia até cancelamento manual e
continuava contando como ocupado na disponibilidade, distorcendo a agenda. Achado pelo QA.
**Correção:** O mesmo lock Redis curto (TTL 5s, chave por tenant+profissional+minuto) que já
existia em `Confirmar` foi adicionado em `Create`, com a checagem de sobreposição revalidada
DENTRO do lock (não só antes dele).
**Como evitar de novo:** Concorrência otimista (`xmin`/`RowVersion`) não protege criação de
registro novo (INSERT vs INSERT) — só protege edição de registro existente. Todo fluxo de
"criar recurso que não pode se sobrepor a outro" em alta concorrência precisa de lock explícito
+ revalidação da invariante dentro do lock, não confiar só na concorrência otimista do EF. Ver
padrão completo em `docs/knowledge/patterns.md` ("Lock Redis serializa a corrida; revalidação de
query garante a invariante").

## [2026-08-17] RBAC por papel sem ownership check permitia um Dentista mexer na agenda de outro

**Task:** docs/tasks/004-agenda-vertical.md
**Causa raiz:** A policy de autorização checava apenas o papel (`Dentista` pode
confirmar/cancelar/concluir agendamento), sem checar se o agendamento pertencia à agenda do
próprio dentista logado. Como múltiplos usuários compartilham o mesmo papel `Dentista` no mesmo
tenant, checar só o papel é insuficiente quando o recurso tem um dono específico dentro desse
papel.
**Sintoma:** Um `Dentista` autenticado conseguia confirmar, cancelar ou concluir agendamentos de
OUTRO dentista do mesmo tenant — violação de escopo de dados dentro do próprio tenant. Achado
pelo QA.
**Correção:** Novo `AgendaOwnershipGuard`, que checa `Profissional.UserId == usuário logado`
quando o papel do usuário é `Dentista` (sem restrição para `Admin`/`Recepcao`). Aplicado em
Confirmar/Cancelar/Concluir; conscientemente não aplicado em Create (ver
docs/knowledge/business-rules.md — pendência de produto).
**Como evitar de novo:** RBAC baseado só em papel (role) é insuficiente sempre que múltiplos
usuários compartilham o mesmo papel mas administram recursos distintos dentro dele (ex: vários
dentistas, cada um só a própria agenda) — nesses casos, adicionar ownership check (dono do
recurso == usuário logado) além da checagem de papel, não em vez dela.

## [2026-08-17] TOCTOU em `CreateProntuarioCommandHandler` — mesma classe de bug pela terceira vez

**Task:** docs/tasks/005-prontuario-eletronico.md
**Causa raiz:** `ExistsByPacienteIdAsync` (check de negócio) e `SaveChangesAsync` (commit real)
são duas operações separadas no tempo — entre uma e outra, uma segunda requisição concorrente
pode passar pelo mesmo check antes da primeira commitar. O índice único `(TenantId, PacienteId)`
no banco é quem de fato impede duas linhas, mas o handler não capturava a exception que isso
gera.
**Sintoma:** Nenhum em produção ainda (achado em auto-revisão antes de qualquer deploy, não pelo
QA desta vez — sessão rodou sem par Dev Backend/QA por limite de gasto em subagent, ver
docs/sprints/sprint-2.md). Se não corrigido, duas requisições `POST /api/prontuarios` concorrentes
pro mesmo paciente resultariam na segunda estourando 500 cru (`DbUpdateException`/Postgres 23505)
em vez de `Result.Failure` tipado.
**Correção:** `try/catch` em volta do `SaveChangesAsync`, capturando
`UniqueConstraintViolationException` (já traduzida na camada certa por
`RecordsDbContext.SaveChangesAsync`, mesmo padrão de Identity/Patients) e devolvendo
`Result.Failure(DomainErrors.Prontuario.JaExistePorPaciente)`. Teste de regressão adicionado.
**Como evitar de novo:** Esta é a MESMA classe de bug que já apareceu em Identity (email
duplicado, task 002) e Patients (CPF duplicado, task 003) — todo `Create*CommandHandler` que faz
um check de unicidade (`Exists*Async`) ANTES de criar precisa, sem exceção, envolver o
`SaveChangesAsync` em try/catch pra `UniqueConstraintViolationException`. Virou checklist: se o
Domain/EF Core tem um índice único, o handler correspondente PRECISA desse try/catch — não é
opcional, não depende de já ter sido pego por QA uma vez.

## [2026-08-17] Navegação de coleção encapsulada quebrada no módulo Billing — 26/26 testes "passando" sem NUNCA construir o Model real do EF Core

**Task:** docs/tasks/006-financeiro-particular.md (bug introduzido), achado na task 008 ao
escrever teste de infraestrutura pro módulo Reporting
**Causa raiz:** `FaturaConfiguration` configurava a coleção `Parcelas` via
`builder.HasMany<Parcela>("_parcelas")` (nome do campo privado) — mas o EF Core TAMBÉM
auto-descobre a propriedade pública `Parcelas` (`IReadOnlyList<Parcela>`) como candidata a
navegação por convenção, e resolve o mesmo backing field `_parcelas` pra ela. Resultado: duas
configurações de navegação apontando pro mesmo campo → `InvalidOperationException` ("member
already used") assim que o EF Core tenta VALIDAR o Model — o que só acontece na primeira query
real contra o `DbContext`. Bug idêntico ao já documentado na task 001 (closure de tenant no Model
cacheado): tudo que só existe "no momento de construir o Model" não aparece em teste nenhum que
não force essa construção a acontecer.
**Sintoma:** NENHUM em produção ainda — os 26 testes de `CreateFaturaParticularCommandHandler`,
`CreateFaturaConvenioCommandHandler` etc. todos usavam `Mock<IFaturaRepository>`, então nunca
instanciavam `BillingDbContext` de verdade nem forçavam o EF Core a validar o Model. O módulo
inteiro passaria 100% dos testes e quebraria em runtime na PRIMEIRA chamada real a
`FaturaRepository.GetByIdAsync`/`ListAsync` (qualquer leitura de fatura) — bug 100% invisível até
alguém escrever um teste com `UseInMemoryDatabase` de verdade.
**Correção:** Trocado `HasMany<Parcela>("_parcelas")` por `HasMany(f => f.Parcelas)` (a
propriedade pública, que É a navegação real) + `Navigation(f => f.Parcelas).UsePropertyAccessMode
(PropertyAccessMode.Field)` (instrui o EF a ler/escrever via o campo por baixo, já que a
propriedade não tem setter visível). Dois `Include("_parcelas")` (string, em `FaturaRepository`
e `FaturamentoSummaryProvider`) também precisaram virar `Include(f => f.Parcelas)` — o nome da
navegação no Model mudou de `_parcelas` pra `Parcelas`.
**Como evitar de novo:** (1) Pra coleção encapsulada (propriedade pública `IReadOnlyList<T>` +
campo privado `List<T>`), configurar SEMPRE via a propriedade pública
(`HasMany(x => x.PropriedadePublica)` + `Navigation(x => x.PropriedadePublica)
.UsePropertyAccessMode(Field)`), nunca via o nome do campo como string — o nome do campo como
string cria uma navegação PARALELA, não a mesma. Isso é diferente do padrão usado com sucesso em
`Records.Infrastructure` (`Prontuario.Odontograma`), onde o campo mapeado é um `Dictionary`
ESCALAR (JSONB), não uma navegação pra outra entidade — os dois casos parecem iguais
("encapsular coleção atrás de propriedade só-leitura") mas exigem configuração diferente no EF
Core. (2) Todo módulo com `DbContext` precisa de PELO MENOS UM teste que instancie o `DbContext`
de verdade com `UseInMemoryDatabase` e rode uma query — um teste que só mocka o repositório nunca
constrói o Model, e é exatamente onde esse tipo de bug mora. Checklist novo: nenhum módulo fecha
sem pelo menos um `TenantQueryFilterTests`-like rodando contra `DbContext` real.

## [2026-08-18] Quase-erro evitado: rename de substring `Unidade`→`Branch` quase trocou o conceito errado (`UnidadeMedida`)

**Task:** docs/tasks/011-rename-unidade-branch.md
**Causa raiz (do risco, não de um bug fechado):** rename em massa por substring não distingue
significado — `ItemEstoque.UnidadeMedida` ("unidade de medida" do item de estoque, ex: kg/un/cx)
contém a substring `Unidade`, mas é um conceito TOTALMENTE diferente de `Unidade` (a clínica
física, alvo real do rename pra `Branch`). Um script de substituição sem proteção prévia teria
produzido `BranchMedida` (identificador sem sentido) e trocado a mensagem de erro `"Unidade de
medida é obrigatória."` por `"Branch de medida é obrigatória."` — o build continuaria verde
(rename de identificador é só substituição textual, `dotnet build` não valida significado), só a
leitura humana ou um teste que comparasse a mensagem literal pegaria o erro.
**Sintoma (o que teria acontecido, não o que de fato aconteceu):** nenhum erro de compilação,
nenhum teste quebrado por padrão — silencioso, porque o rename de identificador continua
compilando e o teste que valida a mensagem de erro (se existir) só falha se comparar a string
exata. Exatamente o tipo de bug que sobrevive a `dotnet build`+`dotnet test` verdes e só aparece
em revisão humana ou em produção, quando alguém lê "Branch de medida é obrigatória." na tela.
**Como foi evitado:** antes do script de substituição, rodou-se
`grep -rohiE "[a-zA-Z_]*unidade[a-zA-Z_]*" | sort -u` pra listar TODOS os ~50 tokens distintos
que contêm a substring `unidade` — foi olhando essa lista, não o resultado do rename, que o Dev
Backend identificou `UnidadeMedida` como conceito distinto e o protegeu com placeholder antes de
rodar o replace (técnica completa em `docs/knowledge/patterns.md`, "Rename mecânico em massa").
Uma ocorrência em texto livre (mensagem de erro com espaço, não identificador colado) escapou da
proteção automática e precisou de correção manual pontual, achada no mesmo grep.
**Como evitar de novo:** rename de substring em massa nunca parte direto pro script de
substituição — o passo obrigatório anterior é grep por TODO token que CONTÉM a palavra-alvo
(não só a palavra isolada) e revisão humana dessa lista completa procurando falsos-cognatos,
antes de rodar qualquer replace. `dotnet build`/`dotnet test` verdes não são evidência de rename
semanticamente correto — só provam que a sintaxe ainda compila, não que o significado de cada
ocorrência foi preservado.

## [2026-08-18] Débito técnico leve: `OrganizationMembership`/`User` sem FK explícita no banco (`OnDelete(Cascade)` da spec não implementada)

**Task:** docs/tasks/013-organization-membership-nn.md
**Causa raiz:** A spec do Architect (item 3, contrato de navegações no `DbContext`) previa
`HasForeignKey(...).OnDelete(DeleteBehavior.Cascade)` explícito entre `OrganizationMembership` e
`User`/`Organization`. A implementação (`OrganizationMembershipConfiguration`,
`UserConfiguration`) não define essa FK — confirmado por QA lendo a migration `InitialCreate`
gerada: nenhuma `ForeignKey` aparece em `OrganizationMemberships`. Não corrigido nesta task —
aceito como débito porque `User.Desativar()`/`Organization.Desativar()` são soft-delete (nunca
hard delete no código atual), então não existe caminho de execução hoje que produza membership
órfã.
**Sintoma:** Nenhum em runtime hoje (soft-delete cobre o caso). Risco é latente: se algum código
futuro fizer hard-delete de `User` ou `Organization` (via migration manual, script de expurgo de
dado, LGPD "direito ao esquecimento", etc.), `OrganizationMembership` ficaria órfã no banco sem
o Postgres impedir ou cascatear — silencioso, sem exception.
**Correção:** Nenhuma ainda — débito aceito conscientemente pelo QA/Writer nesta task, não
bloqueante pro merge.
**Como evitar de novo:** Antes de qualquer feature de hard-delete (expurgo de dado, LGPD,
purge de conta) em `Identity.Domain`, adicionar as FKs explícitas
(`HasForeignKey(m => m.UserId).OnDelete(Cascade)` /
`HasForeignKey(m => m.OrganizationId).OnDelete(Cascade)`) em
`OrganizationMembershipConfiguration` e regerar a migration ANTES de implementar o hard-delete —
não depois. Até lá, qualquer PR que introduza `Users.Remove`/`Organizations.Remove` (hard delete)
deve ser bloqueado em revisão até essa FK existir.

## [2026-08-18] Quase-erro evitado: handoff pro Dev Backend divergiu da spec fechada do Architect (dois itens pedidos que A2 não incluía)

**Task:** docs/tasks/022-contratos-leitura-comissao.md, docs/tasks/023-query-comissoes-endpoint-rbac.md
**Causa raiz (do risco, não de um bug fechado):** o handoff que chegou ao Dev Backend (via
broker/Architect) pedia dois itens — `IComissaoSummaryProvider.ObterComissoesAsync` com parâmetro
extra `profissionalIds` e `IProfissionalLookup.ObterPorUserIdAsync` — que NÃO estavam na
assinatura final fechada pela decisão A2 do Architect, registrada por escrito na seção "Decisão do
Architect" da própria task 022 ("nenhuma mudança nas assinaturas — já estão corretas na spec"). O
handoff aparentemente resumiu/relembrou a intenção da spec em vez de citá-la como fonte, e o resumo
divergiu do texto fechado.
**Sintoma (o que teria acontecido, não o que de fato aconteceu):** se o Dev Backend tivesse
confiado no handoff resumido em vez de reabrir a task e ler a decisão A2 escrita, teria
implementado assinatura fora do que o Architect fechou — reabrindo uma decisão de Architect por
conta própria, sem autoridade pra isso, e criando divergência entre o que a task documenta e o que
o código faz.
**Como foi evitado:** o Dev Backend leu a seção "Decisão do Architect" da task 022 como fonte de
verdade (não o handoff resumido), viu que A2 não incluía os dois itens, e optou por NÃO
implementá-los por conta própria — documentou a discrepância explicitamente em vez de decidir
sozinho, deixando pra Architect/PO ratificar se quisessem. QA confirmou que a alternativa
funcional (composição em memória sobre a porta batch já existente) cobre o mesmo resultado sem
N+1 — ver padrão "RBAC de ownership resolvido em memória" em `docs/knowledge/patterns.md`.
**Como evitar de novo:** todo handoff entre agentes da cadeia que se refere a uma decisão técnica
já fechada (Architect, Tech Lead) deve CITAR o arquivo/seção da task como fonte de verdade
("ver task 022, seção Decisão do Architect A2"), não reformular de memória o que a decisão dizia —
resumir de memória é onde a divergência nasce. Todo agente que recebe handoff sobre uma decisão já
fechada deve ler a fonte citada antes de implementar, não confiar só no resumo do handoff quando a
spec original está disponível e é curta o suficiente pra reler.

## [2026-08-18] Bug latente: `AcceptInviteCommandHandler` não reativa `OrganizationMembership` inativa ao aceitar convite novo pro mesmo email/org

**Task:** docs/tasks/016-invite-afiliacao.md (achado pelo QA), follow-up em
docs/tasks/020-membership-reativar-em-invite.md
**Causa raiz:** `AcceptInviteCommandHandler` (`Identity.Application/Commands/AcceptInvite/AcceptInviteCommandHandler.cs`,
linhas 69-76) trata a existência prévia de uma `OrganizationMembership` pro mesmo `(UserId,
OrganizationId)` como caminho de idempotência — se `existingMembership is not null`, marca o
convite como `Aceito` e retorna sucesso com o `Role` da membership existente, SEM checar
`existingMembership.IsAtivo`. `CreateInviteCommandHandler` (mesmo módulo, linha 56) permite
convidar de novo um email com membership INATIVA na org — só bloqueia se
`existingMembership.IsAtivo`, comportamento documentado pelo teste
`Should_AllowInvite_When_InvitedEmailHasInactiveMembership`. A combinação das duas decisões
(convite permitido pra membership inativa + accept tratando "membership existe" como
"já está tudo certo") deixa a membership presa em `Inativo` mesmo depois do usuário "aceitar" o
convite.
**Sintoma:** Silent success — `POST /api/invites/{token}/accept` devolve `200 Ok` normalmente, o
usuário acredita que entrou na organização, mas a membership continua `Inativo` no banco. A
próxima requisição autenticada dele nessa org falha, porque a eleição de organização no login
(`GetActiveMembershipsForUserAcrossOrganizationsAsync`) filtra por `Ativo` e não a retorna. Sem
exception, sem erro visível no momento do accept — só na tentativa seguinte de uso.
**Correção:** Feita em 2026-08-19 (`docs/tasks/020-membership-reativar-em-invite.md`, status
`done`). `OrganizationMembership.Reativar(Role role)` novo no Domain (seta `Ativo` + atualiza
`Role` pro papel do convite novo, mesmo padrão incondicional de `Desativar()`).
`AcceptInviteCommandHandler` agora bifurca em `existingMembership.IsAtivo`: `true` mantém o
comportamento antigo (idempotência, papel preservado); `false` chama `Reativar(invite.Role)` antes
de aceitar o convite. Teste de regressão:
`Should_ReactivateMembership_When_AcceptingInviteForInactiveMembership`.
**Por que não é crítico hoje:** grep confirma que NÃO existe nenhum caminho de API que dispare
`OrganizationMembership.Desativar()` — o método existe no Domain (soft-delete), mas nenhum
handler o invoca ainda. O cenário só é atingível via seed/migration/acesso direto ao banco;
inalcançável pela superfície de API atual.
**Como evitar de novo:** Toda vez que um handler trata "recurso já existe" como branch de
idempotência (retorna sucesso sem recriar), a checagem de existência precisa cobrir o ESTADO do
recurso, não só sua presença — `existingMembership is not null` não é o mesmo invariante que
`existingMembership.IsAtivo`. Confirmar esse cuidado especificamente antes de implementar
qualquer endpoint de "remover/desativar membro" (roadmap Fase 1, Identity) — nesse dia, o fluxo
"desativar → convidar de novo → aceitar" vira alcançável via API e o silent fail aqui descrito
vira bug de produção instantâneo.

## [2026-08-19] `JsonSerializerOptions` custom sem `PropertyNameCaseInsensitive`/`PropertyNamingPolicy` derruba silenciosamente `ReadFromJsonAsync<T>()` — sem exceção, campo vira `null`

**Task:** docs/tasks/021-bootstrap-integration-tests.md (achado lateral, não o objetivo da task)
**Causa raiz:** `System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>()` SEM
`JsonSerializerOptions` explícitas usa por baixo dos panos um fallback "web" implícito
(`PropertyNameCaseInsensitive = true`, efetivamente tolerante a `camelCase` vs `PascalCase`).
Passar um `JsonSerializerOptions` PRÓPRIO (ex.: só pra registrar `JsonStringEnumConverter`, porque
a API serializa enum como string) SUBSTITUI esse fallback inteiro por um `JsonSerializerOptions`
"puro" — `PropertyNameCaseInsensitive = false` e `PropertyNamingPolicy = null` (exige match EXATO
de nome, `Foo` ≠ `foo`) por padrão. A API (ASP.NET Core MVC, `AddJsonOptions`) serializa em
`camelCase` (`"accessToken"`); os DTOs client-side eram records em `PascalCase`
(`AccessToken`) — nenhuma propriedade batia, `JsonSerializer` (que usa construtor parametrizado
pra records) simplesmente não populava os parâmetros não-casados, cada um ficando com o `default`
do tipo (`string` → `null`) **sem lançar exceção**.
**Sintoma:** `signup.AccessToken` era `null`/`""` sem erro visível na chamada de signup em si — só
estourava 401 Unauthorized bem mais adiante, na PRÓXIMA chamada HTTP que usava esse token vazio
como Bearer. 8 de 9 testes de integração falharam em cascata com a mesma causa raiz mascarada
atrás de "AuthenticationScheme: Bearer was challenged" — sem stack trace nenhum apontando pro JSON.
**Correção:** `JsonSerializerOptions` custom que vai substituir o default do
`ReadFromJsonAsync`/`PostAsJsonAsync` precisa REPLICAR manualmente o que o fallback "web" já dava:
`PropertyNameCaseInsensitive = true` (ou `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
explícito, mais correto quando o client também SERIALIZA request com essas options — caso de
`JsonSerializer.Deserialize` direto, que não tem fallback "web" nenhum). Ver
`tests/Api.IntegrationTests/AuthFlow.cs`, `JsonOptions`.
**Como evitar de novo:** Toda vez que precisar de `JsonSerializerOptions` custom num client HTTP
C# (teste de integração, SDK client, etc.) contra uma API ASP.NET Core, partir de
`new JsonSerializerOptions(JsonSerializerDefaults.Web)` (que JÁ inclui case-insensitive +
camelCase) e só ADICIONAR o que falta (ex.: `Converters.Add(new JsonStringEnumConverter())`), em
vez de `new JsonSerializerOptions()` vazio + preencher tudo na mão — reduz a chance de esquecer
uma das 2-3 opções que o fallback implícito cobria de graça.

## [2026-08-19] Widening de RBAC (Owner) corrigido só no backend (030) — mesmo padrão de gate quebrado reapareceu em 4 arquivos do frontend

**Task:** docs/tasks/030-owner-role-parity-rbac.md (fix original), achado pelo QA em
docs/tasks/027-frontend-dashboard-filtros-filial-classe.md e
docs/tasks/028-frontend-comprovante-pagamento.md (2 rodadas de revalidação)
**Causa raiz:** O achado original que abriu a task 030 (Dev Frontend, durante a implementação da
027) foi registrado com escopo estreito — "`BranchesController` é Admin-only, Owner não lista
filiais" — e o grep que confirmou o padrão foi feito só sobre `[Authorize(Roles=...)]` no
backend (`src/Bootstrap/.../Controllers/*.cs`). Ninguém, no handoff da 030, parou pra perguntar
"esse MESMO padrão de gate (`role === 'Admin'` sem Owner) existe em outra camada além do
backend?" — o achado não gerou reflexo de generalização, ficou pontual ao controller que
disparou o report. O frontend tinha o padrão idêntico (`isAdmin`/`role === 'Admin'` sem Owner)
espalhado em `App.tsx` (rota `/relatorios`), `AppLayout.tsx` (menu "Relatórios"),
`ReportsPage.tsx` (filtro de filial) e `ComprovantePage.tsx` (filtro de filial), todos
consistentes com o backend ANTES da 030, mas nunca atualizados quando a 030 mudou o backend.
**Sintoma:** Depois do backend (030) já liberar `Owner` em `BranchesController` e
`ReportsController`, o usuário Owner continuava sem ver a tela/filtro na prática — redirecionado
pra `/agenda` antes de `ReportsPage` montar (`ProtectedRoute roles={['Admin']}`), sem o item
"Relatórios" no menu (`AppLayout roles: ['Admin']`), e mesmo chegando na tela, com o `<Select>`
de filial escondido (`isAdmin` em vez de `isEmpresa`). O fix interno do backend virou código
morto pro Owner — zero efeito visível, sem nenhum erro/exception, só comportamento idêntico ao
de antes do fix. Passou pela primeira rodada de QA da 027 como PASS parcial (fix pontual
aplicado pelo broker corrigiu só o componente, não a rota/menu) e só foi pego integralmente na
revalidação seguinte, quando o QA insistiu numa varredura sistemática (`grep` por
`=== 'Admin'`, `includes('Admin')`, `roles: [...]`) em vez de aprovar o primeiro fix pontual.
**Correção:** `App.tsx:65` (`roles={['Owner','Admin']}` na rota `/relatorios`),
`AppLayout.tsx:63` (`roles: ['Owner','Admin']` no menu), `ReportsPage.tsx` (`isAdmin`→`isEmpresa`
no render e no `enabled` da query `branches`), `ComprovantePage.tsx` (mesmo troque
`isAdmin`→`isEmpresa`, comentário stale removido). Varredura final do QA confirmou zero gate
`Admin`-only isolado remanescente nas 4 camadas (ver padrão "Widening de RBAC" em
`docs/knowledge/patterns.md`).
**Como evitar de novo:** Quando um achado de bug de RBAC é reportado com escopo "encontrei isso
NUM lugar específico" (ex: um controller), o handler do achado (Tech Lead/Architect ao abrir a
task) precisa perguntar explicitamente "esse padrão se repete em outras camadas do mesmo
sistema (frontend rota, frontend menu, componente inline)?" ANTES de fechar o escopo da task —
não esperar o QA achar isso depois, em rodadas sucessivas de revalidação. Todo widening de
role/permissão nova fecha com o checklist de 4 camadas (backend, rota, menu, componente) grepado
de uma vez, não uma de cada vez.
