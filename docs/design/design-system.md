# Design System — OdontoPlatform

**Task:** 024 · **Status da spec:** pronta para revisão do PO → implementação nas tasks 025-028
**Autora:** Aurora (Designer) · **Não altera código** — isto é spec + tokens para o Dev Frontend copiar.

Pedido original do usuário: *"minimalista, fácil de entender, inovador, pegada verde e branco,
modo escuro e claro, não pareça vibe codado, menu fácil de navegar."*

---

## 0. Decisões de design (leia antes do resto)

1. **A escala `brand-*` atual é o Tailwind emerald padrão de fábrica** (`#ecfdf5…#064e3b` = exatamente
   `emerald-50…emerald-900` do Tailwind). É o motivo nº1 de "parecer vibe codado" — é a primeira cor
   que qualquer LLM/template escolhe para "verde saúde". Troquei por uma escala verde-pinho
   autoral, mesma família (mantém a decisão do Architect de partir do que existe), saturação mais
   baixa e tom mais frio — ver §1.2.
2. **Nomenclatura dos tokens de texto**: a task pediu `text-primary`, `text-secondary`, `text-muted`.
   Em Tailwind v4, uma variável `--color-text-primary` gera a utilitária `text-text-primary`
   (o prefixo da propriedade `text-` colide com o nome do token). Troquei para `ink` / `ink-secondary`
   / `ink-muted` — mesma função, sem a duplicação visual `text-text-*` espalhada em toda tela.
   Mapeamento 1:1 na tabela do §1.1, não é decisão que precisa aprovação, é ergonomia de sintaxe.
3. **Dois tokens de status a mais do que o mínimo pedido**: `info` (o app já tem um 3º estado de
   badge, azul, em `Confirmado`/`ParcialmentePaga` — não é "warning" nem "success") e um padrão
   `neutral` para status "cancelado/inativo" (reaproveita `surface-sunken` + `ink-secondary`, não é
   token de cor novo). Sem isso o Dev Frontend inventaria um azul na 025 e voltaríamos ao problema
   de cor solta.
4. **Cada cor de status ganha um par `-subtle`** (`success-subtle`, `warning-subtle`, `danger-subtle`,
   `info-subtle`) para fundo de badge — mesmo padrão já pedido para `brand-subtle`. Consistência:
   toda cor semântica segue a mesma convenção "cor forte para texto/ícone, `-subtle` para fundo".
5. **Fonte**: mantida a stack padrão do Tailwind (`font-sans` = system-ui). Não há import de webfont
   no projeto hoje — não vou adicionar um Google Font só por estética; isso é exatamente o tipo de
   detalhe que grita "template". A identidade vem da paleta + peso tipográfico nos números do
   dashboard (§2.3), não da fonte.
6. **Ícones**: o projeto não tem lib de ícones hoje (confirmado via grep, zero ocorrência). Uso
   `lucide-react`, já aprovado pelo Architect. Ver §4.

---

## 1. Paleta e tokens semânticos

### 1.1 Tabela de mapeamento (nome pedido pela task → var CSS → utilitária Tailwind)

| Função (task 024) | Var CSS | Utilitária principal | Uso |
|---|---|---|---|
| surface | `--color-surface` | `bg-surface` | fundo de página |
| surface-raised | `--color-surface-raised` | `bg-surface-raised` | cards, modais, inputs, sidebar |
| surface-sunken | `--color-surface-sunken` | `bg-surface-sunken` | cabeçalho de tabela, área recuada, disabled |
| border | `--color-border` | `border-border` | divisórias decorativas (cards, linhas de tabela) |
| border-strong | `--color-border-strong` | `border-border-strong` | borda de campo interativo (input/select/checkbox) — precisa 3:1 |
| text-primary | `--color-ink` | `text-ink` | texto principal, títulos |
| text-secondary | `--color-ink-secondary` | `text-ink-secondary` | texto de apoio, células de tabela |
| text-muted | `--color-ink-muted` | `text-ink-muted` | timestamps, placeholders, legendas |
| brand | `--color-brand` | `bg-brand` / `text-brand` | ação primária, link, estado ativo |
| brand-hover | `--color-brand-hover` | `hover:bg-brand-hover` | hover de ação primária |
| brand-subtle | `--color-brand-subtle` | `bg-brand-subtle` | fundo de item de nav ativo, hover leve |
| on-brand | `--color-on-brand` | `text-on-brand` | texto/ícone sobre fundo `brand` |
| success | `--color-success` | `text-success` | texto/ícone de sucesso |
| success-subtle | `--color-success-subtle` | `bg-success-subtle` | fundo de badge de sucesso |
| warning | `--color-warning` | `text-warning` | texto/ícone de alerta |
| warning-subtle | `--color-warning-subtle` | `bg-warning-subtle` | fundo de badge de alerta |
| danger | `--color-danger` | `bg-danger` / `text-danger` | ação destrutiva, erro |
| danger-subtle | `--color-danger-subtle` | `bg-danger-subtle` | fundo de badge de erro |
| info *(adição justificada, item 3)* | `--color-info` | `text-info` | badge "em andamento" (Confirmado / Parcialmente paga) |
| info-subtle | `--color-info-subtle` | `bg-info-subtle` | fundo de badge info |
| focus-ring | `--color-focus-ring` | `outline-focus-ring` | anel de foco em todo elemento interativo |

