using Billing.Contracts;
using MediatR;
using Patients.Contracts;
using Reporting.Contracts;
using Scheduling.Contracts;
using SharedKernel;

namespace Reporting.Application.Queries.GetDashboardResumo;

/// <summary>
/// Composição EM TEMPO REAL dos três summary providers (Patients/Scheduling/Billing) — não é
/// uma tabela de projeção materializada. Ver docs/knowledge/patterns.md ("projeção CQRS por
/// composição direta") pro porquê: sem worker de Outbox/bus de eventos rodando ainda, manter
/// projeções persistidas sincronizadas exigiria um mecanismo de refresh que não existe. Isso é
/// aceitável pro volume esperado do MVP (poucas dezenas de milhares de linhas por organization); se o
/// volume crescer, trocar por projeção persistida sincronizada por evento é a evolução natural
/// — a composição dos três providers já dá a base pro que a projeção precisaria calcular.
/// </summary>
public sealed class GetDashboardResumoQueryHandler : IRequestHandler<GetDashboardResumoQuery, Result<DashboardResumoDto>>
{
    private readonly IPatientSummaryProvider _patientSummaryProvider;
    private readonly IAgendaSummaryProvider _agendaSummaryProvider;
    private readonly IFaturamentoSummaryProvider _faturamentoSummaryProvider;

    public GetDashboardResumoQueryHandler(
        IPatientSummaryProvider patientSummaryProvider,
        IAgendaSummaryProvider agendaSummaryProvider,
        IFaturamentoSummaryProvider faturamentoSummaryProvider)
    {
        _patientSummaryProvider = patientSummaryProvider;
        _agendaSummaryProvider = agendaSummaryProvider;
        _faturamentoSummaryProvider = faturamentoSummaryProvider;
    }

    public async Task<Result<DashboardResumoDto>> Handle(GetDashboardResumoQuery request, CancellationToken cancellationToken)
    {
        // As 3 chamadas são independentes entre si (módulos diferentes, sem dependência de
        // dado uma da outra) — rodam em paralelo pra não somar latência sequencial.
        var pacientesAtivosTask = _patientSummaryProvider.ContarAtivosAsync(request.OrganizationId, cancellationToken);
        var agendaResumoTask = _agendaSummaryProvider.ObterResumoAsync(request.OrganizationId, request.DataInicio, request.DataFim, cancellationToken);
        var faturamentoResumoTask = _faturamentoSummaryProvider.ObterResumoAsync(request.OrganizationId, request.DataInicio, request.DataFim, cancellationToken);

        await Task.WhenAll(pacientesAtivosTask, agendaResumoTask, faturamentoResumoTask);

        var pacientesAtivos = await pacientesAtivosTask;
        var agendaResumo = await agendaResumoTask;
        var faturamentoResumo = await faturamentoResumoTask;

        var taxaConclusao = agendaResumo.TotalAgendamentos == 0
            ? 0m
            : Math.Round((decimal)agendaResumo.Concluidos / agendaResumo.TotalAgendamentos * 100, 2);

        var dto = new DashboardResumoDto(
            request.OrganizationId,
            request.DataInicio,
            request.DataFim,
            pacientesAtivos,
            agendaResumo.TotalAgendamentos,
            agendaResumo.Concluidos,
            agendaResumo.Cancelados,
            faturamentoResumo.ValorTotalFaturado,
            faturamentoResumo.ValorTotalRecebido,
            faturamentoResumo.ValorTotalPendente,
            taxaConclusao);

        return Result.Success(dto);
    }
}
