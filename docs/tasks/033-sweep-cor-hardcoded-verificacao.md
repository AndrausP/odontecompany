---
task: "033"
sprint: "11"
status: done
---

# 033 — Sweep de cor hardcoded — verificação completa (8 features)

**Sprint:** docs/sprints/sprint-11.md
**Tipo:** FRONTEND (verificação, sem implementação)
**Estimativa:** 0.25 dia
**Depende de:** 024/025 (tokens base, sprint-7), 032 (dark reskin, sprint-9)
**Critério de aceite:**
1. Zero ocorrência de classe Tailwind com cor hardcoded (`bg-slate-*`, `text-red-*`,
   `border-emerald-*` etc — paleta padrão fora de `ink`/`surface`/`border`/`brand`/`danger`/
   `warning`/`success`/`info`/tokens de badge) em `frontend/src/**/*.{tsx,ts,css}`.
2. Cobertura: `ui/` (componentes base) + `AppLayout` + as 8 features (`auth`, `billing`,
   `estoque`, `patients`, `reports`, `scheduling`, `organizations`, `invites`).
3. `npm run lint` e `npm run build` limpos.

## Contexto

Usuário pediu foco em "polimento" (escopo "tudo", sessão nova). Grill-me travou a regra de
negócio: zero cor hardcoded remanescente = critério de "polido" pra esta rodada. `docs/design/
design-system.md` §6 já documentava 93 ocorrências mapeadas E resolvidas na task 025 (sprint-7);
esta task reverifica que nada foi reintroduzido nas sprints 8 (031, Agenda redesign) e 9 (032,
dark reskin).

## Execução

Grep sistemático (mesmo padrão da 024 §6, ampliado pra cobrir toda a paleta Tailwind padrão, não
só as cores já catalogadas):

```
grep -rnE '\b(bg|text|border|divide|outline|ring)-(slate|red|amber|emerald|blue|gray|zinc|neutral|green|yellow|orange)-\d{2,3}\b' frontend/src --include='*.tsx' --include='*.ts' --include='*.css'
```

**Resultado: 0 matches.** Únicos hex literais remanescentes em `index.css` são as definições dos
próprios tokens (`--color-ink: #131B16` etc, light + dark) — esperado, é onde o valor do token
mora, não uso solto num componente.

`npm run lint` (oxlint) → limpo, sem warnings.
`npm run build` (tsc -b && vite build) → limpo, build ok. Aviso não-relacionado: chunk principal
792.90 kB > limite de 500 kB (sem code-splitting) — performance de bundle, fora do critério de
aceite desta task, registrado na retrospectiva da sprint-11 como possível item futuro.

## Fora de escopo

- Code-splitting / lazy loading do bundle (achado lateral, não é cor).
- Revisão visual em navegador (backend não roda nesta máquina, mesma limitação já registrada nas
  tasks 031/032 — verificação foi grep + build + lint, sem screenshot).

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Zero cor hardcoded = critério de aceite, escopo = 8 features (grill-me, 2026-08-19) |
| 2. Contexto | Reader → Writer | design-system.md §6 (93 ocorrências já resolvidas na 025); sprints 8-9 não tocaram cor fora do token system |
| 3. Quebra | Tech Lead | 1 task, sprint nova (11) — sprint-10 já fechada (backlog backend) |
| 4. Estrutura | Architect | Sem mudança de contrato/schema — verificação pura, sem handoff de implementação |
| 5. Aprovação | Product Owner | Aprovado — achado (frontend já 100% migrado) aceito como resultado válido, não precisa de rework |
| 6-9. Implementação | — | N/A, nada a implementar |
| 10. Teste | QA | grep (0 matches) + `npm run lint` + `npm run build`, ambos limpos |
| 11. Documentação | Writer | `docs/knowledge/patterns.md` (nota de verificação), `docs/decisions.md` |

## Status

planned → in-progress → in-review (QA) → **done**.

## Notas

Resultado é confirmação, não correção — frontend já estava conforme antes desta task rodar. Valor
entregue: reduz a incerteza de "será que sobrou alguma cor solta" pra zero, com evidência
reproduzível (comando de grep documentado acima, roda de novo a qualquer momento).
