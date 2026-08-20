# Regras de Negócio

> Mantido pelo Writer. Substitui o antigo `business-analyst.md` por agente — agora é o
> Product Owner quem decide a regra, e o Writer quem persiste e indexa no graphify:
> `/graphify docs/knowledge --update`.

## Regras explícitas

<!-- Formato: - [YYYY-MM-DD] [regra] — fonte: /law ou task docs/tasks/{id}.md -->

- [2026-08-17] Sem registro público de tenant/usuário. Um seed inicial cria o primeiro `Admin`
  junto com um tenant base ("Clínica Demo"). Todo usuário adicional (`Dentista`, `Recepcao`) só
  é criado por um `Admin` já autenticado, via `POST /api/users` (policy `RequireRole(Admin)`),
  sempre dentro do próprio tenant do admin que cria — fonte: task docs/tasks/002-identity-access-login.md
  (decisão do PO: controle centralizado de provisionamento, produto não é SaaS self-serve).
- [2026-08-17] RBAC com 3 papéis mínimos: `Admin`, `Dentista`, `Recepcao`. Papel vai como claim
  no JWT — fonte: task docs/tasks/002-identity-access-login.md.
- [2026-08-17] Login emite JWT de acesso com 15 minutos de duração + refresh token com 7 dias,
  rotativo (cada uso do refresh token gera um novo par access+refresh e invalida o token
  anterior) — fonte: task docs/tasks/002-identity-access-login.md.
- [2026-08-17] Login recusa autenticação se o tenant do usuário estiver inativo
  (`tenant.Ativo == false`), mesmo com credenciais corretas — fonte: sugestão do QA aplicada na
  task docs/tasks/002-identity-access-login.md, erro de domínio `DomainErrors.Tenant.Inativo`.
- [2026-08-17] Email de usuário é único globalmente na plataforma, não por tenant — um mesmo
  email não pode existir em dois tenants diferentes. Login identifica o tenant do usuário pelo
  próprio email, sem precisar de header/subdomínio na requisição — fonte: task
  docs/tasks/002-identity-access-login.md (decisão técnica do Dev Backend, ver também
  docs/decisions.md).

- [2026-08-17] CPF do paciente é único por `(TenantId, Cpf)`, não globalmente — o mesmo CPF pode
  existir em dois tenants (clínicas) diferentes, porque são operações independentes. Diferente
  do email do Identity, que é único globalmente na plataforma — fonte: task
  docs/tasks/003-pacientes-crm.md (decisão do Dev Backend).
- [2026-08-17] CPF é validado por Value Object com dígito verificador mod-11 real e rejeita
  sequência repetida (ex: "11111111111"), não só formato — fonte: task
  docs/tasks/003-pacientes-crm.md.
- [2026-08-17] Cadastro de paciente exige `ConsentimentoLgpd = true` na criação, validado em
  dupla camada: FluentValidation (input da API) e domínio (`Patient.Create` recusa criar sem
  consentimento, independente de quem chame) — fonte: task docs/tasks/003-pacientes-crm.md.
- [2026-08-17] CPF do paciente é imutável após a criação — `AtualizarDadosCadastrais` não aceita
  alterar CPF. QA adicionou teste de salvaguarda pra impedir que `TenantId` ou consentimento
  LGPD sejam adicionados acidentalmente como parâmetro mutável no futuro — fonte: task
  docs/tasks/003-pacientes-crm.md.
- [2026-08-17] Exclusão de paciente é soft delete real: `Desativar()` seta `Ativo=false`, nunca
  remove a linha física — fonte: task docs/tasks/003-pacientes-crm.md.
- [2026-08-17] RBAC de Pacientes: `Admin` e `Recepcao` administram (criam/editam/desativam)
  cadastro de paciente; `Dentista` só lê — fonte: task docs/tasks/003-pacientes-crm.md.
- [2026-08-17] Agendamento segue máquina de estados: `Agendado` → `Confirmado` → `Concluido`, ou
  `Agendado`/`Confirmado` → `Cancelado`. Transições fora desse grafo são rejeitadas pelo próprio
  agregado — fonte: task docs/tasks/004-agenda-vertical.md.