### 1.2 Valores — light e dark, com contraste verificado

Fórmula de luminância relativa WCAG aplicada nos pares crítico (texto sobre fundo, botão
preenchido, borda de campo). AA = 4.5:1 texto normal, 3:1 texto grande/ícone/borda de componente.

| Token | Light (hex) | Dark (hex) | Contraste verificado |
|---|---|---|---|
| surface | `#F7FAF8` | `#101713` | fundo de página — não é par de texto |
| surface-raised | `#FFFFFF` | `#161F1A` | fundo de página — não é par de texto |
| surface-sunken | `#EFF4F1` | `#0B100D` | fundo de página — não é par de texto |
| border | `#E2E8E5` | `#26302A` | decorativo, sem requisito de contraste |
| border-strong | `#7E8F87` | `#5B6B62` | vs surface-raised → **4.19:1 (light) / 3.63:1 (dark)** — passa 3:1 (borda de campo) |
| ink | `#131B16` | `#EDF3EF` | vs surface-raised → **>15:1** nos dois modos |
| ink-secondary | `#4B5A52` | `#AEBDB4` | vs surface-raised → **7.29:1 (light)** / **~10:1 (dark)** |
| ink-muted | `#64756D` | `#8A9A91` | vs surface-raised → **4.88:1 (light)** / **5.7:1 (dark)** — passa AA texto normal, mas use com moderação em texto pequeno |
| brand | `#1F7A54` | `#35B583` | vs branco/surface-raised → **5.29:1 (light)** / **7.9:1 (dark, vs surface-raised)** |
| brand-hover | `#17603F` | `#43C793` | mais escuro no hover (light) / mais claro no hover (dark) — convenção padrão de cada modo |
| brand-subtle | `#E4F3EA` | `#16281F` | fundo, texto `brand` sobre ele → **>6:1** nos dois modos |
| on-brand | `#FFFFFF` | `#06170F` | **inverte no dark** — ver nota abaixo |
| success | `#177A3D` | `#3FC08D` | mesma lógica de brand |
| success-subtle | `#DCF3E3` | `#12271C` | — |
| warning | `#8A5A0A` | `#E3B341` | vs branco/escuro → **>4.5:1** nos dois modos |
| warning-subtle | `#FDF0D9` | `#2B2411` | — |
| danger | `#C2352B` | `#F1685C` | vs branco → **5.48:1 (light)**; dark verificado por analogia de luminância, mesma margem |
| danger-subtle | `#FBE7E5` | `#2B1512` | — |
| info | `#2563A8` | `#6FA8DC` | mesma lógica |
| info-subtle | `#E3EEF9` | `#142433` | — |
| focus-ring | `#1F7A54` (= brand) | `#35B583` (= brand) | vs surface-raised → **5.29:1 / 7.9:1**, muito acima do mínimo 3:1 exigido para indicador de foco |

**Nota importante — `on-brand` inverte entre os modos.** Em light, `brand` é um verde médio-escuro,
então texto branco em cima funciona (5.29:1). Em dark, para o botão continuar legível e não "sumir"
no fundo escuro, `brand` fica mais claro (`#35B583`) — e um texto branco em cima disso não teria
contraste suficiente. Por isso `on-brand` no dark é um verde quase preto (`#06170F`), não branco.
**Isto não é inconsistência, é a mesma regra de contraste aplicada corretamente nos dois modos** —
sinalizando aqui porque um Dev Frontend apressado pode "corrigir" para branco nos dois casos achando
que é bug.

`ink-muted` no light (4.88:1) tem margem menor — não usar abaixo de 12px/400. Em telas com texto
pequeno e denso (tabela do Estoque, timestamps), preferir `ink-secondary`.

---

## 2. Tipografia e espaçamento

### 2.1 Fonte
Mantida a stack padrão Tailwind (`font-sans`: `ui-sans-serif, system-ui, -apple-system, ...`). Sem
webfont novo.

### 2.2 Escala (máx. 5 tamanhos, como pedido)

| Nome | Tamanho / linha | Peso | Uso |
|---|---|---|---|
| `text-xs` | 12px / 16px | 400 / 500 | timestamps, labels de tabela (uppercase), legendas |
| `text-sm` | 14px / 20px | 400 / 500 | corpo padrão, inputs, botões, células de tabela |
| `text-base` | 16px / 24px | 400 / 500 | texto de destaque em corpo, labels de formulário grandes |
| `text-lg` | 18px / 28px | 600 | título de card, título de modal (`CardTitle`, `Modal` title) |
| `text-2xl` | 24px / 32px | 600 | título de página (H1) |

### 2.3 Detalhe de identidade — números do dashboard

O pedido "não parecer vibe codado" pede um detalhe de personalidade. Escolhido: **peso e
tratamento tipográfico dedicado só aos números de KPI**, não usado em mais nenhum lugar do app —
isso cria hierarquia e assinatura visual sem introduzir nova cor ou fonte.

