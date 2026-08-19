namespace Reporting.Contracts;

/// <summary>
/// KPIs consolidados de um período — composição em tempo real dos summary providers de
/// Patients/Scheduling/Billing (ver docs/knowledge/patterns.md, "projeção CQRS por composição
/// direta"). Não é um snapshot persistido — cada chamada reflete o estado atual do banco.
/// </summary>
public sealed record DashboardResumoDto(
    Guid OrganizationId,
    DateTime DataInicio,
    DateTime DataFim,
    int PacientesAtivos,
    int TotalAgendamentos,
    int AgendamentosConcluidos,
    int AgendamentosCancelados,
    decimal ValorTotalFaturado,
    decimal ValorTotalRecebido,
    decimal ValorTotalPendente,
    // Concluídos / Total agendado no período (0-100). NÃO é ocupação de agenda contra
    // capacidade teórica — não há conceito de "horário disponível total" implementado ainda
    // (precisaria de configuração de expediente por clínica, fora de escopo desta task).
    decimal TaxaConclusao);