- [2026-08-17] RBAC de Agenda: `Dentista` PODE gerenciar agenda (diferente de Pacientes), mas só
  a PRÓPRIA — checado via `Profissional.UserId` vinculado ao usuário logado
  (`AgendaOwnershipGuard`). `Admin` e `Recepcao` gerenciam qualquer agenda do tenant, sem
  restrição de ownership. Ownership é aplicado em Confirmar/Cancelar/Concluir — NÃO em Create
  (pendência de produto: "dentista só cria na própria agenda" ainda não foi pedido pelo PO) —
  fonte: task docs/tasks/004-agenda-vertical.md, fix pós-QA.
- [2026-08-17] `ConsultaConcluidaEvent` é o contrato estável entre Scheduling e o futuro módulo
  Financeiro/Billing (task 006). É gravado via Outbox na mesma transação que marca o
  agendamento como `Concluido` (atomicidade real), mas o worker de publicação (MassTransit/
  RabbitMQ) ainda não existe — dívida técnica conhecida, não bug, documentada em
  docs/sprints/sprint-1.md — fonte: task docs/tasks/004-agenda-vertical.md.

- [2026-08-17] Prontuário eletrônico é 1:1 por paciente (um prontuário por `(TenantId,
  PacienteId)`, índice único). Acesso restrito a `Admin` e `Dentista` — `Recepcao` NÃO tem
  acesso a dado clínico (diferente de Pacientes, onde Recepcao administra o cadastro) — fonte:
  task docs/tasks/005-prontuario-eletronico.md.
- [2026-08-17] Toda LEITURA de prontuário é registrada na trilha de auditoria — não é opcional,
  não existe endpoint de leitura que não audite. Escrita (criação, evolução clínica, upload de
  anexo) também audita. Trilha é append-only: não existe endpoint nem método de código pra
  editar/apagar uma entrada já gravada — fonte: task docs/tasks/005-prontuario-eletronico.md.
- [2026-08-17] Descrição clínica de evolução (`EvolucaoClinica.DescricaoClinica`) é cifrada em
  repouso (AES-256-GCM) — dado clínico sensível sob a LGPD. Odontograma (mapa dente→status) NÃO
  é cifrado (dado estrutural, não texto livre) — fonte: task docs/tasks/005-prontuario-eletronico.md.
- [2026-08-17] Histórico clínico (evolução) e anexo são imutáveis após criados — nunca editados
  ou removidos, só acumulados. Correção de erro se resolve com nova entrada/novo upload, nunca
  edição do existente — fonte: task docs/tasks/005-prontuario-eletronico.md.
- [2026-08-17] Visualização da trilha de auditoria do prontuário é Admin-only (view de
  compliance) — fonte: task docs/tasks/005-prontuario-eletronico.md.

- [2026-08-17] Fatura particular parcela de 1 a 12 vezes; fatura de convênio é sempre 1 "parcela"
  interna (parcelamento real, se houver, é regra do convênio externo). Status da fatura é sempre
  DERIVADO do status das parcelas (Pendente/ParcialmentePaga/Paga/Vencida), nunca setado
  manualmente — fonte: task docs/tasks/006-financeiro-particular.md.
- [2026-08-17] Geração de fatura a partir de consulta concluída é idempotente: no máximo uma
  fatura por `AgendamentoId` (índice único parcial), reentrega do mesmo evento nunca duplica —
  fonte: task docs/tasks/006-financeiro-particular.md.
- [2026-08-17] RBAC de Financeiro: `Admin` e `Recepcao` administram fatura; `Dentista` não tem
  acesso (mesmo raciocínio de Pacientes — operação administrativa, não clínica). Cadastro de
  convênio é Admin-only — fonte: task docs/tasks/006-financeiro-particular.md.
- [2026-08-17] Comissão de dentista (`ComissaoDentistaPercentual`) é campo opcional em QUALQUER
  fatura (particular ou convênio), não exclusivo de um tipo — fonte: task
  docs/tasks/007-financeiro-convenios.md.
- [2026-08-17] Falha no envio de fatura ao convênio externo NÃO desfaz a fatura já persistida —
  ela fica registrada sem `ProtocoloConvenio`, disponível pra reenvio manual — fonte: task
  docs/tasks/007-financeiro-convenios.md.