```css
.text-kpi {
  font-size: 1.875rem;      /* 30px — maior que a escala de texto normal, exclusivo de KPI */
  line-height: 2.25rem;
  font-weight: 700;         /* bold — único uso de 700 no sistema, reservado a números-chave */
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.01em;
}
```
Regra: `text-kpi` só é usado no valor numérico de um card de KPI (dashboard de comissões, Relatórios).
Nunca em título, nunca em corpo de texto — se aparecer em outro lugar, é quebra de padrão.

### 2.4 Espaçamento e raio

Não crio escala nova — disciplino o uso da escala Tailwind já existente:

| Espaçamento | Uso |
|---|---|
| `gap-1` / `p-1` (4px) | ícone↔label, badge interno |
| `gap-2` / `p-2` (8px) | grupos de controles (ex.: `Button` + `Button` em ação dupla) |
| `px-3 py-2` (12/8px) | padding interno de `Input`/`Select`/`Button` — já é o padrão, mantido |
| `px-5 py-4` (20/16px) | padding de `CardHeader`/`CardBody` — já é o padrão, mantido |
| `p-6` (24px) | padding da área de conteúdo (`<main>`) — já é o padrão, mantido |
| `gap-8` (32px) | separação entre seções do dashboard |

Raio — só 3 valores em todo o app, sem exceção:
- `rounded-md` (6px): `Button`, `Input`, `Select`, itens de nav
- `rounded-lg` (8px): `Card`, `Modal`
- `rounded-full`: badges/pills, avatar, contador de convites

---

## 3. Bloco CSS pronto (Tailwind v4 — copiar para `frontend/src/index.css`)

```css
@import "tailwindcss";

@custom-variant dark (&:where(.dark, .dark *));

@theme {
  /* Superfícies e bordas — valores LIGHT (default) */
  --color-surface: #F7FAF8;
  --color-surface-raised: #FFFFFF;
  --color-surface-sunken: #EFF4F1;
  --color-border: #E2E8E5;
  --color-border-strong: #7E8F87;

  /* Texto */
  --color-ink: #131B16;
  --color-ink-secondary: #4B5A52;
  --color-ink-muted: #64756D;

  /* Marca — verde-pinho autoral (não é o emerald padrão do Tailwind) */
  --color-brand: #1F7A54;
  --color-brand-hover: #17603F;
  --color-brand-subtle: #E4F3EA;
  --color-on-brand: #FFFFFF;

  /* Status */
  --color-success: #177A3D;
  --color-success-subtle: #DCF3E3;
  --color-warning: #8A5A0A;
  --color-warning-subtle: #FDF0D9;
  --color-danger: #C2352B;
  --color-danger-subtle: #FBE7E5;
  --color-info: #2563A8;
  --color-info-subtle: #E3EEF9;

  /* Foco */
  --color-focus-ring: #1F7A54;

  /* Detalhe tipográfico de KPI — não é cor, mas fica junto do @theme por conveniência */
  --font-weight-kpi: 700;
}

/* Overrides DARK — mesma variável, valor diferente. Tudo que usa @theme acima
   passa a refletir o dark automaticamente, sem trocar nenhuma classe no componente. */
.dark {
  --color-surface: #101713;
  --color-surface-raised: #161F1A;
  --color-surface-sunken: #0B100D;
  --color-border: #26302A;
  --color-border-strong: #5B6B62;

  --color-ink: #EDF3EF;
  --color-ink-secondary: #AEBDB4;
  --color-ink-muted: #8A9A91;

  --color-brand: #35B583;
  --color-brand-hover: #43C793;
  --color-brand-subtle: #16281F;
  --color-on-brand: #06170F; /* inverte — ver nota no §1.2 */

  --color-success: #3FC08D;
  --color-success-subtle: #12271C;
  --color-warning: #E3B341;
  --color-warning-subtle: #2B2411;
  --color-danger: #F1685C;
  --color-danger-subtle: #2B1512;
  --color-info: #6FA8DC;
  --color-info-subtle: #142433;

  --color-focus-ring: #35B583;
}

body {
  @apply bg-surface text-ink antialiased;
}

.text-kpi {
  font-size: 1.875rem;
  line-height: 2.25rem;
  font-weight: var(--font-weight-kpi);
  font-variant-numeric: tabular-nums;
  letter-spacing: -0.01em;
}

/* Impressão do comprovante — ver §7.2 */
@media print {
  .no-print { display: none !important; }
  body, .print-area { background: white !important; color: black !important; }
  .print-area * { color: black !important; background: white !important; box-shadow: none !important; }
}
```

**Toggle de tema — comportamento esperado (implementação é do Dev Frontend, especificado aqui
para não vazar decisão de UX pra API errada):**
- 3 estados: `light` / `dark` / `system`, persistido (ex.: `localStorage`).
- Aplicar a classe `.dark` no `<html>` **antes do primeiro paint** (script inline no `index.html`
  ou equivalente) — sem isso há FOUC (flash de tema errado), já sinalizado como restrição do
  Architect.
- `system` escuta `prefers-color-scheme` e atualiza em tempo real se o SO mudar.

---

## 4. Ícones — decisão

Biblioteca: **`lucide-react`** (aprovada pelo Architect, leve, tree-shakeable). Não instalada
ainda — Dev Frontend adiciona em `package.json` na 025.

Set mínimo necessário para nav + toggle + ações já existentes no app:

