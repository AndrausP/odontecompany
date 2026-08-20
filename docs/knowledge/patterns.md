# Padrões do Projeto

> Mantido pelo Writer. Convenções e padrões descobertos ou definidos por Architect, Dev
> Backend, Dev Frontend ou Designer, sempre via handoff — nunca escrito direto por eles.
> Indexado no graphify após cada escrita: `/graphify docs/knowledge --update`.

<!-- Cada entrada segue este formato:

## [Nome do padrão]

**Camada/área:** Domain | Application | Infrastructure | Presentation | UI | outro
**Definido por:** Architect | Dev Backend | Dev Frontend | Designer
**Descrição:** [o padrão em si]
**Quando usar:** [contexto de aplicação]
**Exemplo:** [arquivo/trecho de referência]

-->

## Clean Architecture por módulo, com fronteira `*.Contracts`

**Camada/área:** Domain | Application | Infrastructure | Presentation
**Definido por:** Dev Backend (task docs/tasks/002-identity-access-login.md)
**Descrição:** Cada módulo de negócio (ex: `Identity`) é dividido em 4 projetos:
`{Modulo}.Domain` (entidades, VOs, regras), `{Modulo}.Application` (use cases/handlers, DTOs,
ports/interfaces), `{Modulo}.Infrastructure` (implementação de repositórios, EF Core, serviços
externos), `{Modulo}.Contracts` (o que outros módulos podem consumir — requests/responses
públicos). Domain só pode depender do `SharedKernel`. `IMustHaveTenant` mora no `SharedKernel`
(não em `Infrastructure.Common`) exatamente por essa regra — Domain não pode depender de
Infrastructure.
**Quando usar:** Todo novo módulo de negócio da plataforma (Pacientes, Agenda, etc.).
**Exemplo:** `src/Modules/Identity/{Identity.Domain,Identity.Application,Identity.Infrastructure,Identity.Contracts}`

## Result pattern para erro de negócio

**Camada/área:** Application | Domain
**Definido por:** Dev Backend (task docs/tasks/002-identity-access-login.md)
**Descrição:** Falhas de regra de negócio (não exceptions de infra) retornam `Result` /
`Result<T>` (definidos no `SharedKernel`) em vez de lançar exception. O pipeline MediatR
(`ValidationBehavior<TRequest,TResponse>`) já intercepta falha de validação de input e devolve
`Result` de falha antes mesmo de chegar no handler — handler nunca precisa lançar exception pra
sinalizar erro de negócio esperado (ex: email duplicado, tenant inativo, credenciais inválidas).
**Quando usar:** Toda vez que a falha é uma regra de negócio esperada, não uma condição
excepcional/de infra.
**Exemplo:** `DomainErrors.User.EmailJaCadastrado`, `DomainErrors.Tenant.Inativo` — ver também
erro aprendido sobre TOCTOU em `docs/knowledge/errors-aprendidos.md`.

## Ports-and-adapters para serviços de infraestrutura sensíveis

**Camada/área:** Application (port) | Infrastructure (adapter)
**Definido por:** Dev Backend (task docs/tasks/002-identity-access-login.md)
**Descrição:** Interfaces como `IPasswordHasher` e `IJwtTokenService` são declaradas em
`Identity.Application` (o consumidor define o contrato), e implementadas em
`Identity.Infrastructure`. Isso permite trocar a implementação (ex: Argon2id → outro algoritmo)
sem tocar em regra de negócio, e testar handlers com mock/fake do port.
**Quando usar:** Qualquer dependência de infra que a camada Application precisa consumir
(hashing, emissão de token, envio de email, etc.).
**Exemplo:** `IPasswordHasher`, `IJwtTokenService` em `Identity.Application`; implementação em
`Identity.Infrastructure`.

## Tradução de exception de infraestrutura em erro de domínio na camada de acesso a dados

**Camada/área:** Infrastructure
**Definido por:** Dev Backend (fix de bug do QA — task docs/tasks/002-identity-access-login.md)
**Descrição:** Exceptions vindas do banco (ex: `DbUpdateException` por violação de constraint
única do Postgres, código 23505) são capturadas e traduzidas em uma exception de domínio
tipada (`UniqueConstraintViolationException`) dentro do próprio `DbContext`
(`IdentityDbContext.SaveChangesAsync`), não na camada Application. O handler de Application só
precisa capturar a exception de domínio e converter em `Result.Failure(...)`. Evita que erro
cru de infraestrutura (ex: 500 genérico do Postgres) vaze pra camada de aplicação/API.
**Quando usar:** Toda vez que uma condição de concorrência (TOCTOU) ou constraint de banco pode
ser violada em corridas de requisições simultâneas — a tradução acontece o mais perto possível
da origem do erro (o DbContext), não em uma camada acima.
**Exemplo:** `IdentityDbContext.SaveChangesAsync` → `UniqueConstraintViolationException` →
`Result.Failure(DomainErrors.User.EmailJaCadastrado)`.

## Filtro global de tenant genérico por `TContext`, nunca por serviço scoped no closure

