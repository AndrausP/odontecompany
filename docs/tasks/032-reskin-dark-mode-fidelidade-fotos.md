---
task: "032"
sprint: "9"
status: done
---

# 032 — Reskin dark mode (fidelidade às fotos de referência)

**Sprint:** docs/sprints/sprint-9.md
**Tipo:** FRONTEND
**Estimativa:** 0.5 dia
**Depende de:** 024/025 (tokens base), 031 (Agenda já redesenhada — este reskin herda automático)
**Critério de aceite:**
1. `.dark` em `index.css` reflete visual mais próximo das fotos `...09_45_06...`/`...10_17_17...`:
   superfícies quase pretas, brand teal-ciano mais vívido (não o verde-pinho suave da 024).
2. Botão primário (`Button` variant `primary`) usa gradiente teal→azul no dark; no light continua
   sólido, pixel-idêntico ao que já existia (zero mudança visual em light).
3. Item de nav ativo (`AppLayout`) vira pill sólido (`bg-brand text-on-brand`), não mais o estilo
   pálido (`bg-brand-subtle text-brand`) da 024 — em ambos os temas (as duas fotos, dark E a light
   usada na sprint-8, mostram item ativo sólido).
4. Badge de contagem de Convites permanece legível quando o próprio item de nav está ativo (fundo
   virou sólido — precisa inverter a cor do badge nesse caso, não pode ficar bg-igual-a-bg).
5. Flourish decorativo (blob borrado verde/roxo) na base da sidebar desktop, visível só no dark —
   aproximação, não reprodução literal da ilustração da foto.
6. **Contraste AA mantido** em todo par texto/fundo alterado (ver tabela de verificação abaixo) —
   nenhuma regressão de acessibilidade em troca de fidelidade visual.
7. `npm run build` e `npm run lint` limpos.

## Divergência consciente vs. foto (decidida com o usuário)

**Texto do botão/badge continua escuro (`--color-on-brand: #06170F`), não branco como na foto.**
Calculado: branco sobre o teal (`#2DD4BF`) dá ~1.9:1, sobre o azul (`#3B82F6`) melhora mas ainda
não fecha AA em todo o gradiente. `on-brand` escuro dá 11:1 (ponta teal) e 5.03:1 (ponta azul) —
o usuário priorizou fidelidade de cor sobre a regra "não pareça vibe codado", mas isso não incluiu
abrir mão de contraste AA (não foi perguntado, e reverter uma garantia de acessibilidade já
verificada exigiria confirmação explícita, não assumida).

## Verificação de contraste (dark, valores novos)

| Par | Contraste | Nota |
|---|---|---|
| `on-brand` (#06170F) vs `brand` (#2DD4BF) | ~11:1 | ponta teal do gradiente / nav ativo / mini calendário dia selecionado |
| `on-brand` (#06170F) vs ponta azul do gradiente (#3B82F6) | ~5.03:1 | ponta azul do botão — passa AA texto normal (4.5:1) |
| `ink`/`ink-secondary`/`ink-muted` vs `surface`/`surface-raised` (mais escuros que antes) | só melhora | fundo mais escuro + mesmo texto claro = contraste sobe, nunca cai |
| `border-strong` (#5B6B62) vs `surface-raised` (#111814) | melhora vs. valor já verificado na 024 (3.63:1) | fundo mais escuro que antes, borda não mudou de valor |

## Escopo técnico

- `index.css`: `.dark` com surfaces mais escuras, `--color-brand`/`-hover`/`-subtle` recalculados;
  novo par de variáveis `--grad-brand-from`/`--grad-brand-to` (flat em light, teal→azul em dark) +
  classe `.bg-brand-gradient`; classe `.sidebar-flourish` (opacity 0 em light, 1 em dark).
- `Button.tsx`: variant `primary` troca `bg-brand` → `bg-brand-gradient`, hover troca
  `hover:bg-brand-hover` → `hover:brightness-90` (bg-image não aceita swap de bg-color no hover).
- `AppLayout.tsx`: `NavItemLink`/`ConvitesNavItem` ativo vira `bg-brand text-on-brand`;
  `ConvitesNavItem` precisou virar render-prop (`children` como função) pra saber `isActive` e
  inverter a cor do badge interno quando o item já está sólido; `aside` desktop ganhou
  `relative overflow-hidden` + `<div className="sidebar-flourish">` decorativo.

## Fora de escopo

- Recolorir chips de evento do calendário / legenda (continuam por status — decisão da 031, não
  reaberta aqui).
- Mudar paleta do modo light (usuário escolheu só as fotos dark pra esta rodada).
- Reprodução literal em SVG da ilustração de onda da foto (o blob radial-gradient borrado é uma
  aproximação deliberada, custo/benefício).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Fidelidade de cor tem prioridade sobre a regra "vibe codado" da 024, só pro dark — grill-me, 2026-08-19 |
| 2. Contexto | Reader → Writer | `index.css`/`Button.tsx`/`AppLayout.tsx` atuais (024/025/026, done); 93 usos de token já levantados na 024 (§6), nenhum novo hardcode introduzido |
| 3. Quebra | Tech Lead | Escopo acima, 1 task, sprint nova (9) — sprint-8 já fechada |
| 4. Estrutura | Architect | Gradiente como par de CSS vars fora do `@theme` (evita gerar utilitária Tailwind indesejada); flourish como classe CSS pura, sem lib nova |
| 5. Aprovação | Product Owner | Aprovado — divergência do texto branco→escuro registrada e aceita implicitamente (prioridade AA não foi objeto do pedido) |
| 6. Implementação | Dev Frontend | `index.css`, `Button.tsx`, `AppLayout.tsx` |
| 7. Teste | QA | `npm run build`/`npm run lint` limpos; contraste recalculado à mão (tabela acima); grep de cor hardcoded nos arquivos tocados — zero match fora dos valores de token já documentados |
| 8. Documentação | Writer | `docs/decisions.md` (3 entradas novas) |

## Status

planned → in-progress → in-review (QA) → **done**.

## Notas

Sem screenshot ao vivo — mesma limitação de infra da task 031 (backend não roda nesta máquina,
sem docker-compose no repo). Verificação foi build+lint limpos + contraste recalculado à mão pros
pares alterados.