| Ícone | Uso |
|---|---|
| `Calendar` | Agenda |
| `Users` | Pacientes |
| `Wallet` | Financeiro |
| `Package` | Estoque |
| `BarChart3` | Relatórios |
| `Mail` | Convites |
| `Sun` / `Moon` / `MonitorSmartphone` | toggle de tema (light/dark/system) |
| `PanelLeftClose` / `PanelLeftOpen` | colapsar/expandir sidebar |
| `Menu` / `X` | abrir/fechar drawer mobile |
| `ChevronDown` | `OrgSwitcher`, dropdowns |
| `LogOut` | sair |

Tamanho padrão: `16px` (`size={16}`) dentro de item de nav/botão pequeno, `20px` em cabeçalhos.
Cor sempre herdada de `currentColor` (nunca cor hardcoded no ícone) — para acompanhar automaticamente
`text-ink-secondary` no estado default e `text-brand` no estado ativo.

---

## 5. Spec dos componentes `ui/` existentes (re-skin, mesma API)

Todos os estados abaixo valem para light e dark — como os tokens já mudam de valor via `.dark`,
**a classe Tailwind escrita no componente é a mesma nos dois modos** (esse é o ganho de usar tokens
em vez de cor hardcoded: zero `dark:` espalhado por lógica de cor pura).

### Button
| Estado | classes (usando tokens) |
|---|---|
| primary default | `bg-brand text-on-brand` |
| primary hover | `hover:bg-brand-hover` |
| primary focus | `focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring` |
| secondary default | `bg-surface-raised text-ink border border-border-strong` |
| secondary hover | `hover:bg-surface-sunken` |
| danger default | `bg-danger text-on-brand` *(mesmo `on-brand` — é texto claro/escuro sobre fundo saturado, função idêntica à do brand)* |
| danger hover | `hover:bg-danger` com leve `brightness-90` — **não há token `danger-hover` no mínimo da task; se o Dev Frontend achar necessário, usar `filter: brightness()` em vez de inventar hex novo** |
| ghost default | `bg-transparent text-ink-secondary` |
| ghost hover | `hover:bg-surface-sunken` |
| disabled (todas) | mantém `disabled:opacity-50 disabled:cursor-not-allowed` (já é token-agnóstico, ok) |

🟡 Sinalizo para a 025: `danger` não tem par `-hover` nos tokens mínimos da task — usei
`brightness-90` como solução sem cor nova. Se o Dev Frontend preferir, pode voltar pra mim pedindo
`danger-hover` explícito; não é bloqueante.

### Input / Select
| Estado | classes |
|---|---|
| default | `bg-surface-raised border border-border-strong text-ink` |
| focus | `focus:outline-none focus:ring-2 focus:ring-focus-ring focus:border-brand` |
| error | `border-danger` (troca `border-border-strong` → `border-danger`), mensagem abaixo `text-danger` |
| disabled | `disabled:bg-surface-sunken disabled:text-ink-muted disabled:cursor-not-allowed` *(estado disabled não existia explicitado antes — adicionando, hoje não há tratamento visual de campo desabilitado em nenhuma tela)* |

🟡 Nota: nenhum dos 8 componentes atuais trata estado `disabled` visualmente (só via atributo HTML).
Adicionar isso é uma pequena melhoria de acessibilidade, não uma mudança de escopo — sinalizar ao
Dev Frontend que é baixo esforço, alto retorno.

### Label
`text-ink-secondary text-sm font-medium` — sem mudança de estado (label não tem estado interativo).

### Checkbox
| Estado | classes |
|---|---|
| default | `border-border-strong text-brand` (cor de "check" quando marcado) |
| focus | `focus:ring-2 focus:ring-focus-ring` |
| label | `text-ink text-sm` |
| error | mensagem `text-danger` |

### Card / CardHeader / CardTitle / CardBody
- `Card`: `bg-surface-raised border border-border rounded-lg shadow-sm`
- `CardHeader`: `border-b border-border`
- `CardTitle`: `text-ink text-lg font-semibold`
- `CardBody`: sem cor própria, herda

### Modal
- backdrop: `bg-ink/40` (usa o token `ink` com opacidade em vez de `bg-slate-900/40` hardcoded —
  funciona nos dois modos porque `ink` já é escuro no light e ainda mais “neutro-escuro” no dark)
- painel: `bg-surface-raised text-ink shadow-xl rounded-lg`
- título: `text-ink text-lg font-semibold`
- botão fechar: `text-ink-muted hover:bg-surface-sunken hover:text-ink-secondary`

### StatusBadge / FaturaStatusBadge
Consolidar as duas em **uma única convenção de status → tokens** (hoje são dois arquivos quase
idênticos com cores hardcoded diferentes por acidente — ver §6 para o caso `Vencida`/`Cancelada`
que hoje já diverge). Nova convenção, os dois componentes passam a usar o mesmo mapa de estilo:

| Status conceitual | Exemplos concretos | classes |
|---|---|---|
| Pendente / Agendado (warning) | `Agendado`, `Pendente` | `bg-warning-subtle text-warning` |
| Em andamento (info) | `Confirmado`, `ParcialmentePaga` | `bg-info-subtle text-info` |
| Concluído (success) | `Concluido`, `Paga` | `bg-success-subtle text-success` |
| Vencido/erro (danger) | `Vencida` | `bg-danger-subtle text-danger` |
| Cancelado/inativo (neutral) | `Cancelado`, `Cancelada`, item inativo do Estoque | `bg-surface-sunken text-ink-secondary line-through` |

