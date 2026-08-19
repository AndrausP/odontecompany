---
task: "025"
sprint: "7"
status: done
---

# 025 — Fundação visual: tokens light/dark, ThemeProvider e re-skin dos componentes `ui/`

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** FRONTEND
**Estimativa:** 2 dias
**Depende de:** 024 (design system)
**Critério de aceite:**
1. Tokens da task 024 aplicados em `frontend/src/index.css`; alternar o tema muda a aparência de
   **todas** as telas existentes (Agenda, Pacientes, Financeiro, Estoque, Relatórios, Convites,
   Login, Signup, Onboarding) sem nenhuma ficar ilegível.
2. Existe um toggle claro/escuro funcional; a escolha **persiste entre reloads**.
3. Primeira visita sem escolha salva respeita `prefers-color-scheme` do sistema.
4. Sem flash de tema errado no carregamento (FOUC).
5. Os 8 componentes de `components/ui/` usam **apenas tokens semânticos** — zero `slate-*`,
   `emerald-*` ou hex hardcoded neles.
6. `npm run build` passa sem erro de TypeScript.

## Escopo técnico

1. **`index.css`**: substituir o bloco `@theme` atual pelos tokens da 024 (light + dark).
   Remover o `@apply bg-slate-50 text-slate-900` hardcoded do `body` — passa a usar token.
2. **Estratégia de dark mode — Architect decide.**

**A5: `.dark` class + `@custom-variant dark` em `index.css`**

Implementação:
- **ThemeProvider**: estado Zustand `'light' | 'dark' | 'system'`, persistido em localStorage
- **Script inline** em `index.html` (antes do React render): se houver localStorage, aplica classe `.dark` ao `<html>` imediatamente (evita FOUC)
- **Fallback**: sem localStorage = respeita `@media (prefers-color-scheme: dark)` do SO
- **Tokens em `index.css`**: bloco `@custom-variant dark { @media (prefers-color-scheme: dark) }` e todos os tokens semânticos com valor light/dark via CSS variables

Motivo: Tailwind v4 nativo suporta `@custom-variant`; permite toggle manual (exigência do usuário) + boot respeitando SO; sem necessidade de `data-theme` separado. ThemeProvider gerencia o estado e toca a classe `.dark` no `<html>` quando o usuário alterna (React lifecycle, sem delay).
3. **`ThemeProvider`** (`frontend/src/lib/` ou `components/theme/`): estado `light | dark | system`,
   persistência em `localStorage`, leitura de `prefers-color-scheme`, aplicação da classe/atributo
   no `<html>`. Script inline no `index.html` pra aplicar o tema **antes** do React montar (evita
   FOUC — critério 4).
4. **Re-skin dos 8 componentes `ui/`** conforme a spec da 024, trocando utilitário hardcoded por
   token. API pública de cada componente **não muda** — nenhuma página existente pode quebrar.
5. **Varredura das páginas existentes**: trocar cor hardcoded por token nas telas já prontas. É o
   grosso do trabalho, não subestimar (ver R5 da task 024). Não redesenhar layout aqui — só cor.

## Fora de escopo

- Redesenhar o `AppLayout`/nav (é a 026).
- Telas novas de dashboard/comprovante (027/028).

## Riscos

- **R6 (alto):** volume de arquivos tocados. Se a spec da 024 não listar todos os utilitários de
  cor em uso, o Dev descobre no meio e a task estoura de 2 pra 4 dias. Mitigação: a 024 entrega a
  lista; se não entregar, **não liberar** a 025.
- **R7 (médio):** dark mode expõe contraste ruim em componentes que hoje "funcionam" só no claro
  (`StatusBadge`, `FaturaStatusBadge`, tons de erro em vermelho). Revisar badge por badge.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Modo claro E escuro (sprint-7) |
| 2. Contexto | Reader → Writer | Tailwind v4 `@theme`, cores hardcoded espalhadas |
| 3. Quebra | Tech Lead | Escopo acima |
| 4. Estrutura | Architect | **A5**: `.dark` class + `@custom-variant dark` em `index.css` (acima) |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Frontend | [pendente] |
| 7. Teste | QA | [na 029] |
| 8. Documentação | Writer | documentado — ver docs/knowledge/patterns.md (tokens semânticos `ink*`, dark mode `.dark`+`@custom-variant`+script anti-FOUC) |

## Status

planned → in-progress → in-review (QA) → **done**

Varredura das 18 telas de `features/` concluída (`bg-gray-*`/`text-gray-*`/`border-gray-*`/
`bg-emerald-*`/etc. trocados pelos tokens do §1.1). `AgendaPage.tsx` também tinha hex cru
(`#f59e0b`/`#3b82f6`/`#10b981`/`#94a3b8`) no mapa de cor de evento do FullCalendar — fora do
grep de classes Tailwind do §6, corrigido à parte usando `var(--color-*)` inline (acompanha o
dark mode automaticamente, já que FullCalendar não aceita classe utilitária, só string de cor).
`PatientsPage.tsx:97` usava `text-brand-700` (escala numerada antiga) — trocado por `text-brand`.
`StatusBadge`/`FaturaStatusBadge` já estavam consolidados em `lib/status-tone.ts` (sessão
anterior). `ReportsPage.tsx` já estava consistente (tokens aplicados, sem cor hardcoded) —
nenhuma alteração necessária. `npm run build` (`tsc -b && vite build`) limpo, 0 erro.
`grep -rE "(bg|text|border)-(gray|slate|emerald|green|red|blue|yellow|amber|indigo)-[0-9]+" src
--include=*.tsx` → 0 ocorrências.

## Notas

Bloqueia 026, 027 e 028. Fazer **antes** de qualquer tela nova — construir tela nova com cor
hardcoded e tokenizar depois é retrabalho garantido.
