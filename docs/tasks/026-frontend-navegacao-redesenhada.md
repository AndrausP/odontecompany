---
task: "026"
sprint: "7"
status: done
---

# 026 — Navegação redesenhada (`AppLayout`) + toggle de tema

**Sprint:** docs/sprints/sprint-7.md
**Tipo:** FRONTEND
**Estimativa:** 2 dias
**Depende de:** 025 (tokens + ThemeProvider), 024 (spec de nav)
**Critério de aceite:**
1. `AppLayout.tsx` implementa a navegação especificada na 024: itens agrupados por área, com
   ícone, estado ativo/hover/foco visíveis nos dois temas.
2. Sidebar colapsa e o estado de colapso persiste.
3. Em tela estreita (<768px) a navegação é usável — hoje não é (sidebar fixa de 224px).
4. Toggle claro/escuro acessível pela navegação, na posição definida pela 024.
5. **Filtro por role preservado**: `Financeiro`/`Estoque` só pra `Admin`/`Recepcao`, `Relatórios`
   só pra `Admin`, e `claims?.role` continua tratado como possivelmente ausente (usuário sem
   organization ativa **não pode** quebrar a tela).
6. Badge de convites pendentes preservado, alimentado pelo mesmo `useMe()` (`queryKey ['me']` —
   nenhum request extra novo).
7. `OrgSwitcher` continua funcional na nova estrutura.
8. Navegação inteira operável por teclado (Tab/Enter), com foco visível.

## Escopo técnico

- Reescrita de `frontend/src/components/layout/AppLayout.tsx` seguindo a spec.
- Item de nav novo pro **Comprovante** (rota da task 028) — visível pra `Owner`/`Admin`/`Dentista`,
  **oculto pra `Recepcao`** (que recebe 403 no endpoint; item de menu que sempre dá erro é bug de
  UX, não feature).
- **A6 — Ícones**: se task 024 especificar ícones, adicionar `lucide-react` ao `package.json`.
  Motivo: nenhuma lib instalada hoje; `lucide-react` é leve, bem mantida (zero deps pesadas), segue
  design minimalista do projeto. Se a 024 não pedir ícones, não adicionar.
- Estado de colapso em `localStorage`, mesmo padrão do ThemeProvider da 025.

## Fora de escopo

- Mudar rotas do `App.tsx` além da rota nova de comprovante.
- Mexer em permissão de backend.

## Riscos

- **R8 (baixo):** `AppLayout` é o componente mais transversal do app — quebrá-lo derruba toda a
  navegação. Mitigação: preservar exatamente a lógica de `visibleItems` e `pendingInvitesCount`
  que já existe; é re-skin estrutural, não mudança de regra de acesso.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | "Menu fácil de navegar" (sprint-7) |
| 2. Contexto | Reader → Writer | AppLayout atual: sidebar 224px, nav plana, filtro por role |
| 3. Quebra | Tech Lead | Escopo acima |
| 4. Estrutura | Architect | [pendente — só se a 024 pedir dependência nova de ícones] |
| 5. Aprovação | Product Owner | [pendente] |
| 6. Implementação | Dev Frontend | [pendente] |
| 7. Teste | QA | [na 029] |
| 8. Documentação | Writer | documentado — sem padrão novo digno de entrada própria (re-skin estrutural do `AppLayout`, lógica de RBAC/visibleItems preservada, não é padrão novo, ver docs/knowledge/patterns.md "RBAC de escopo forçado pelo controller" já cobre a base) |

## Status

planned → in-progress → in-review (QA) → **done**. `npm run build` limpo em `frontend/`
(`tsc -b && vite build`, 0 erros). `AppLayout.tsx` reescrito seguindo a spec §7; `lucide-react`
adicionado ao `package.json` (`^1.32.0`). Detalhes da implementação no handoff `[dev→qa]` abaixo.

## Notas

**Task mais cortável da sprint.** Se o prazo apertar, a 025 já entrega a paleta nova no layout
atual (o menu fica verde e com dark mode, só não agrupado/colapsável/mobile). Cortar a 026 não
compromete nenhuma regra de negócio — as 027/028 não dependem dela, só da 025. Ver "Notas do Tech
Lead" na sprint-7.