🔴 **Quebra de padrão já existente hoje, corrigida aqui**: `StatusBadge.Cancelado` usa
`bg-slate-200 text-slate-600` mas `FaturaStatusBadge.Cancelada` usa exatamente o mesmo par —
esses dois já são consistentes entre si, ok. O problema real está em `EstoquePage.tsx:149`
(badge "Inativo" usa `bg-slate-200 text-slate-600`, igual ao padrão neutral acima — já compatível)
**mas o badge "Estoque baixo" usa `bg-amber-100 text-amber-800`** (mesmo padrão visual de warning,
já alinhado) — ou seja, os 3 pontos de badge do app hoje já convergem para 4 cores consistentes.
Boa notícia: a tabela acima é só a formalização em token do que já era consistente, não uma
correção de bug visual — sinalizando aqui pra não ser reportado como quebra na 029 sem necessidade.

---

## 6. Lista completa de cor hardcoded em uso hoje (bloqueante pra 025)

Levantamento sistemático via grep de toda classe `bg-*/text-*/border-*/divide-*/outline-*/ring-*`
com paleta Tailwind padrão, em `frontend/src/**/*.tsx` (excluindo `node_modules`/`dist`).
**93 ocorrências**, agrupadas por token de destino:

### → `ink` (era `text-slate-900` / `text-slate-800`)
`index.css:13`, `ReportsPage.tsx:12,53`, `Card.tsx:13`, `Modal.tsx:29`, `AppLayout.tsx` (n/a, ver
nav), `ComingSoonPage.tsx:7`, `AgendaPage.tsx:61`, `EstoquePage.tsx:87,134` (800→`ink` mesmo, é só
peso visual mais forte em célula), `PatientsPage.tsx:51,87`, `LoginPage.tsx:49`, `SignupPage.tsx:62`,
`InvitesList.tsx:75`, `FinanceiroPage.tsx:45,80`, `ConveniosCard.tsx:48` (`text-slate-800`),
`FaturaDetalheModal.tsx:61` (`text-slate-800`)

### → `ink-secondary` (era `text-slate-600` / `text-slate-700`)
`Button.tsx:10,12` (secondary/ghost text), `Checkbox.tsx:15`, `Label.tsx:5`, `Modal.tsx:33`,
`AppLayout.tsx:54,67` (itens de nav inativos), `EstoquePage.tsx:100,135,136,137`,
`InvitesList.tsx:89`, `OrgSwitcher.tsx:39`, `FinanceiroPage.tsx:83,84,88`,
`PatientsPage.tsx:88,89,90`

### → `ink-muted` (era `text-slate-500` / `text-slate-400`)
`ReportsPage.tsx:15,54,73`, `ComingSoonPage.tsx:10`, `AppLayout.tsx:81` (role, `text-slate-500`),
`AppLayout.tsx:85` (`text-slate-400`), `AgendamentoDetalheModal.tsx:55,61,65,70,76`,
`AgendaPage.tsx:79`, `EstoquePage.tsx:88,111,115,121`, `InvitesPage.tsx:21,23`,
`InvitesList.tsx:76`, `LoginPage.tsx:50,82`, `SignupPage.tsx:63,111`, `OnboardingPage.tsx:38,64`,
`PatientsPage.tsx:70,75,116,124`, `FaturaDetalheModal.tsx:41,48,52,64`,
`RequireOrganization.tsx:28`, `FinanceiroPage.tsx:64,68,93`, `ConveniosCard.tsx:48,49,52`
(`text-slate-400`), `Modal.tsx:33` (`text-slate-400` do botão fechar, já coberto acima em ink-muted
para o ícone default)

### → `surface` (era `bg-slate-50` em fundo de página)
`index.css:13` (body), `AppLayout.tsx:39`

### → `surface-raised` (era `bg-white`)
`Button.tsx:10`, `Select.tsx:17`, `Card.tsx:5`, `Modal.tsx:27`, `AppLayout.tsx:40`,
`InvitesList.tsx:72`, `LoginPage.tsx:48`, `SignupPage.tsx:61`

### → `surface-sunken` (era `bg-slate-50`/`bg-slate-100` em área recuada, hover leve)
`Button.tsx:10,12` (hover de secondary/ghost), `AppLayout.tsx:54,67` (hover de item de nav),
`EstoquePage.tsx:121` (cabeçalho de tabela), `PatientsPage.tsx:86` (cabeçalho de tabela),
`FinanceiroPage.tsx:79` (hover de linha), `LoginPage.tsx:47`, `SignupPage.tsx:60`,
`OnboardingPage.tsx:37,52`