- [2026-08-17] Hierarquia `Tenant (rede) → Unidade (clínica) → Recurso` (Fase 5) é ADITIVA:
  `UnidadeId` é opcional em `Profissional`, `Sala` (Scheduling), `User` (Identity) e
  `ItemEstoque`. Null = sem restrição de unidade (Admin de rede, ou recurso "solto" no tenant) —
  fonte: task docs/tasks/009-rede-multi-unidade.md.
- [2026-08-17] Recepcao com `UnidadeId` setado só enxerga agendamentos de profissionais lotados
  na própria unidade (`ListAgendamentosQuery`) — o filtro é forçado pelo controller a partir da
  claim JWT, nunca aceito como parâmetro de query string (não dá pra um Recepcao "escolher" ver
  outra unidade). Admin e Dentista não são restritos por unidade — fonte: task
  docs/tasks/009-rede-multi-unidade.md.
- [2026-08-17] Estoque (`ItemEstoque`) nunca fica com quantidade negativa — `RegistrarSaida`
  maior que o disponível é rejeitado (`Result.Failure`), nunca truncado em zero. `UnidadeId`
  opcional: null = estoque compartilhado do tenant inteiro — fonte: task
  docs/tasks/009-rede-multi-unidade.md.
- [2026-08-17] Cadastro de convênio, unidade e criação de Profissional/Sala/usuário são
  operações Admin-only (configuração organizacional) — fonte: tasks docs/tasks/007, 009.

- [2026-08-18] Comprovante de pagamento = comissão do profissional (`Fatura.ComissaoDentistaPercentual`)
  sobre faturas com PARCELA PAGA no período filtrado (`Parcela.DataPagamento`), regime de caixa —
  não é folha de salário fixo (conceito inexistente no domínio hoje). Visão **colaborador**: só a
  própria comissão (RBAC de ownership). Visão **empresa** (`Admin`/`Owner`): agregado por filial
  (`BranchId`, derivado de `Profissional.BranchId`) e por classe (`Role` da membership). Sistema
  mostra total do período E **média diária** (total ÷ dias do intervalo, inclusivo nas duas
  pontas, mínimo 1 dia), pras duas visões. Sem PDF gerado no backend nesta rodada — comprovante é
  view estruturada, exportável via `window.print()` do browser; PDF real é dívida nomeada — fonte:
  decisão do PO em docs/sprints/sprint-7.md, implementada nas tasks
  docs/tasks/022-contratos-leitura-comissao.md e docs/tasks/023-query-comissoes-endpoint-rbac.md.

- [2026-08-19] Hierarquia de tenancy pra onboarding é a que já existe — Organization = empresa (1
  por conta, dados legais/nome), Branch = cada unidade/filial dela (nome, endereço) — **sem**
  camada nova ("franquia"/"filiado" são só como o usuário se refere a Branch em linguagem
  informal, não conceitos de domínio novos). Organization pode ter N branches, todas acessíveis a
  partir da mesma conta — fonte: grill-me do usuário, task docs/tasks/037-onboarding-guiado-org-filial.md.
- [2026-08-19] Onboarding pós-login é guiado em 2 passos obrigatórios pra quem escolhe criar:
  Organization (empresa) → primeira Branch (unidade) → só depois cai na tela de gestão
  (Agenda/Pacientes/etc). Criar mais Branches depois fica fora do onboarding (fluxo normal do
  app) — fonte: task docs/tasks/037-onboarding-guiado-org-filial.md.
- [2026-08-19] Usuário pode pular a criação de empresa no onboarding ("por enquanto não") — escolha
  persistida no backend (`User.OnboardingSkipped`), não expira, não é por sessão/device. Sem
  organization + já pulou = app abre normalmente (nav/tema/logout funcionam), mas a área de
  conteúdo mostra só um convite pra criar a empresa — nenhuma página de gestão real renderiza
  (todas dependem de `organizationId` do JWT, inexistente nesse estado) — fonte: task
  docs/tasks/036-backend-skip-onboarding.md.
