---
task: "024"
sprint: "7"
status: done
---

# 024 — Sistema visual: paleta verde+branco, tokens light/dark, spec de navegação

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** DESIGN
**Estimativa:** 2 dias
**Depende de:** Nenhuma (roda em paralelo com 022/023)
**Critério de aceite:** Existe `docs/design/design-system.md` com: (a) paleta completa em tokens
semânticos, com valor definido para **light e dark**; (b) escala tipográfica e de espaçamento;
(c) spec dos componentes de `frontend/src/components/ui/` já existentes; (d) spec da navegação
redesenhada; (e) spec das duas telas novas (dashboard e comprovante). **Nenhum arquivo de
`frontend/src/features/` é tocado nesta task** — o Designer entrega spec + tokens, o Dev Frontend
aplica nas tasks 025-028.

## Por que esta task existe antes do frontend

Regra do usuário: *"não pode parecer código gerado sem cuidado visual"*. Se o Dev Frontend
inventar cor a cada tela, o resultado é inconsistente por construção. Design vem primeiro, com
tokens fechados. **Esta etapa não pode ser pulada.**

## Contexto (o que já existe)

- Tailwind **v4** com `@theme` em `frontend/src/index.css`. Já existe uma escala `brand-*` em
  emerald (`#ecfdf5 → #064e3b`) — **ponto de partida, não recomeço do zero**.
- `frontend/src/components/ui/`: `Button`, `Input`, `Label`, `Select`, `Card`, `Modal`,
  `StatusBadge`, `Checkbox`.
- `frontend/src/components/layout/AppLayout.tsx`: sidebar fixa de 224px, nav plana de 6 itens
  (Agenda, Pacientes, Financeiro, Estoque, Relatórios, Convites), `OrgSwitcher` no topo, role +
  logout no rodapé. Hoje usa `bg-slate-50` hardcoded no body e nos itens.
- Cores hoje estão **hardcoded por classe utilitária** (`text-slate-900`, `bg-slate-50`,
  `text-emerald-600`) espalhadas nas páginas — por isso dark mode exige tokens semânticos.

## Escopo

### 1. Tokens semânticos (o entregável mais importante)

Nomear por **função**, não por cor. Mínimo:
`surface`, `surface-raised`, `surface-sunken`, `border`, `border-strong`, `text-primary`,
`text-secondary`, `text-muted`, `brand`, `brand-hover`, `brand-subtle`, `on-brand`,
`success`, `warning`, `danger`, `focus-ring`.

Cada token com valor **light e dark**. Entregar o bloco `@theme` / `@custom-variant dark` pronto
pra colar em `index.css` — o Dev Frontend não deve escolher hex nenhum.

Restrição do usuário: **verde + branco, minimalista**. Verde é acento, não plano de fundo — light
mode é branco/off-white com verde em ação e destaque. Dark mode: superfícies neutras escuras
(não preto puro), verde ajustado pra manter contraste.

**Contraste AA (4.5:1 texto normal, 3:1 texto grande e bordas de foco) é obrigatório nos dois
modos** — verificado e anotado no documento, não presumido.

### 2. Tipografia e espaçamento

Escala tipográfica (máx. 5 tamanhos), pesos, altura de linha. Escala de espaçamento e raio de
borda. Minimalista = poucas variações, aplicadas com disciplina.

### 3. Spec dos componentes `ui/` existentes

Para cada um dos 8: estados (default/hover/focus/disabled/error) nos dois modos, usando só tokens.
Sem redesenhar a API dos componentes — é re-skin, não reescrita.

### 4. Navegação redesenhada

Pedido do usuário: *"menu fácil de navegar"*. Spec deve cobrir:
- Agrupamento dos itens por área (ex.: Clínico / Financeiro / Gestão) — nav plana de 6 itens vira
  agrupada.
- Ícone por item (definir o set — inclui a decisão de qual biblioteca de ícones, se alguma).
- Estado ativo, hover e foco por teclado.
- Sidebar colapsável e comportamento em tela estreita (a app hoje **não tem** tratamento mobile na
  sidebar — definir: drawer, bottom bar ou colapso).
- Onde vive o **toggle light/dark** e como ele se apresenta.
- Badge de convites pendentes preservado (já existe, não perder).

### 5. Spec das telas novas

- **Dashboard de comissões**: barra de filtros (período + filial + classe), cards de KPI, tabela
  por profissional, estado vazio e estado de carregamento.
- **Comprovante de pagamento**: layout tipo documento (cabeçalho com organization/profissional/
  período, corpo com valores e média diária, rodapé). **Precisa de uma versão de impressão** —
  a sprint decidiu `window.print()` + CSS de impressão, sem PDF no backend. Especificar o que
  some na impressão (nav, filtros, botões) e como fica em preto e branco.

## Fora de escopo

- Escrever/alterar código em `frontend/src/features/**` ou `frontend/src/components/**`.
- Logo ou identidade de marca nova.
- Animações complexas / micro-interações elaboradas.

## Riscos

- **R5 (médio):** dark mode exige trocar cor hardcoded por token em **todas** as páginas já
  existentes (Agenda, Pacientes, Financeiro, Estoque, Relatórios, Auth). Se a spec não cobrir os
  padrões usados nessas telas, a 025 estoura. Designer deve varrer as páginas atuais e listar todo
  utilitário de cor hardcoded que precisa de token equivalente.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Verde+branco, minimalista, dark/light, nav fácil (sprint-7) |
| 2. Contexto | Reader → Writer | Tailwind v4 `@theme`, brand emerald já existe, 8 componentes ui/ |
| 3. Quebra | Tech Lead | Escopo acima; entregável = documento + bloco de tokens |
| 4. Estrutura | Architect | **Não bloqueia** — Architect só valida a estratégia de dark mode na 025 |
| 5. Aprovação | Product Owner | [pendente — PO valida o visual antes do Dev Frontend implementar] |
| 6. Implementação | Designer | Entregue: `docs/design/design-system.md` — tokens light/dark, spec dos 8 componentes `ui/`, lista de ~93 cores hardcoded mapeadas, spec de nav/dashboard/comprovante, contraste AA calculado |
| 7. Teste | QA | [na 029 — contraste AA e consistência com a spec] |
| 8. Documentação | Writer | [pendente] |

## Status

**done** — aprovado pelo PO/broker (2026-08-18): paleta corrige causa raiz do "vibe coded"
(brand-* era emerald padrão do Tailwind, trocado por verde-pinho autoral), contraste AA calculado
por luminância relativa real (não estimado), nomenclatura `ink*` evita colisão de utilitária
Tailwind v4, decisão `on-brand` invertido entre light/dark justificada. Consumido nas tasks 025/026
sem reabertura.

## Notas

Bloqueia 025, 026, 027, 028. É o caminho crítico do lado visual — **começar no dia 1 da sprint,
em paralelo com a 022**, senão o frontend fica parado esperando.