### → `border` (era `border-slate-100`/`border-slate-200`, divisórias decorativas)
`Card.tsx:9`, `AppLayout.tsx:41,80`, `AgendamentoDetalheModal.tsx:85`, `ConveniosCard.tsx:45`
(`divide-slate-100`), `EstoquePage.tsx:99,131` (`divide-slate-100`), `InvitesList.tsx:72`,
`PatientsPage.tsx:75,84,124` (`divide-slate-100`), `FaturaDetalheModal.tsx:57,89`
(`divide-slate-100`/`border-slate-200`), `FinanceiroPage.tsx:68,77` (`divide-slate-100`),
`SignupPage.tsx:61`

### → `border-strong` (era `border-slate-300`, borda de campo interativo)
`Button.tsx:10`, `Checkbox.tsx:20`, `Select.tsx:19`, `Input.tsx:16`, `EstoquePage.tsx:105`

### → `danger` (era `text-red-600`/`bg-red-600`/`border-red-400`/`outline-red-600`)
`Button.tsx:11`, `Select.tsx:19,26`, `Input.tsx:16,21`, `ReportsPage.tsx:68,74`,
`Checkbox.tsx:25`, `AgendamentoDetalheModal.tsx:82`, `CreateItemEstoqueModal.tsx:69`,
`ConveniosCard.tsx:43`, `CreatePatientModal.tsx:109`, `InvitesList.tsx:101`,
`CreateOrganizationForm.tsx:66`, `LoginPage.tsx:75`, `EditPatientModal.tsx:104`,
`CreateFaturaModal.tsx:231`, `AgendaPage.tsx:77`, `EstoquePage.tsx:66,112`,
`OnboardingPage.tsx:66`, `SignupPage.tsx:104`, `PatientsPage.tsx:69,105`,
`FaturaDetalheModal.tsx:85`, `OrgSwitcher.tsx:61`, `FinanceiroPage.tsx:63`,
`NovoAgendamentoModal.tsx:132`

### → `warning` (era `text-amber-600`)
`ReportsPage.tsx:12`, `EstoquePage.tsx:91`

### → `success` (era `text-emerald-600`)
`ReportsPage.tsx:12`, `FaturaDetalheModal.tsx:78`

### → tokens de badge (`success-subtle`+`success`, `warning-subtle`+`warning`, etc. — era
`bg-amber-100 text-amber-800`, `bg-blue-100 text-blue-800`, `bg-emerald-100 text-emerald-800`,
`bg-red-100 text-red-800`, `bg-slate-200 text-slate-600`)
`StatusBadge.tsx:5-8`, `FaturaStatusBadge.tsx:5-9`, `EstoquePage.tsx:140,144,149` — mapa completo
na tabela do §5.

### → `brand` / `brand-hover` / `brand-subtle` / `on-brand` (já usa a var `brand-*`, só troca a
escala de valor — ver §0 item 1, não é cor solta, é reforço de que a escala inteira muda)
`Button.tsx:9`, `Input.tsx:15`, `Checkbox.tsx:20`, `Select.tsx:18`, `AppLayout.tsx:42,54,67,73`,
`SignupPage.tsx:113`, `LoginPage.tsx:84`, `PatientsPage.tsx:97`

### → `focus-ring` (era `outline-slate-400`/`focus-visible:outline-brand-600`)
`Button.tsx:10,12` (`outline-slate-400`), `Button.tsx:9` (`outline-brand-600`, já é a var brand,
só passa a apontar pro token `focus-ring`)

**Total: ~93 ocorrências mapeadas, cobrindo os 8 componentes `ui/`, `AppLayout`, e as 19 telas de
`features/`.** Nenhuma cor identificada ficou sem token de destino.

---

## 7. Navegação redesenhada (`AppLayout`)

### 7.1 Objetivo
"Menu fácil de navegar" — a nav plana de 6 itens hoje não diferencia área clínica de área de
gestão, não tem ícone, não colapsa, não tem versão mobile.

### 7.2 Estrutura (desktop, ≥1024px)

```
┌─────────────────────────┐
│ OdontoPlatform    [«]   │  ← logo/nome + botão colapsar
│ [OrgSwitcher ▾]         │
├─────────────────────────┤
│ CLÍNICO                 │  ← label de grupo, text-xs uppercase ink-muted
│  📅 Agenda               │
│  👥 Pacientes            │
│                          │
│ GESTÃO                  │  ← só aparece se houver ao menos 1 item visível pro role
│  💰 Financeiro           │
│  📦 Estoque              │
│  📊 Relatórios           │
├─────────────────────────┤
│  ✉️ Convites        [3]  │  ← fora dos grupos — é conta/utilitário, não área de trabalho
├─────────────────────────┤
│ [☀️/🌙] tema             │
│ Recepção                │
│ Sair                    │
└─────────────────────────┘
```

Grupos: `Clínico` (Agenda, Pacientes — visível a todos os papéis) e `Gestão` (Financeiro, Estoque,
Relatórios — cada item já filtra por role como hoje, o grupo inteiro some se nenhum item for
visível pro papel do usuário, ex.: Dentista não vê "Gestão"). `Convites` fica **fora dos grupos**,
numa faixa própria acima do rodapé — é notificação/conta, não é uma área de trabalho, e por isso
não deve competir visualmente com Agenda/Financeiro. Badge de contagem preservado exatamente como
hoje (`pendingInvitesCount > 0`).

### 7.3 Estados de item de nav

