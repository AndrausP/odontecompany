---
task: "007"
sprint: "3"
status: done
---

# 007 — Financeiro: convênios (Fase 3)

**Sprint:** docs/sprints/sprint-3.md
**Critério de aceite:** Um adaptador (Anti-Corruption Layer) por convênio; formato externo nunca
vaza pro domínio Financeiro. Comissão de dentistas.

## Cadeia

| Etapa | Agente | Output |
|-------|--------|--------|
| 1. Objetivo | Product Owner | Fatura de convênio isolada do formato externo via ACL; comissão de dentista calculável em qualquer fatura (particular ou convênio). |
| 2-5 | Broker | Mesma cadeia da task 006 — implementadas juntas no mesmo módulo `Billing`. |
| 6. Implementação | Broker (thread principal) | `Convenio` (entidade própria, `CodigoExterno` opcional). `IConvenioAdapter` (ACL) — porta genérica em Billing.Application, `EnviarFaturaAsync(Fatura, Convenio) → Result<string>` (protocolo). Implementação de referência `ManualConvenioAdapter` (Billing.Infrastructure) — SEM integração real com nenhuma operadora, gera protocolo local e loga; documentado que cada convênio real precisa do próprio adaptador. `Fatura.CreateConvenio` sempre 1 parcela (parcelamento de convênio é regra da operadora, fora desta plataforma). Falha de envio ao convênio NÃO desfaz a fatura já persistida (fatura fica sem `ProtocoloConvenio`, reenvio manual/job futuro). `Fatura.ComissaoDentistaPercentual` + `CalcularValorComissao()` — campo opcional em QUALQUER fatura (particular ou convênio), não exclusivo de convênio. |
| 7. Teste | Broker (auto-revisão) | 4 cenários testados: convênio inexistente, convênio inativo, envio com sucesso (protocolo registrado, 2 SaveChanges — persiste depois registra protocolo), envio com falha (fatura persiste sem protocolo, 1 SaveChanges). Cálculo de comissão testado com percentual e sem percentual (zero). |
| 8. Documentação | Broker | Registrado em docs/knowledge e docs/decisions.md. |

## Status

done — coberto pelos mesmos 26/26 testes e build da task 006 (mesmo módulo). Dívidas técnicas
conhecidas:
- `ManualConvenioAdapter` é só referência — nenhuma operadora real (Amil Dental, Odontoprev etc)
  integrada. Cada convênio real precisa do próprio `IConvenioAdapter`, registrado no DI no lugar
  do manual (ports-and-adapters já preparado pra essa troca).
- Sem reenvio automático/job pra fatura que falhou o envio ao convênio (`ProtocoloConvenio`
  null) — hoje é reconciliação manual.
- Sem par Dev Backend/QA dedicado (mesma situação das tasks 005-006).

## Notas

Depende de 006 (faturamento particular já sólido) — na prática as duas tasks foram implementadas
juntas, no mesmo módulo `Billing`, porque compartilham 100% da estrutura de agregado
(`Fatura`/`Parcela`) e não fazia sentido separar em dois módulos.