- [2026-08-19] Landing page pública (rota "/") existe pra converter visitante em conta
  (trial/paga) — nav fixa (logo + Entrar/Criar conta), hero vendendo o produto, seção de planos.
  Pricing nessa rodada é **vitrine estática** (3 planos: Starter/Profissional/Rede, conteúdo
  definido pelo time, sem gateway de pagamento/checkout real) — todo CTA de plano leva pro
  `/signup`, cobrança é dívida técnica registrada, não implementada — fonte: grill-me do usuário,
  task docs/tasks/038-landing-page-publica.md.

- [2026-08-19] Organização sem plano ativo (`Subscriptions.Subscription`) é bloqueada de acessar o
  app — mesmo tratamento de organização inexistente (`RequireOrganization`/`/onboarding`).
  Escolher plano nesta rodada NÃO cobra (upsert direto, `Status=Ativa`) — checkout real (Stripe)
  é dívida técnica nomeada, endpoint/porta existem mas não chamam API externa nenhuma — fonte:
  grill-me do usuário, task docs/tasks/039-sistema-planos-onboarding-completo.md.
- [2026-08-19] 3 planos fixos (não configuráveis por Admin/seed): Starter (1 filial, R$129/mês),
  Profissional (3 filiais, R$349/mês), Rede (filiais ilimitadas, preço sob consulta) — fonte única
  `Subscriptions.Domain.PlanCatalog`, espelhada na landing e no onboarding. Criar filial além do
  limite do plano ativo é bloqueado (`Branch.LimiteDoPlanoAtingido`) — fonte: task
  docs/tasks/039-sistema-planos-onboarding-completo.md.
- [2026-08-19] Organization (Cnpj/Telefone/Endereco) e Branch (Telefone) ganham campos opcionais
  na criação — só o Nome continua obrigatório, resto é "configurar depois" (sem tela de edição
  dedicada ainda, dívida técnica nomeada) — fonte: pedido do usuário, task
  docs/tasks/039-sistema-planos-onboarding-completo.md.

- [2026-08-19] Limite de USUÁRIO por plano (não só filial) — Starter até 3, Profissional/Rede
  ilimitado, mesma fonte `PlanCatalog.LimiteUsuarios`. Checado no ACCEPT do convite (não na
  criação), bloqueado com `Membership.LimiteDoPlanoAtingido` — fonte: auditoria pré-venda, task
  docs/tasks/041-remediacao-auditoria-pre-venda.md.
- [2026-08-19] Downgrade de plano bloqueia se a organização tem mais filiais ATIVAS do que o
  plano novo permite (`Subscription.DowngradeExcedeFiliaisAtivas`) — fecha dívida nomeada desde a
  039 — fonte: task docs/tasks/041-remediacao-auditoria-pre-venda.md.
- [2026-08-19] Cobrança da plataforma é MANUAL por fora nesta fase (decisão do usuário, sem chave
  Stripe disponível) — escolher um plano libera acesso na hora sem cobrar, aviso explícito na UI
  (`PlanCards`). Stripe real fica pra quando a integração acontecer de verdade — fonte: task
  docs/tasks/041-remediacao-auditoria-pre-venda.md.

- [2026-08-20] Cadastro de Profissional aceita Tipo de Contrato (CLT/PJ/Autônomo, obrigatório) e Comissão % padrão (opcional, só informativo — Billing não lê automaticamente ainda). Email opcional dispara convite Role.Dentista; sem email, o profissional é só recurso da Agenda, sem login — fonte: task docs/tasks/042-cadastro-dentista-com-convite.md.
- [2026-08-20] Aceitar um convite vincula automaticamente o usuário a qualquer Profissional pendente (sem UserId) com o mesmo email na organização — sem passo manual. Vínculo é definitivo pro primeiro que aceitar (não sobrescreve) — fonte: task docs/tasks/042-cadastro-dentista-com-convite.md.

Registrar novas com `/law [regra]` ou via primeira pergunta do `/bigtask`.

## Regras inferidas

<!-- Formato: - [YYYY-MM-DD] [regra] [inferred] — origem: task docs/tasks/{id}.md, aguardando confirmação do PO -->

Nenhuma ainda.