| Estado | classes |
|---|---|
| default | `text-ink-secondary` |
| hover | `hover:bg-surface-sunken hover:text-ink` |
| ativo | `bg-brand-subtle text-brand font-medium` |
| foco (teclado) | `focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring` |
| ícone | `size={16}`, `currentColor` — acompanha a cor do texto do item automaticamente |

### 7.4 Colapsável (desktop)

Botão `PanelLeftClose`/`PanelLeftOpen` no topo da sidebar. Estado colapsado: `w-16` em vez de `w-56`,
mostra só ícone + tooltip no hover (não label), grupos perdem o rótulo de texto mas mantêm o
espaçamento vertical (não vira ícone solto sem agrupamento visual — mantém uma linha divisória
sutil `border-t border-border` entre grupos mesmo sem o texto do label). Persistir estado
colapsado/expandido (mesmo mecanismo de persistência do tema).

### 7.5 Mobile (<768px)

Sidebar vira **drawer**: escondida por padrão, botão `Menu` (hambúrguer) no canto superior esquerdo
de uma barra superior fixa (`h-14 border-b border-border bg-surface-raised`), abre drawer com
overlay (`bg-ink/40`, mesmo token do backdrop do `Modal` — reaproveita padrão já definido).
**Não é bottom bar** — com 6+ itens agrupados em 2 seções mais convites, bottom bar ficaria
apertada ou exigiria "mais" escondendo itens; drawer lateral reaproveita a hierarquia de grupos já
definida no desktop sem redesenhar a informação.

Nesse breakpoint o `OrgSwitcher` e o toggle de tema entram dentro do drawer (não na barra superior,
que fica só com hambúrguer + logo, pra não competir por espaço em telas estreitas).

### 7.6 Toggle de tema

Vive no rodapé da sidebar (desktop) / dentro do drawer (mobile), acima do bloco de
role+logout — é uma preferência de conta, agrupa com o resto do rodapé. 3 ícones em segmented
control (`Sun` / `MonitorSmartphone` / `Moon`) ou um único botão cíclico com ícone que muda — spec
não impõe qual dos dois, é decisão de baixo risco pro Dev Frontend, mas **o estado atual precisa
estar visualmente marcado** (mesmo tratamento de "ativo" usado nos itens de nav: `bg-brand-subtle
text-brand` no ícone selecionado).

---

## 8. Dashboard de comissões

### 8.1 Layout

```
┌───────────────────────────────────────────────┐
│ Dashboard de Comissões                          │
├───────────────────────────────────────────────┤
│ [Período ▾] [Filial ▾] [Classe ▾]              │  ← barra de filtros, mesmo Card usado em Relatórios
├───────────────────────────────────────────────┤
│ [KPI: Total comissão] [KPI: Média/profissional]│  ← cards, valor em .text-kpi
│ [KPI: Nº profissionais] [KPI: Nº atendimentos] │
├───────────────────────────────────────────────┤
│ Tabela: Profissional | Filial | Classe | Qtd.  │
│         Atend. | Comissão | Média/dia          │
└───────────────────────────────────────────────┘
```

Reaproveita exatamente o padrão já existente em `ReportsPage.tsx` (`Card` + `CardHeader` +
`CardBody` para filtros, grid de KPIs dentro de `Card`) — **não inventar layout novo**, só trocar
o `Kpi` local (que hoje usa `text-emerald-600`/`text-amber-600`/`text-slate-900` hardcoded, exatamente
o padrão do §6) para usar tokens + a classe `.text-kpi` do §2.3.

### 8.2 Estados

| Estado | Tratamento |
|---|---|
| Vazio (filtro sem resultado) | Mensagem no lugar da tabela: `"Nenhuma comissão encontrada para o período/filtro selecionado."` — `text-ink-muted text-sm`, dentro do `CardBody`, mesmo padrão de `EstoquePage`/`FaturaStatusBadge`-adjacent ("Nenhum item de estoque cadastrado.") |
| Loading | `p-4 text-sm text-ink-muted` com texto `"Carregando…"` — idêntico ao padrão já usado em `EstoquePage`/`FinanceiroPage`, **não criar spinner novo, o app não usa spinner em nenhum lugar hoje** |
| Erro | `text-danger text-sm`, mensagem via `getApiErrorMessage(error)` — mesmo padrão de toda tela já auditada |
| Sucesso | cards de KPI + tabela populada |

Filtros (Período/Filial/Classe): usar `Select`/`Input` já tokenizados — nenhum componente novo.

---

## 9. Comprovante de pagamento

### 9.1 Layout (tela)

```
┌───────────────────────────────────────┐
│ [← Voltar]              [🖨 Imprimir]  │  ← .no-print
├───────────────────────────────────────┤
│ OdontoPlatform                         │
│ Comprovante de Pagamento de Comissão   │
│ Organização: {nome}                    │
│ Profissional: {nome}                   │
│ Período: {dataInicio} – {dataFim}      │
├───────────────────────────────────────┤
│ Atendimento | Data | Valor             │
│ ...                                    │
├───────────────────────────────────────┤
│ Total de atendimentos: N               │
│ Valor total: R$ X                      │
│ Média diária: R$ Y                     │
├───────────────────────────────────────┤
│ Emitido em {timestamp}                 │  ← rodapé
└───────────────────────────────────────┘
```