**Camada/área:** Infrastructure
**Definido por:** Dev Backend (task docs/tasks/001-sharedkernel-tenancy.md)
**Descrição:** `ApplyTenantQueryFilters<TContext>(ModelBuilder, TContext)` em
`Infrastructure.Common` aplica `HasQueryFilter` a toda entidade que implementa
`IMustHaveTenant`, capturando no closure da expression tree o próprio `TContext` (o DbContext,
via `ITenantAwareDbContext.CurrentTenantId`) — nunca um serviço scoped como `ITenantContext`
diretamente. Qualquer módulo usa em uma linha no `OnModelCreating`. Ver causa raiz completa em
`docs/knowledge/errors-aprendidos.md` ("closure de serviço scoped no Model cacheado do EF
Core").
**Quando usar:** Todo `DbContext` de módulo que tem entidades com `TenantId` e precisa de
isolamento automático por query filter.
**Exemplo:** `Infrastructure.Common.ApplyTenantQueryFilters<TContext>`, usado por
`PatientsDbContext` e `SchedulingDbContext` no `OnModelCreating`.

## `ValidationBehavior` compartilhado, não duplicado por módulo

**Camada/área:** Application
**Definido por:** Dev Backend (task docs/tasks/003-pacientes-crm.md)
**Descrição:** `ValidationBehavior<TRequest,TResponse>` (pipeline MediatR de validação
automática) foi extraído de `Identity.Application` pra um projeto novo e compartilhado
`src/Shared/Application.Common`, registrado uma única vez em `Program.cs`. Copiar/duplicar essa
classe por módulo faria o MediatR registrar o open generic duas vezes no container de DI,
rodando a validação 2x por request — bug real e silencioso (sem erro, só custo dobrado e
potencial de mensagens de erro duplicadas).
**Quando usar:** Todo behavior de pipeline MediatR (validação, logging, transação) que se aplica
a mais de um módulo — vai em `Application.Common`, nunca copiado.
**Exemplo:** `src/Shared/Application.Common/Behaviors/ValidationBehavior.cs`, registro único em
`Program.cs`.

## Porta de leitura cross-module (`I{Modulo}Lookup`) em vez de referenciar Domain/Infrastructure de outro módulo

**Camada/área:** Application (Contracts)
**Definido por:** Dev Backend (task docs/tasks/004-agenda-vertical.md)
**Descrição:** `Scheduling.Application` precisa validar/consultar dados de paciente, mas nunca
referencia `Patients.Domain` nem `Patients.Infrastructure` — só `Patients.Contracts`, através de
uma porta nova `IPatientLookup` (não existia leitura cross-module antes desta task). Confirmado
por QA via grep: zero violação de fronteira de módulo.
**Quando usar:** Sempre que um módulo precisa ler (nunca escrever) dado de outro módulo — cria-se
uma porta `I{Modulo}Lookup` no `.Contracts` do módulo dono do dado, implementada no
`.Infrastructure` do módulo dono, consumida via DI pelo módulo consumidor.
**Exemplo:** `Patients.Contracts.IPatientLookup`, consumido por `Scheduling.Application`.

## Lock Redis serializa a corrida; revalidação de query garante a invariante de negócio

**Camada/área:** Application | Infrastructure
**Definido por:** Dev Backend (task docs/tasks/004-agenda-vertical.md), a partir de bug achado
pelo QA
**Descrição:** São dois papéis diferentes que não podem ser confundidos: o lock Redis (TTL 5s,
chave por tenant+profissional+minuto, formato `yyyyMMddHHmm`) só serializa a corrida — garante
que duas requisições concorrentes pro mesmo profissional/minuto não executem em paralelo. Quem
garante de fato a invariante de não-sobreposição é a query de disponibilidade REVALIDADA dentro
do lock, depois de adquirido. O lock por minuto exato não pega sobreposição parcial entre
minutos diferentes por si só — é a revalidação que cobre isso. Ver bug de origem ("race real na
criação sem lock") em `docs/knowledge/errors-aprendidos.md`.
**Quando usar:** Todo recurso concorrido (slot de agenda, reserva, estoque) onde múltiplas
requisições podem competir pelo mesmo recurso antes de qualquer commit — lock serializa, query
revalidada dentro do lock garante a regra de negócio. Padrão que deve se repetir em outros
módulos com recurso concorrido.
**Exemplo:** `CreateAgendamentoCommandHandler` e `ConfirmarAgendamentoCommandHandler` em
`Scheduling.Application`.

## `xmin` do Postgres como concurrency token nativo, não `RowVersion byte[]` manual

**Camada/área:** Domain | Infrastructure
**Definido por:** Dev Backend (task docs/tasks/004-agenda-vertical.md)
**Descrição:** A spec original do Architect (docs/decisions.md) previa `RowVersion` via EF Core
`[Timestamp]`. O Dev Backend trocou para o campo de sistema `xmin` do Postgres, mapeado como
concurrency token via convenção do provider Npgsql (`IsRowVersion()` sobre coluna `xmin`), em
vez de manter um campo `byte[] RowVersion` explícito na entidade de Domain. Mantém o Domain
limpo de detalhe de persistência (xmin não é uma propriedade que o domínio precisa conhecer ou
gerenciar) e é o padrão nativo recomendado pelo próprio provider Npgsql pra concorrência
otimista.
**Quando usar:** Toda entidade que precisa de concorrência otimista em Postgres via EF Core +
Npgsql — preferir `xmin` a `RowVersion byte[]` manual, a menos que haja motivo específico pra
portabilidade cross-database.
**Exemplo:** `Agendamento` em `Scheduling.Domain`, configurado em
`SchedulingDbContext.OnModelCreating`.

## `IEntityTypeConfiguration` com dependência injetada — aplicada manualmente, fora de `ApplyConfigurationsFromAssembly`

**Camada/área:** Infrastructure
**Definido por:** Dev Backend (task docs/tasks/005-prontuario-eletronico.md)
**Descrição:** `ApplyConfigurationsFromAssembly` só consegue instanciar `IEntityTypeConfiguration<T>`
com construtor sem parâmetro (usa `Activator.CreateInstance` via reflection). Quando uma
configuration precisa de uma dependência de verdade (ex: `EvolucaoClinicaConfiguration` precisa
de `IEncryptionService` pra cifrar `DescricaoClinica` via `HasConversion`), ela NÃO pode ser
descoberta pelo scan automático — é instanciada manualmente e aplicada com
`modelBuilder.ApplyConfiguration(new EvolucaoClinicaConfiguration(_encryptionService))` dentro do
próprio `OnModelCreating`, junto (não em vez) das demais configurations do módulo, que continuam
usando `ApplyConfigurationsFromAssembly` normalmente.
**Quando usar:** Toda vez que uma `IEntityTypeConfiguration` precisa de um serviço externo pra
montar um `ValueConverter` (criptografia, hashing, serialização customizada).
**Exemplo:** `Records.Infrastructure.Persistence.Configurations.EvolucaoClinicaConfiguration`,
aplicada em `RecordsDbContext.OnModelCreating`.

## Criptografia de campo em repouso via `ValueConverter`, chave derivada por hash

**Camada/área:** Infrastructure
**Definido por:** Dev Backend (task docs/tasks/005-prontuario-eletronico.md)
**Descrição:** Campo clínico sensível (`EvolucaoClinica.DescricaoClinica`) fica em texto puro no
Domain — a cifra (AES-256-GCM) só acontece na fronteira com o banco, via `HasConversion` na
`IEntityTypeConfiguration`. `IEncryptionService`/`AesEncryptionService` deriva os 32 bytes reais
da chave configurada via SHA-256 (`Encoding.UTF8.GetBytes` + hash), então o valor de config pode
ter qualquer tamanho sem estourar `ArgumentException` de "chave com tamanho errado". Formato
persistido: base64 de `nonce(12) || tag(16) || ciphertext`. Serviço é stateless → registrado como
Singleton, seguro mesmo sendo usado dentro de `OnModelCreating` (roda uma vez por processo, não
por request).
**Quando usar:** Todo campo de texto livre que carrega dado sensível (clínico, financeiro,
documento pessoal) e precisa de criptografia em repouso além do TLS em trânsito.
**Exemplo:** `Records.Infrastructure.Security.AesEncryptionService`, consumido por
`EvolucaoClinicaConfiguration`.

## Trilha de auditoria append-only com `SaveChangesAsync` próprio, separado da operação principal

**Camada/área:** Application | Infrastructure
**Definido por:** Dev Backend (task docs/tasks/005-prontuario-eletronico.md)
**Descrição:** `IAuditLogWriter.LogAsync` chama seu PRÓPRIO `SaveChangesAsync` no mesmo
`DbContext` da request, DEPOIS que a operação principal (criar prontuário, adicionar evolução,
etc.) já commitou. Cada chamada a `SaveChangesAsync` sem `BeginTransaction` explícito envolvendo
as duas é sua própria transação implícita no Postgres — isso cumpre o requisito de "auditoria
independente do fluxo normal de dados" sem precisar de Outbox/worker externo: se algo falhar
DEPOIS do log (ex: serialização da resposta HTTP), o rastro já está persistido. A entidade de
auditoria (`RecordsAuditLog`) não tem NENHUM método de mutação — só o factory `Registrar` — e o
repositório só expõe `AddAsync`, nunca update/delete.
**Quando usar:** Toda trilha de auditoria/compliance onde perder o registro do acesso é
inaceitável, mesmo que a resposta ao cliente falhe depois. Nota: isso NÃO substitui o padrão de
Outbox pra eventos que precisam cruzar módulo/processo (ver `ConsultaConcluidaEvent`,
task 004) — são dois problemas diferentes (persistir localmente vs. publicar externamente).
**Exemplo:** `Records.Infrastructure.Audit.AuditLogWriter`, consumido por todo handler que lê ou
escreve prontuário.

## Anti-Corruption Layer via porta genérica + implementação de referência sem integração real

**Camada/área:** Application (porta) | Infrastructure (adapter)
**Definido por:** Broker (task docs/tasks/007-financeiro-convenios.md)
**Descrição:** `IConvenioAdapter` (Billing.Application) define o contrato genérico que todo
adaptador de convênio implementa (`EnviarFaturaAsync(Fatura, Convenio) → Result<string>`,
devolve só um protocolo genérico — o formato externo real de cada operadora nunca aparece na
assinatura). `ManualConvenioAdapter` (Billing.Infrastructure) é uma implementação de REFERÊNCIA
sem integração com nenhuma operadora real — gera um protocolo local e loga a intenção de envio.
Isso permite construir e testar TODO o resto do fluxo (persistência, idempotência, RBAC,
cálculo de comissão) antes de qualquer credencial/API de convênio existir. Trocar por integração
real é mudança isolada no DI (`Billing.Infrastructure.DependencyInjection`), sem tocar
Application/Domain — é o próprio propósito da ACL.
**Quando usar:** Toda integração com sistema externo cujo formato/protocolo é instável, variado
por parceiro, ou ainda não definido — não esperar a integração real existir pra poder testar o
resto do fluxo.
**Exemplo:** `Billing.Application.Interfaces.IConvenioAdapter`,
`Billing.Infrastructure.Adapters.ManualConvenioAdapter`.

## Índice único PARCIAL (`HasFilter`) como garantia de idempotência de evento

**Camada/área:** Infrastructure
**Definido por:** Broker (task docs/tasks/006-financeiro-particular.md)
**Descrição:** `CreateFaturaFromConsultaConcluidaCommand` precisa ser seguro contra reentrega de
mensagem (garantia at-least-once de fila) — processar o mesmo `AgendamentoId` duas vezes não
pode gerar duas faturas. Checar `ExistsByAgendamentoIdAsync` na Application é a primeira linha de
defesa, mas sozinha tem a mesma janela de corrida do TOCTOU já documentado (duas entregas
concorrentes passam no check antes de qualquer uma commitar). A garantia real é um índice único
PARCIAL no Postgres: `HasIndex(f => f.AgendamentoId).IsUnique().HasFilter("\"AgendamentoId\" IS
NOT NULL")` — único quando não-nulo, permitindo múltiplas faturas particulares avulsas
(`AgendamentoId == null`) sem violar a constraint. O handler trata a exception resultante como
SUCESSO (idempotência), não como erro — reentrega de mensagem processada duas vezes não deveria
aparecer como falha pro consumidor.
**Quando usar:** Toda operação que precisa ser idempotente em relação a uma chave de correlação
que é opcional na maioria dos registros mas exclusiva quando presente (evento processado no
máximo uma vez, mas nem todo registro vem de um evento).
**Exemplo:** `Billing.Infrastructure.Persistence.Configurations.FaturaConfiguration`,
`CreateFaturaFromConsultaConcluidaCommandHandler`.

## Coleção encapsulada (`IReadOnlyList<T>` + campo privado): mapear pela PROPRIEDADE, nunca pelo nome do campo

**Camada/área:** Infrastructure
**Definido por:** Broker (bug real corrigido — task docs/tasks/008-inteligencia-bi.md, ver
docs/knowledge/errors-aprendidos.md)
**Descrição:** Quando um agregado expõe uma coleção de OUTRA entidade só-leitura (ex:
`Fatura.Parcelas : IReadOnlyList<Parcela>`, backing field privado `_parcelas`), a configuração
EF Core correta é `builder.HasMany(f => f.Parcelas).WithOne()...` (a expressão lambda da
propriedade PÚBLICA) seguida de `builder.Navigation(f => f.Parcelas)
.UsePropertyAccessMode(PropertyAccessMode.Field)` — isso diz ao EF "a navegação é
`Parcelas`, mas leia/escreva via o campo por baixo". Configurar via `HasMany<Parcela>("_parcelas")`
(nome do campo como string) parece equivalente mas NÃO é — o EF Core auto-descobre a propriedade
pública como navegação por convenção E a configuração manual do campo cria uma SEGUNDA navegação
apontando pro mesmo campo, gerando `InvalidOperationException` só quando o Model é validado (na
primeira query real, nunca em teste que só mocka o repositório).
**Diferente de:** campo/coleção ESCALAR (ex: `Dictionary<int,StatusDente>` do
`Prontuario.Odontograma`, mapeado como JSONB) — aí sim `builder.Property<T>("_nomeDoCampo")` é
correto, porque não é navegação pra outra entidade, é uma coluna simples.
**Quando usar:** Toda vez que um agregado tem uma coleção de entidade filha exposta só como
`IReadOnlyList<T>`/`IReadOnlyCollection<T>` (sem setter público).
**Exemplo:** `Billing.Infrastructure.Persistence.Configurations.FaturaConfiguration` (correto,
depois do fix); `Include(f => f.Parcelas)` (não mais `Include("_parcelas")`) em
`FaturaRepository`/`FaturamentoSummaryProvider`.

## Todo módulo com DbContext precisa de ao menos um teste que construa o Model de verdade

**Camada/área:** Testes
**Definido por:** Broker (task docs/tasks/008-inteligencia-bi.md)
**Descrição:** Testes de handler que só mockam `I{Entidade}Repository` (`Mock<T>`) NUNCA
instanciam o `DbContext` real — o EF Core só valida o Model (detecta configuração de navegação
inválida, conflito de campo, etc.) na primeira vez que o Model é efetivamente construído, o que
só acontece com uma instância real de `DbContext` executando uma query. Um módulo pode ter 100%
dos testes de handler passando e ainda assim quebrar toda leitura/escrita em produção. Foi assim
que o bug de `Fatura.Parcelas` (ver errors-aprendidos.md) ficou invisível por 3 tasks inteiras.
**Quando usar:** Todo módulo fecha com pelo menos um teste no estilo
`TenantQueryFilterTests`/`{X}SummaryProviderTests` — `UseInMemoryDatabase` real, sem mock do
repositório, exercitando o `DbContext` de ponta a ponta.
**Exemplo:** `tests/Patients.UnitTests/Persistence/TenantQueryFilterTests.cs`,
`tests/Billing.UnitTests/Persistence/FaturamentoSummaryProviderTests.cs`.

## Controller separado quando `[Authorize(Roles=...)]` de classe existente bloquearia role nova por AND cumulativo

**Camada/área:** Presentation
**Definido por:** Tech Lead + QA (task docs/tasks/023-query-comissoes-endpoint-rbac.md, Sprint 7,
decisão D4)
**Descrição:** Em ASP.NET Core, múltiplos atributos `[Authorize]` (um na classe, outro no método)
são cumulativos — avaliados em AND, não em OR. Se um controller já tem `[Authorize(Roles =
"Admin")]` na classe (ex: `ReportsController`) e uma rota nova precisa liberar acesso a um role
que a classe não inclui (ex: `Dentista`, pra visão de ownership da própria comissão), adicionar
`[Authorize(Roles = "Dentista")]` só no método NÃO afrouxa a exigência da classe — o `Dentista`
continua bloqueado (precisaria ser Admin E Dentista ao mesmo tempo, impossível). A solução testada
e confirmada pelo QA foi criar um controller novo (`ComissoesController`, rota própria) com sua
própria policy de `[Authorize(Roles = "Owner,Admin,Dentista", ...)]`, em vez de mexer no
`[Authorize]` de classe do controller existente — evita regressão nos endpoints Admin-only já
protegidos por ele.
**Quando usar:** Toda vez que uma rota nova precisa de um conjunto de roles que não é subconjunto
do `[Authorize]` de classe de um controller já existente — não adicionar `[Authorize]` no método
esperando "OR"; criar controller novo com a policy correta, ou revisar o `[Authorize]` de classe
inteiro conscientemente (nunca por acidente).
**Exemplo:** `ComissoesController` (`api/reports/comissoes`, `Owner,Admin,Dentista`) separado de
`ReportsController` (`Admin`-only na classe) — ver `docs/tasks/023-query-comissoes-endpoint-rbac.md`
(D4), QA confirmou via `[Authorize]` declarativo (linha 21) que `Recepcao` recebe 403 automático
por não estar na lista.

## Regime de caixa: cálculo por unidade paga (parcela), nunca pelo total do documento pai (fatura)

**Camada/área:** Domain
**Definido por:** Tech Lead + Architect (task docs/tasks/022-contratos-leitura-comissao.md, Sprint
7, decisão D2/A1)
**Descrição:** Quando uma regra de negócio distingue regime de CAIXA (o que foi efetivamente pago)
de regime de COMPETÊNCIA (o valor total do documento, independente de pagamento), calcular sobre o
total do documento pai é sempre errado nesse contexto — um documento com 3 parcelas e só 1 paga não
pode gerar o mesmo resultado que um documento inteiramente pago. `Fatura.CalcularValorComissao()`
já existente é regime de competência (usa `ValorTotal`); o método novo
`Fatura.CalcularComissaoSobre(decimal valorPago)` recebe o valor da PARCELA paga como parâmetro e é
chamado uma vez por parcela paga no período, com a soma agregada depois — nunca `ValorTotal *
percentual` direto. Mesmo arredondamento do método de competência (`Math.Round`, 2 casas,
`MidpointRounding.ToEven`), método público em Domain (é regra de negócio pura, não consulta).
**Quando usar:** Toda vez que uma feature nova precisa somar "o que foi pago num período" a partir
de um agregado que tem sub-itens de pagamento parcial (parcela, tranche, installment) — a unidade
de cálculo é sempre o sub-item pago, nunca o total do agregado pai, mesmo que o agregado pai já
tenha um método de cálculo pronto que pareça servir.
**Exemplo:** `Fatura.CalcularComissaoSobre(decimal valorPago)` em `Billing.Domain/Entities/Fatura.cs`,
consumido por `ComissaoSummaryProvider.ObterComissoesAsync` — chamado por `Parcela.ValorParcela`
de cada parcela `Paga` no período, nunca por `Fatura.ValorTotal`. Ver
`docs/tasks/022-contratos-leitura-comissao.md`.

## RBAC de ownership resolvido em memória com lista já carregada, sem porta nova de leitura

**Camada/área:** Presentation | Application
**Definido por:** Dev Backend (task docs/tasks/023-query-comissoes-endpoint-rbac.md, Sprint 7),
confirmado por QA
**Descrição:** Quando o Architect especifica uma porta de leitura nova só pra resolver um
`UserId → EntidadeId` único (ex: `IProfissionalLookup.ObterPorUserIdAsync`, A4 da task 023), mas
já existe uma porta batch que devolve a lista inteira da organization num único round-trip (ex:
`IProfissionalLookup.ListarPorOrganizationAsync`), resolver o `UserId → EntidadeId` com um
`.FirstOrDefault(x => x.UserId == userId)` em memória sobre a lista já carregada é funcionalmente
equivalente (mesmo isolamento cross-org, sem N+1, mesmo round-trip único) e evita porta nova sem
necessidade real de performance. O Dev Backend sinalizou a discrepância formal com a spec do
Architect (não implementou a sobrecarga pedida) em vez de decidir sozinho reabrir a decisão — QA
confirmou correção funcional e marcou como risco residual não-bloqueante, pendente só de
ratificação formal se o Architect/PO quiserem alinhar a spec ao código.
**Quando usar:** Toda vez que a porta batch já carrega em memória tudo que a porta "unitária"
pediria de novo — antes de implementar a porta nova especificada, verificar se um filtro em
memória sobre a lista já obtida resolve sem round-trip extra; se resolver, sinalizar a
discrepância com a spec explicitamente (não decidir sozinho, não implementar por conta própria),
deixando a ratificação formal para Architect/PO.
**Exemplo:** `ComissoesController` resolve `UserId → ProfissionalId` via
`IProfissionalLookup.ListarPorOrganizationAsync().FirstOrDefault(p => p.UserId == userId)`, sem a
sobrecarga `ObterPorUserIdAsync` especificada em A4 — ver "Discrepância handoff x spec" em
`docs/tasks/022-contratos-leitura-comissao.md` e risco residual #2 em
`docs/tasks/023-query-comissoes-endpoint-rbac.md`.

## Tokens de design semânticos: nomear pra evitar colisão com prefixo de utilitária Tailwind

**Camada/área:** UI
**Definido por:** Designer (task docs/tasks/024-design-system-verde-dark-light.md, Sprint 7)
**Descrição:** Em Tailwind v4, uma variável de tema `--color-text-primary` gera a utilitária
`text-text-primary` — o prefixo da propriedade (`text-`) colide com o nome do próprio token
(`text-primary`), produzindo classe redundante/visualmente confusa espalhada por toda a base de
código. A spec original pedia tokens nomeados `text-primary`/`text-secondary`/`text-muted`; o
Designer renomeou pra `ink`/`ink-secondary`/`ink-muted` (mesma função semântica, sem a duplicação
de prefixo), gerando `text-ink`/`text-ink-secondary`/`text-ink-muted` como utilitárias finais.
Decisão de ergonomia de sintaxe, não de aprovação de produto — mapeamento 1:1 documentado.
**Quando usar:** Ao nomear qualquer token semântico do Tailwind v4 que vai virar prefixo de
propriedade (`text-*`, `bg-*`, `border-*`, `outline-*`) — evitar que o nome do token comece com a
mesma palavra do prefixo da utilitária que ele vai gerar; preferir um nome curto e distinto
(`ink`, não `text-primary`; padrão equivalente pra `border-*`/`bg-*` se a mesma colisão aparecer).
**Exemplo:** `--color-ink` → `text-ink` (não `text-text-primary`), ver
`docs/design/design-system.md` §0.2 e tabela §1.1.

## Dark mode via `.dark` class + `@custom-variant` + script anti-FOUC inline no `index.html`

**Camada/área:** UI | Infrastructure (bootstrap do frontend)
**Definido por:** Architect (A5) + Dev Frontend (task docs/tasks/025-frontend-tokens-tema-dark-light.md,
Sprint 7)
**Descrição:** Tailwind v4 tem suporte nativo a `@custom-variant dark { @media
(prefers-color-scheme: dark) }` em `index.css`, permitindo toggle manual sem precisar de
`data-theme` como atributo separado — a classe `.dark` no `<html>` já é o gatilho. Estado
`light|dark|system` é gerido por um `ThemeProvider` (Zustand, persistido em `localStorage`); a
aplicação da classe no `<html>` **antes** do React montar é feita por um script inline colado
direto no `index.html` (fora do bundle React) — sem esse script, a primeira pintura da tela usa o
tema default até o React hidratar e aplicar a classe certa, causando flash de tema errado (FOUC).
Sem escolha salva em `localStorage`, o fallback é `@media (prefers-color-scheme: dark)` do SO.
**Quando usar:** Toda implementação de dark mode em Tailwind v4 que precisa (a) permitir toggle
manual do usuário, (b) respeitar preferência do SO por default, e (c) não piscar tema errado no
primeiro paint — os três exigem que a decisão de classe aconteça antes do JS do framework rodar,
não depois.
**Exemplo:** Script inline em `frontend/index.html` (antes do `<div id="root">`), `ThemeProvider`
em `frontend/src/` (Zustand + localStorage), bloco `@custom-variant dark` em
`frontend/src/index.css` — ver `docs/tasks/025-frontend-tokens-tema-dark-light.md` (A5) e
`docs/decisions.md`.

## RBAC de escopo (unidade/recurso) forçado pelo controller, nunca aceito como input

**Camada/área:** Presentation
**Definido por:** Broker (task docs/tasks/009-rede-multi-unidade.md)
**Descrição:** Quando um usuário deve enxergar só um subconjunto de dados (ex: Recepcao só a
própria unidade), o filtro correspondente é decidido pelo CONTROLLER a partir de
`ICurrentUserAccessor` (claim do JWT) e passado pro Query/Command — NUNCA aceito como parâmetro
vindo do cliente (query string/body). Se fosse aceito do cliente, bastaria omitir o parâmetro
(ou mandar outro valor) pra escapar da restrição. Mesmo padrão já usado em
`CreateProntuarioCommand`/`CreatePatientCommand` (TenantId sempre do token, nunca do body) —
aqui generalizado pra qualquer nível de escopo abaixo do tenant.
**Quando usar:** Todo filtro de autorização que reduz o que um papel específico pode ver/mexer
além do tenant (unidade, recurso próprio, etc.).
**Exemplo:** `SchedulingController.List` — `var unidadeId = _currentUser.Role == "Recepcao" ?
_currentUser.UnidadeId : null;` antes de montar `ListAgendamentosQuery`.

## Módulo novo nasce com teste de infraestrutura real desde o primeiro commit

**Camada/área:** Testes
**Definido por:** Broker (task docs/tasks/009-rede-multi-unidade.md, aplicando a lição da 008)
**Descrição:** Depois do bug de `Fatura.Parcelas` (docs/knowledge/errors-aprendidos.md) ter
ficado invisível por 3 tasks porque nenhum teste instanciava o `DbContext` de verdade, todo
módulo novo a partir da task 009 (Tenancy, Estoque) já nasce com pelo menos um teste
`UseInMemoryDatabase` real (não mock) desde o primeiro commit, não como reação a um bug
encontrado depois. Um desses testes (Estoque) achou um bug NO PRÓPRIO TESTE — não no código —
justamente porque o teste existia desde o início.
**Quando usar:** Todo módulo com `DbContext` — não esperar acumular tasks pra só então
adicionar o teste de infraestrutura.
**Exemplo:** `tests/Tenancy.UnitTests/Persistence/TenantQueryFilterTests.cs`,
`tests/Estoque.UnitTests/Persistence/TenantQueryFilterTests.cs`.

## Rename mecânico em massa: script multi-pass por casing + proteção de falso-cognato via placeholder

**Camada/área:** Infrastructure | ferramenta/processo (todo o código-fonte)
**Definido por:** Dev Backend (tasks docs/tasks/010-rename-tenant-organization.md,
docs/tasks/011-rename-unidade-branch.md)
**Descrição:** Pra renomear um conceito de domínio inteiro (ex: `Tenant`→`Organization`,
`Unidade`→`Branch`) em centenas de arquivos, o rename é feito por SCRIPT (`sed`), não por edição
arquivo-a-arquivo, em **3 passes por casing** na ordem certa — cada casing tem que ser
substituído separadamente porque `sed` não faz match case-preserving sozinho:
1. `TENANT`→`ORGANIZATION` (tudo maiúsculo — constantes, nomes de coluna literais)
2. `Tenant`→`Organization` (PascalCase/TitleCase — classes, propriedades, namespaces)
3. `tenant`→`organization` (lowercase — variáveis locais, JSON camelCase, comentários)
Quando o termo tem plural em inglês com forma irregular (`Unidade`→`Branch`,
`Unidades`→`Branches`, não `Branchs`), o **plural precisa ser substituído ANTES do singular** no
script — senão o replace do singular consome o prefixo e o "s" sobra solto, gerando o plural
errado por concatenação simples (`Branch`+`s` = `Branchs`, quando o correto exige o `-es`).
**Proteção de falso-cognato:** antes de qualquer passe de substituição, rodar
`grep -rohiE "[a-zA-Z_]*{palavra}[a-zA-Z_]*" | sort -u` pra listar TODO token que CONTÉM a
palavra-alvo (não só a palavra isolada). Revisar essa lista manualmente procurando conceitos que
só coincidem na substring mas são semanticamente diferentes (ex: `UnidadeMedida` — "unidade de
medida" de estoque como kg/un/cx — não tem nada a ver com `Unidade`/Branch, a clínica física).
Cada token falso-cognato identificado é protegido com um TOKEN PLACEHOLDER temporário antes de
rodar o script de substituição, e restaurado ao valor original depois. Texto livre com espaço
(ex: mensagem de erro `"Unidade de medida é obrigatória."`) escapa da proteção automática de
identificador colado e precisa de correção manual pontual — revisar strings literais além de
identificadores.
**Validação pós-rename:** `dotnet build` (0 erro/0 aviso) + `dotnet test` (mesmo número de testes
verdes que o baseline pré-rename, confirmando zero regressão funcional) + grep final
case-insensitive pela palavra antiga em `src/`+`tests/`, esperando encontrar SÓ os falsos-cognatos
já mapeados (nada mais).
**Quando usar:** Todo rename de conceito de domínio que atravessa múltiplos módulos/camadas —
antes de rodar qualquer substituição em massa, mapear falsos-cognatos primeiro (grep de token
contendo a palavra-alvo), proteger via placeholder, só então rodar os 3 passes por casing
(maiúsculo → PascalCase → lowercase), com plural antes de singular quando aplicável.
**Exemplo:** rename `Tenant`→`Organization` (228 arquivos, task 010) e `Unidade`→`Branch` (task
011, achado do falso-cognato `ItemEstoque.UnidadeMedida` em `Estoque.Domain`) — ver também
`docs/knowledge/errors-aprendidos.md` ("falso-cognato em rename de substring pode trocar conceito
sem gerar erro de build").

## Afiliação N:N (entidade tenant-scoped) substitui FK direta quando a cardinalidade multi-tenant por usuário existe

**Camada/área:** Domain | Infrastructure
**Definido por:** Architect + Dev Backend (task docs/tasks/013-organization-membership-nn.md)
**Descrição:** Quando um agregado que antes carregava uma FK direta pra organização/tenant
(`User.OrganizationId`, 1:1) precisa passar a suportar N organizações por instância (um mesmo
usuário afiliado a 2+ clínicas), a solução não é tornar a FK uma lista — é (1) o agregado original
(`User`) vira GLOBAL, deixando de implementar `IMustHaveOrganization` e saindo do filtro de query
por tenant; (2) nasce uma entidade nova de afiliação (`OrganizationMembership`) que É
tenant-scoped (implementa `IMustHaveOrganization`), carrega `(UserId, OrganizationId, Role,
Status)` com índice único `(OrganizationId, UserId)`, e é ela — não o agregado original — quem
aparece no filtro global de isolamento. Todo dado que antes vivia 1:1 no agregado global e é
na verdade escopado por afiliação (ex: `Role`, `BranchId`) migra junto para a entidade de
afiliação. `IMustHaveOrganization` continua sendo aplicado por reflection sobre o Model inteiro
(mesmo mecanismo do padrão "Filtro global de tenant genérico por `TContext`" já documentado) —
a entidade nova cai no filtro automaticamente, sem código extra no `OnModelCreating` além da
implementação da interface.
**Quando usar:** Toda vez que um agregado que hoje é 1:1 com o tenant precisa virar N:N (mesmo
usuário/recurso pertencendo a múltiplos tenants/organizações simultaneamente) — não adicionar
lista/coleção de tenant no agregado original; extrair uma entidade de afiliação tenant-scoped
própria, e mover pro agregado original só o que é verdadeiramente global (identidade, não
pertencimento).
**Exemplo:** `Identity.Domain.Entities.User` (perdeu `OrganizationId`/`Role`, virou global) +
`Identity.Domain.Entities.OrganizationMembership` (nova, tenant-scoped, `(OrganizationId, UserId)`
único) — ver `docs/tasks/013-organization-membership-nn.md`.

## Policy nomeada com `RequireClaim`, fail-closed by construction, pra distinguir endpoint que exige escopo ativo de endpoint que tolera token "incompleto"

**Camada/área:** Presentation
**Definido por:** Architect + Dev Backend (task docs/tasks/014-auth-multi-org.md)
**Descrição:** Quando um token JWT pode legitimamente ser emitido sem uma claim de escopo (ex:
usuário recém-criado ainda sem organização, `organization_id` ausente por design — não vazio,
AUSENTE mesmo), a distinção entre "endpoint que precisa do escopo" e "endpoint que funciona sem
ele" (`GET /api/me`, `POST /api/organizations`, aceitar convite) não é feita por `if` espalhado
em cada handler — é uma policy nomeada registrada uma vez em `Program.cs`
(`.AddPolicy("RequireActiveOrganization", p => p.RequireClaim("organization_id"))`), usando o
requirement built-in do ASP.NET Core (`ClaimsAuthorizationRequirement`), aplicada via
`[Authorize(Policy = "RequireActiveOrganization")]` em todo controller de negócio. É fail-closed
por construção: claim ausente ou principal não autenticado sempre resulta em falha de
autorização — não existe handler customizado que possa short-circuitar pra sucesso. O emissor do
token reforça a mesma regra do lado oposto: `JwtTokenService` explicitamente NÃO adiciona a claim
quando o valor é null (não manda vazio/zero — omite a claim), pra `RequireClaim` de fato falhar.
Aplicada em todos os controllers de domínio (Agenda, Pacientes, Financeiro, Estoque, Prontuário,
Relatórios, Profissionais, Salas, Convênios, Filiais, Usuários); deliberadamente NÃO aplicada em
`AuthController` (login/refresh são `[AllowAnonymous]`; `switch-organization` é só `[Authorize]`
sem a policy, porque é justamente o endpoint que tira o usuário do estado "sem org").
**Quando usar:** Toda vez que uma claim de escopo (organização, unidade, recurso) pode
legitimamente estar ausente do token em certos estados de usuário, e um subconjunto de endpoints
precisa continuar acessível nesse estado — nomear a policy pelo que ela exige (não pelo endpoint),
registrar uma vez, aplicar seletivamente; nunca checagem ad-hoc de claim dentro do handler.
**Exemplo:** `RequireActiveOrganization` em `Program.cs`, aplicada via `[Authorize(Policy = ...)]`
nos 11 controllers de domínio — ver `docs/tasks/014-auth-multi-org.md`.

## Teste de infraestrutura em módulo com campo cifrado via `ValueConverter`: reusar o serviço de criptografia REAL com options de teste, nunca mockar `IEncryptionService`

**Camada/área:** Testes
**Definido por:** QA (task docs/tasks/019-qa-regressao-multi-org.md)
**Descrição:** O padrão "Todo módulo com DbContext precisa de ao menos um teste que construa o
Model de verdade" (já documentado acima) exige `UseInMemoryDatabase` real sem mock de
repositório — mas quando a `IEntityTypeConfiguration` do módulo usa `HasConversion` com um
`ValueConverter` que depende de um serviço injetado (ex: `EvolucaoClinicaConfiguration` +
`IEncryptionService`, ver padrão "Criptografia de campo em repouso via `ValueConverter`"), mockar
esse serviço (`Mock<IEncryptionService>`) pra construir o `DbContext` de teste é o caminho errado:
o `ValueConverter` só é exercitado de verdade (serialização + deserialização ida-e-volta) se o
serviço real rodar. A técnica correta é instanciar o serviço de criptografia REAL
(`AesEncryptionService`) com uma instância de options de TESTE (`RecordsEncryptionOptions` com
uma chave fixa de teste, não a de produção) — o mesmo padrão já usado em
`AesEncryptionServiceTests` (unitário do serviço isolado) — em vez de inventar um stub/fake novo
pro `IEncryptionService` só para os testes de filtro de query. Isso era o bloqueio que tinha
impedido a sessão anterior de fechar cobertura de `OrganizationQueryFilterTests` em `Records`;
resolvido nesta task instanciando `AesEncryptionService` de verdade no construtor da classe de
teste.
**Quando usar:** Todo teste de infraestrutura (`UseInMemoryDatabase` real) de um módulo cuja
`IEntityTypeConfiguration` usa `HasConversion` com `ValueConverter` dependente de serviço
injetado (criptografia, hashing, serialização customizada) — instanciar o serviço real com
options de teste, nunca mockar a interface só pra fazer o `DbContext` compilar/rodar.
**Exemplo:** `tests/Records.UnitTests/Persistence/OrganizationQueryFilterTests.cs` — instancia
`AesEncryptionService` real com `RecordsEncryptionOptions` de teste, mesmo padrão de
`AesEncryptionServiceTests`, cobrindo `EvolucaoClinica` (campo cifrado) junto com `Prontuario` e
`AnexoMetadata` (campos não-cifrados) no mesmo conjunto de testes de filtro de isolamento.

## IDOR fechado: `{id}` de rota sempre validado contra a claim do token, mesmo com RBAC de role correto

**Camada/área:** Presentation
**Definido por:** Dev Backend (task docs/tasks/016-invite-afiliacao.md, achado em auto-revisão,
confirmado pelo QA)
**Descrição:** `[Authorize(Roles = "Owner,Admin")]` (ou uma policy de role equivalente) sozinho
responde só "este usuário é Owner/Admin de ALGUMA organização" — não "é Owner/Admin DESTA
organização específica que aparece na URL". Quando um endpoint recebe um `{id}` de organização (ou
qualquer recurso escopado) na rota, isso não basta: um Owner da organização A conseguiria escrever
num recurso da organização B só trocando o guid na URL, mesmo passando a checagem de role. O fix é
uma linha explícita no início do action, comparando o `{id}` da rota contra o `organization_id` do
token: `if (_currentUser.OrganizationId != id) return Forbid();`. Isso é uma checagem DIFERENTE e
COMPLEMENTAR à policy `RequireActiveOrganization` (que só garante que a claim existe, não que ela
bate com o recurso pedido) e ao filtro global de query por tenant (que protege LEITURA de recurso
que já foi resolvido pelo id do token, não recurso cujo id vem da própria URL).
**Quando usar:** Todo endpoint que recebe um `{id}` de organização/tenant (ou de qualquer entidade
que representa o próprio escopo de isolamento) diretamente na rota — nunca confiar que a policy de
role, sozinha, impede acesso cross-tenant; validar explicitamente `{id da rota} == {escopo do
token}` antes de qualquer operação de escrita ou leitura sensível.
**Exemplo:** `OrganizationsController.CreateInvite`/`GetInvites` —
`if (_currentUser.OrganizationId is null || _currentUser.OrganizationId != id) return Forbid();`
(linhas 65-66, 90-91). Mesma classe de bug (IDOR cross-tenant por id de rota não validado contra o
dono real) já registrada em sessão anterior de outro projeto (Harpia.WEB) — reincidência conhecida
do Dev Backend, por isso vira padrão explícito aqui em vez de ficar só em memória individual.

## Widening de RBAC (papel novo tipo Owner) precisa varrer TODAS as camadas de gate, não só o backend

**Camada/área:** Presentation | UI
**Definido por:** QA (task docs/tasks/030-owner-role-parity-rbac.md, docs/tasks/027-frontend-dashboard-filtros-filial-classe.md,
docs/tasks/028-frontend-comprovante-pagamento.md, Sprint 7), achado incremental em 2 rodadas de
revalidação
**Descrição:** Quando um papel novo (ou uma role existente ganha acesso novo, ex: `Owner` virando
superset de `Admin`) precisa passar a acessar algo que antes só `Admin` acessava, existem
tipicamente QUATRO camadas de gate independentes que verificam a mesma coisa de formas diferentes,
e corrigir só uma NÃO corrige as outras:
1. Backend — `[Authorize(Roles = "...")]` em controller (classe e/ou método, cumulativos em AND).
2. Frontend rota — `<ProtectedRoute roles={[...]}>` no `App.tsx`, decide se a rota nem monta.
3. Frontend menu — item da sidebar (`AppLayout`) com seu próprio `roles: [...]`, decide se o link
   aparece; independente da rota (usuário pode ter a rota liberada e o menu escondido, ou vice-versa).
4. Frontend componente — condicional inline dentro da tela (`role === 'Admin'`, `isAdmin`,
   `canManage`, etc.) que gate um bloco/filtro/ação específica, ex: `PatientsPage.canManage`,
   `ReportsPage.isEmpresa`, `ComprovantePage`, `FinanceiroPage` → `ConveniosCard`.
Nesta sprint, o bug "Owner sem acesso" apareceu nas 4 camadas: a task 030 corrigiu só a camada 1
(backend), achando que resolvia o problema por completo; o QA achou as camadas 2, 3 e 4 ainda
bloqueando Owner no frontend, em 2 rodadas de revalidação sucessivas (027 e 028), porque cada
fix pontual revelava a próxima camada ainda quebrada.
**Quando usar:** Toda vez que um papel ganha (ou perde) acesso a uma função administrativa —
antes de declarar o widening concluído, `grep` sistemático nas 4 camadas de uma vez (backend
`[Authorize(Roles=`, frontend `ProtectedRoute roles=`, `AppLayout` `roles:`, e qualquer
`role === '<Role>'`/`isX`/`canX` inline em componente), não uma camada de cada vez esperando o
QA achar a próxima.
**Exemplo:** `docs/tasks/030-owner-role-parity-rbac.md` (backend, 10 controllers) +
`docs/tasks/027-frontend-dashboard-filtros-filial-classe.md` (`App.tsx:65` rota `/relatorios`,
`AppLayout.tsx:63` menu "Relatórios", `ReportsPage.tsx` `isEmpresa`) +
`docs/tasks/028-frontend-comprovante-pagamento.md` (`ComprovantePage.tsx` `isAdmin`→`isEmpresa`) —
varredura final do QA (027, revalidação 2026-08-19) confirmou zero gate `Admin`-only isolado
remanescente nas 4 camadas.

## Widget de sidebar client-side: recebe dado já carregado da página-pai, nunca dispara request próprio

**Camada/área:** Presentation | UI
**Definido por:** Dev Frontend (task docs/tasks/031-agenda-redesign-referencia-visual.md, Sprint 8)
**Descrição:** Ao quebrar uma tela em widgets menores (ex.: `MiniCalendar`, `UpcomingAppointments`,
`DaySummary` na Agenda), cada widget recebe o array de dado já buscado pela página (`agendamentos:
Agendamento[]` via prop) e faz sua própria derivação (`useMemo`) em cima dele — nenhum widget tem
`useQuery` próprio. Evita 2 problemas: (1) N requests idênticos disparados por N widgets na mesma
tela lendo a mesma coleção; (2) widgets fora de sincronia entre si (ex.: "Resumo do dia" e
"Próximos agendamentos" mostrando dado de momentos diferentes se cada um tivesse seu próprio
`refetchInterval`/cache).
**Quando usar:** Sempre que uma tela ganha um layout tipo "dashboard" (card principal + sidebar de
mini-widgets) e os widgets consomem a MESMA entidade que a tela já busca — não crie
`useQuery` novo dentro do widget só porque ele mora em arquivo próprio. Só quebre esse padrão se o
widget precisar de um recorte de dado genuinamente diferente (período diferente, entidade
diferente) que a query da página não cobre.
**Exemplo:** `AgendaPage.tsx` busca `listAgendamentos` uma vez (`queryKey ['agendamentos']`) e
passa `agendamentos` como prop pra `MiniCalendar` (deriva `daysWithEvents`),
`UpcomingAppointments` (filtra futuro + ordena) e `DaySummary` (filtra por dia selecionado + soma
duração) — os 3 widgets, zero request próprio.

## Mapeamento enum→apresentação (tom/label/cor) centralizado por entidade, não duplicado por componente

**Camada/área:** Presentation | UI
**Definido por:** Dev Frontend (task docs/tasks/031-agenda-redesign-referencia-visual.md, Sprint 8)
**Descrição:** Quando um enum de domínio (`AgendamentoStatus`) precisa de mapeamento visual
(label, tom semântico `StatusTone`, classe de dot, cor de evento do FullCalendar) em mais de 1
componente, o mapeamento mora num módulo próprio (`features/<modulo>/status-display.ts`) exportado
como `const`, e todo componente que precisa dele importa de lá — nenhum componente redeclara o
`Record<Enum, string>` localmente. Antes da 031, esse mapeamento só existia dentro de
`StatusBadge.tsx` sem export; quando o chip do calendário/lista de próximos/legenda precisaram do
mesmo mapeamento, a alternativa errada seria copiar o `Record` de novo em cada lugar — exatamente o
tipo de duplicação que já causou o drift `StatusBadge`/`FaturaStatusBadge` documentado no
design-system.md §5.
**Quando usar:** No 2º componente que precisar do mesmo mapeamento enum→visual (não espera o 3º) —
mover pra módulo compartilhado e fazer os componentes existentes importarem de lá.
**Exemplo:** `features/scheduling/status-display.ts` (`statusLabels`, `statusTone`,
`statusDotClasses`, `statusEventColorVar`) — consumido por `StatusBadge.tsx` (badge),
`AgendaPage.tsx` (chip do evento + legenda), `UpcomingAppointments.tsx` (dot da lista).

## Sweep de cor hardcoded — comando de verificação reprodutível, não confiar em "acho que já migrei tudo"

**Camada/área:** UI
**Definido por:** QA (task docs/tasks/033-sweep-cor-hardcoded-verificacao.md, Sprint 11)
**Descrição:** Pra confirmar que nenhuma classe Tailwind de cor hardcoded (fora dos tokens
semânticos de `docs/design/design-system.md` §1.1) foi reintroduzida numa tela, rodar grep amplo
sobre toda a paleta padrão — não só as cores já catalogadas em §6. A lista de §6 (93 ocorrências)
foi um levantamento pontual da task 025; qualquer sweep posterior precisa cobrir a paleta inteira
de novo, porque um Dev pode introduzir uma cor que nunca apareceu no levantamento original (ex.:
`bg-orange-400` novo, que não estava nos 93 mapeados).
**Comando:**
```
grep -rnE '\b(bg|text|border|divide|outline|ring)-(slate|red|amber|emerald|blue|gray|zinc|neutral|green|yellow|orange)-\d{2,3}\b' frontend/src --include='*.tsx' --include='*.ts' --include='*.css'
```
Zero matches = pass. Hex literal (`#RRGGBB`) só é aceitável dentro de `index.css`, nas próprias
definições de `--color-*` (light/dark) — qualquer hex fora desse arquivo é hardcode disfarçado.
**Quando usar:** Fim de qualquer sprint/task que toque `frontend/src` visualmente, antes do QA dar
pass — roda em segundos, sem custo, e é a única forma barata de confirmar "zero cor hardcoded"
sem revisão visual tela por tela.
**Exemplo:** Sprint 11 (033) rodou esse grep após as sprints 8/9 (031 Agenda redesign, 032 dark
reskin) — 0 matches, confirmando que nenhuma das duas reintroduziu cor solta apesar de mexerem
bastante em `AppLayout.tsx`/`Button.tsx`/componentes novos (`MiniCalendar`, `UpcomingAppointments`,
`DaySummary`).

## Code-splitting por rota: `React.lazy` só na página, shell (`AppLayout`/`ProtectedRoute`) sempre eager

**Camada/área:** Presentation | UI
**Definido por:** Dev Frontend (task docs/tasks/034-code-splitting-rotas.md, Sprint 11)
**Descrição:** Quando `vite build` avisa bundle >500 kB, a divisão certa é por página de rota
(`App.tsx`), não por componente aleatório: cada `<Route element={<XPage />} />` vira
`const XPage = lazy(() => import('./features/.../XPage').then((m) => ({ default: m.XPage })))`,
e `<Routes>` inteiro entra num `<Suspense fallback={<RouteFallback />}>` único. O shell da
aplicação (`AppLayout`, `ProtectedRoute`, `RequireOrganization`) NUNCA vira lazy — ele precisa
estar pronto antes de qualquer rota resolver, senão a nav pisca/monta depois do conteúdo (layout
shift). O fallback do Suspense reusa o mesmo idioma de loading já usado dentro das páginas
(`text-sm text-ink-muted`), não inventa spinner novo.
**Quando usar:** No 1º aviso de `vite build` sobre chunk >500 kB — divisão por rota é sempre o
primeiro corte (barato, sem lib nova, sem risco de comportamento); só granularizar mais (lib
pesada dentro de 1 página só) se o bundle voltar a crescer depois do split por rota.
**Exemplo:** `App.tsx` — 10 páginas (`LoginPage`...`ComprovantePage`) viraram lazy; bundle
principal 792.90 kB → 250.79 kB. Maior chunk remanescente é `AgendaPage` (285.02 kB, por causa do
FullCalendar) — ficou como está por já estar abaixo do limite de aviso, sem split adicional.

## DateTime UTC em EF Core: convenção no `DbContext`, nunca `SpecifyKind` handler a handler

**Camada/área:** Infrastructure | Backend
**Definido por:** Broker (task docs/tasks/035-fix-datetime-utc-endpoints.md, Sprint 11) — ver
docs/knowledge/errors-aprendidos.md pro bug original.
**Descrição:** Todo `DateTime` que entra vindo de request body (model binding de JSON) chega com
`Kind=Unspecified`; Npgsql 6+ rejeita gravar isso numa coluna `timestamp with time zone`. Em vez
de `DateTime.SpecifyKind(x, DateTimeKind.Utc)` espalhado em cada `CommandHandler` que recebe uma
data do cliente, a correção mora uma vez em `Infrastructure.Common.Persistence
.UtcDateTimeConventionExtensions.ApplyUtcDateTimeConversion()` — dois `ValueConverter`
(`DateTime`/`DateTime?`) registrados via `ConfigureConventions(ModelConfigurationBuilder)`,
aplicados a TODA propriedade `DateTime` do modelo automaticamente, sem tocar em nenhuma
`IEntityTypeConfiguration` individual.
**Quando usar:** Todo `DbContext` novo do monólito precisa chamar
`configurationBuilder.ApplyUtcDateTimeConversion()` dentro do próprio `ConfigureConventions` —
mesma linha nos 7 existentes (Patients/Scheduling/Billing/Estoque/Identity/Records/Tenancy).
Nunca resolver caso a caso com `SpecifyKind` num handler: some sozinho e some o próximo campo
de data que alguém adicionar em qualquer módulo repete o mesmo 500.
**Exemplo:**
```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
    configurationBuilder.ApplyUtcDateTimeConversion();
```

## CTA de landing/marketing que navega: `<Link>` estilizado, nunca `<Link>` dentro de `<Button>`

**Camada/área:** Presentation | UI
**Definido por:** Dev Frontend (task docs/tasks/038-landing-page-publica.md, Sprint 11)
**Descrição:** `components/ui/Button.tsx` renderiza um `<button>` de verdade — colocar um
`<Link>`/`<a>` do react-router dentro dele é HTML inválido (elemento interativo dentro de
elemento interativo) e quebra semântica/acessibilidade. Onde o "botão" é navegação pura (CTA de
landing, "Assinar plano" etc.), o padrão é estilizar o próprio `<Link>` com as MESMAS classes
Tailwind do variant do `Button` (copiadas como constante local, ex.: `ctaPrimary`/`ctaSecondary`
em `LandingPage.tsx`) — não tentar fazer o `Button` aceitar `as`/`asChild` nem envolver o link
num botão.
**Quando usar:** Qualquer CTA que é 100% navegação (sem `onClick`/side-effect nenhum) — se o
elemento só faz `navigate()`, ele é um link estilizado, não um botão. Se precisar de `onClick`
real (mutation, side-effect) antes de navegar, aí sim é `Button` de verdade com `useNavigate()`
dentro do handler.
**Exemplo:** `features/marketing/LandingPage.tsx` — `ctaPrimary`/`ctaSecondary` (classes copiadas
de `Button.tsx` variant `primary`/`secondary`) aplicadas direto em `<Link>` no nav, hero e cards
de plano; zero `<Button>` na página inteira.

## Módulo novo cross-module: só `*.Contracts` referenciado por fora, nunca `*.Domain`/`*.Infrastructure`

**Camada/área:** Application | Architecture
**Definido por:** Architect (task docs/tasks/039-sistema-planos-onboarding-completo.md, Sprint 11)
**Descrição:** Quando um módulo novo precisa ser consultado por módulos já existentes (aqui:
`Subscriptions` por `Identity` — GetMe expõe o plano ativo — e por `Tenancy` — CreateBranch checa
limite de filial), a dependência entra SÓ no `*.Application.csproj` do módulo consumidor, apontando
pro `*.Contracts.csproj` do módulo novo (nunca `*.Domain`/`*.Infrastructure`) — mesma fronteira já
usada por `IBranchLookup`/`IPatientLookup`. A interface do lookup (`ISubscriptionLookup`) mora no
`Contracts`, a implementação real (`SubscriptionLookup`, EF Core) mora no `Infrastructure` do
módulo dono, registrada no `DependencyInjection.cs` dele — quem consome só injeta a interface.
**Quando usar:** Todo módulo novo que expõe estado pra outros módulos lerem (não escrever) — CRUD
completo fica dentro do módulo dono (Application/Infrastructure próprios), só a LEITURA
cross-module vira porta em Contracts.
**Exemplo:** `Tenancy.Application.csproj` ganhou `<ProjectReference>` só pra
`Subscriptions.Contracts.csproj`; `CreateBranchCommandHandler` injeta `ISubscriptionLookup`
(nunca viu `Subscriptions.Domain.Entities.Subscription` nem `SubscriptionsDbContext`).

## Esqueleto de integração externa: porta real + implementação que loga e devolve placeholder, nunca throw

**Camada/área:** Infrastructure | Backend
**Definido por:** Dev Backend (task docs/tasks/039-sistema-planos-onboarding-completo.md, Sprint 11)
**Descrição:** Quando o pedido é "conexão com X ainda não precisa funcionar mas o esqueleto tem
que existir" (aqui: Stripe), a porta (`IPaymentGatewayService`) e o Command/Handler que a usa
(`StartCheckoutCommand`) são construídos completos e reais — o único "fake" é a implementação de
Infrastructure (`StripePaymentGatewayService`), que NÃO instala o SDK do provedor nem faz nenhuma
chamada de rede: loga a intenção (`ILogger`) e devolve um valor de placeholder plausível (aqui,
uma URL fake + id gerado). Nunca lança `NotImplementedException` — isso quebraria qualquer teste
de integração/E2E que exercite o fluxo antes do dia em que o provedor real for conectado. Mesmo
padrão já usado em `IConvenioAdapter`/`ManualConvenioAdapter` (Billing, ver docs/decisions.md).
**Quando usar:** Todo pedido de "esqueleto"/"skeleton" de integração externa (gateway de
pagamento, provedor de email, serviço de terceiro qualquer) sem credencial/conta disponível ainda.
**Exemplo:** `Subscriptions.Infrastructure.Payments.StripePaymentGatewayService.CriarCheckoutSessionAsync`
— devolve `about:blank#stripe-checkout-esqueleto-{id}` e loga via `ILogger`, sem `Stripe.net` no
`.csproj`. Trocar por chamada real ao SDK não muda `StartCheckoutCommandHandler` nem o controller.

## Onboarding multi-passo: estado "como chegou" trava em `ref` na entrada, guard de atalho respeita o `step` local

**Camada/área:** Presentation | UI
**Definido por:** Dev Frontend (task docs/tasks/039-sistema-planos-onboarding-completo.md, Sprint 11)
**Descrição:** Um fluxo de N passos guiado por `useState<Step>` local, onde CADA passo pode mudar
o mesmo cache assíncrono (`useMe()`) que decide o passo seguinte, tem 2 armadilhas — ambas achadas
ao vivo nesta task, ver docs/knowledge/errors-aprendidos.md pro relato completo: (1) todo "o que
o usuário já tinha antes de começar" precisa travar num `useRef` no primeiro render com dado
carregado, nunca recalculado depois (senão o passo 1 muda o cache e o componente passa a achar que
"sempre teve" o que acabou de criar); (2) todo guard de atalho tipo `if (dataJáCompleta) return
<Navigate/>` no topo do componente precisa checar também se o `step` local está em transição
deliberada (`step === null`) — senão o guard dispara no meio de uma transição explícita, porque o
cache já reflete o resultado final antes do próximo passo ter chance de renderizar.
**Quando usar:** Qualquer wizard/onboarding de múltiplos passos onde os passos escrevem no MESMO
cache (React Query) que o componente lê pra decidir navegação — não só quando há mutation entre
passos, mas especificamente quando o mesmo `queryKey` alimenta tanto a decisão de "por onde
entrar" quanto o guard de "já terminei".
**Exemplo:** `OnboardingPage.tsx` — `hadOrganizationOnArrivalRef` (useRef, travado uma vez) decide
se escolher o plano vai pro passo "unidade" ou direto pro app; o guard de topo
`if (step === null && data.organizations.length > 0 && data.activePlanTier)` só atalha em chegada
fria, nunca no meio de uma transição de `step` já em andamento.

## Card de plano compartilhado entre onboarding e configurações (task 040)

`features/subscriptions/PlanCards.tsx` recebe `plans` (catálogo) + `activeTier?` (opcional) +
`onSelect`. Sem `activeTier` (onboarding, `SelectPlanStep`), todo card mostra "Escolher". Com
`activeTier` (tela de configurações), o card cujo `tier` bate ganha badge "Plano atual" e o botão
vira desabilitado — mesmo componente, dois contextos, sem duplicar o grid nem os bullets de
benefício.
**Quando usar:** Qualquer lista de opções (planos, templates, tiers) que aparece tanto num fluxo
de "escolher pela primeira vez" quanto num fluxo de "ver o que já está escolhido e trocar" — evita
o mesmo risco de drift já registrado pro par `StatusBadge`/`FaturaStatusBadge` (sprint-8).

## Chip de ícone nos itens de nav da sidebar (task 040)

`NavItemLink` (`AppLayout.tsx`) envolve o ícone do lucide-react num `<span>` de fundo arredondado
(`bg-surface-sunken` inativo, `bg-on-brand/15` ativo) em vez de deixar o ícone solto ao lado do
texto — dá cara de "módulo" ao item (pedido do usuário), mesma lógica de inversão de cor que já
existia no badge de contagem de Convites (item ativo sólido → conteúdo interno precisa inverter,
senão fica invisível).

## Recurso de negócio (backend) sem tela — audita antes de assumir "pronto" (task 041)

`ProfissionaisController`/`SalasController` (Scheduling) tinham `POST`/`GET` completos e
funcionando desde tasks anteriores, mas nenhuma tela em `frontend/src/features/scheduling/` os
chamava — só `NovoAgendamentoModal` os LIA, nunca escreveu. Um módulo "pronto" no backend não
significa usável: sempre confirmar que toda entidade que o fluxo principal depende (aqui: Agenda
depende de Profissional+Sala existirem) tem caminho de criação na UI, não só de leitura.
**Quando usar:** Ao auditar prontidão de um módulo/produto pra uso real, listar toda entidade que
um fluxo crítico referencia (FK, select, dropdown) e confirmar que cada uma tem tela de CRIAÇÃO,
não só o fluxo que a consome.

## Token opaco de uso único (convite/reset de senha) — mesmo esqueleto, 3 implementações (task 041)

`RefreshToken`/`Invite`/`PasswordResetToken` seguem o MESMO desenho: entidade guarda só o hash
(SHA-256, não Argon2 — não é senha escolhida por humano), gerador dedicado por conceito
(`IRefreshTokenGenerator`/`IInviteTokenGenerator`/`IPasswordResetTokenGenerator` — classes
separadas mesmo com implementação de baixo nível idêntica, é conceito de domínio diferente),
notificação via porta com implementação `Logging*` NO-OP até existir provider de email real, e erro
único genérico na validação (`Invite.NaoEncontrado`/`PasswordReset.TokenInvalido`) que NUNCA
distingue "não existe" de "expirado"/"já usado" — anti-enumeração.
**Quando usar:** Qualquer novo fluxo de "link de uso único enviado por fora" (convite, reset de
senha, confirmação de email, etc.) — copiar este desenho em vez de inventar um novo.

## Limite de plano checado no momento em que o recurso realmente ocupa a vaga (task 041)

Limite de filial é checado em `CreateBranchCommandHandler` (momento em que a Branch é criada).
Limite de usuário é checado em `AcceptInviteCommandHandler` (momento em que a Membership é criada
— NÃO em `CreateInviteCommandHandler`, porque o convite pendente não ocupa vaga nenhuma; checar lá
deixaria passar N convites pendentes que todos vão ocupar vaga quando aceitos depois).
**Quando usar:** Ao adicionar um novo limite de plano, achar o Handler onde a ENTIDADE que conta
pro limite é de fato criada/reativada — não o Handler que só inicia um processo assíncrono
(convite, solicitação, reserva) que pode nunca se concretizar.

## Composição entre módulos mora no controller (Bootstrap), não em Application chamando Application (task 042)

Cadastrar um Profissional (Scheduling) + convidar um Dentista (Identity) na mesma requisição
parecia pedir uma nova porta cross-module (tipo `IInviteIssuer` em `Identity.Contracts`). Em vez
disso, o `ProfissionaisController.Create` (Bootstrap) chama os dois `IRequest`s em sequência —
`CreateProfissionalCommand` primeiro, `CreateInviteCommand` depois se `Email` veio. Mesma lógica no
outro sentido: `InvitesController.Accept` chama `AcceptInviteCommand` e depois
`LinkProfissionalUserCommand`. Nenhum dos dois `Application` layers passa a conhecer o outro.
**Quando usar:** Sempre que uma ação do usuário precisar tocar 2 módulos e a alternativa fosse
criar uma porta cross-module só pra aquele fluxo — se a orquestração é "faça A, depois faça B"
(sem lógica de negócio nova, só sequenciamento), ela pertence ao controller. Reservar porta
cross-module (`*.Contracts`) pra quando um módulo precisa CONSULTAR ou DECIDIR com base em dado de
outro dentro do próprio Handler (ex.: limite de plano, validação de Branch) — isso sim é
Application chamando Contracts, não controller orquestrando.

## Recurso "convidável" — a entidade sempre existe, o login é opcional por cima (task 042)

`Profissional` nunca depende de ter usuário — sempre foi assim (recurso puro da Agenda). O convite
(quando o email é informado) é estritamente ADITIVO: só cria a possibilidade de login, nunca é
pré-requisito. `VincularUsuario` só roda em best-effort, depois do fato consumado (convite
aceito) — nunca bloqueia nem o cadastro do recurso, nem o aceite do convite.
**Quando usar:** Qualquer entidade de "pessoa que trabalha na clínica mas pode ou não ter login"
(recepcionista sem sistema próprio, técnico, etc.) — mesmo desenho: recurso primeiro, acesso
depois, opcional, nunca no caminho crítico um do outro.