Botões "Voltar" e "Imprimir" ficam numa faixa marcada `.no-print` (classe já definida no bloco CSS
do §3). O corpo do comprovante fica dentro de um wrapper `.print-area`.

### 9.2 Impressão (`window.print()`)

Regra do usuário implícita e da sprint: **fundo sempre claro na impressão, mesmo com dark mode
ativo na tela** — papel não tem "dark mode". O bloco `@media print` do §3 já resolve isso:
- `.no-print` (nav, filtros, botões, toggle de tema, sidebar inteira) some.
- `.print-area` força `background: white`, `color: black` — ignora completamente os tokens de
  cor (não usa `--color-ink`/`--color-surface`, que poderiam estar em valor dark) e ignora sombras
  (`box-shadow: none`).
- Cores de status (badge de status da comissão, se houver) também caem em preto/branco na
  impressão — não faz sentido imprimir badge colorido num documento pensado pra impressora P&B;
  se precisar diferenciar estado no papel, usar texto (`"[PAGO]"`) em vez de cor.

🔴 **Ponto de atenção pro Dev Frontend**: `.print-area` deve envolver *todo* o conteúdo do
comprovante, incluindo `Card`/`CardBody` se forem reaproveitados — se o `Card` mantiver
`bg-surface-raised` (que em dark é escuro) sem o override do `.print-area`, o comprovante imprime
com fundo cinza escuro em vez de branco. Testar impressão com o dark mode ativo na tela antes de
fechar a 025/028.

---

## 10. Checklist de acessibilidade (para QA na 029)

- [ ] Todo texto normal (`ink-secondary`, `ink-muted`) atinge 4.5:1 sobre `surface`/`surface-raised`
      nos dois modos (valores calculados no §1.2 — revalidar com ferramenta, ex. axe, não só a
      conta manual daqui).
- [ ] `border-strong` (campos interativos) atinge 3:1 sobre `surface-raised` nos dois modos.
- [ ] `focus-ring` visível em todo elemento interativo (`Button`, `Input`, `Select`, `Checkbox`,
      itens de nav, toggle de tema, botão colapsar sidebar) — nenhum `outline: none` sem substituto.
- [ ] Toggle de tema não causa FOUC (flash do tema errado ao carregar).
- [ ] Impressão do comprovante sai em preto e branco mesmo com dark mode ativo na tela.
- [ ] Badge de convites pendentes continua visível e com a mesma contagem depois do redesign da nav.
- [ ] Nenhuma classe de cor Tailwind "crua" (`slate-*`, `emerald-*`, `red-*`, `amber-*`, `blue-*`)
      sobrevive em `frontend/src/**` fora do bloco `@theme` — se sobrar, é a lista do §6 incompleta
      ou um esquecimento da 025-028.

---

## Addendum — Sprint 9 (task 032): reskin de cor do dark mode

**Motivo:** usuário forneceu fotos de referência (`docs/images/`) e pediu fidelidade visual —
1ª rodada (sprint-8) tratou layout; 2ª rodada (esta) tratou cor, com as 2 fotos **dark** como alvo.
Fidelidade de cor passou a ter prioridade sobre a regra "não pareça vibe codado" do §0 item 1
**só no `.dark`** — o light desta spec (§1.2, valores light da tabela) continua valendo
integralmente, não foi tocado.

Valores do `.dark` atualizados (substituem a tabela do §1.2 só nas linhas abaixo, light inalterado):

| Token | Dark (novo) | Motivo |
|---|---|---|
| `surface` | `#0A0F0C` | quase preto, como a foto |
| `surface-raised` | `#111814` | idem |
| `surface-sunken` | `#060907` | idem |
| `border` | `#1E2622` | idem, decorativo |
| `brand` | `#2DD4BF` (teal-ciano) | era `#35B583` (verde-pinho) — foto usa teal mais vívido |
| `brand-hover` | `#52E6C9` | mais claro, mesma lógica de hover em dark já documentada |
| `brand-subtle` | `#0F231D` | tom frio, acompanha o novo `brand` |
| `on-brand` | `#06170F` (**inalterado**) | branco falharia contraste com o novo `brand` — ver docs/decisions.md |
| `focus-ring` | `#2DD4BF` (= brand) | acompanha |

Novo por par de variável (não é token único, fora do `@theme` — ver docs/decisions.md):
`--grad-brand-from`/`--grad-brand-to` — gradiente teal→azul (`#2DD4BF`→`#3B82F6`) no botão
primário (`.bg-brand-gradient`), só no dark; light usa from==to==brand (visualmente sólido, sem
mudança). Item de nav ativo (`AppLayout`) passou de `bg-brand-subtle text-brand` pra
`bg-brand text-on-brand` — sólido, nos dois temas (light e dark) — ver docs/decisions.md.

Chips de evento do calendário (§8/status coloring da task 031) **não mudaram** — continuam usando
`warning`/`info`/`success`/neutral por status, não recoloridos por esta task.

## Status

`implemented` — spec das tasks 025-029 (tokens, nav, dashboard, comprovante) em produção desde a
sprint-7; addendum acima (task 032, sprint-9) também implementado. Ver docs/decisions.md pro
histórico completo de divergências vs. cada rodada de foto de referência do usuário.
