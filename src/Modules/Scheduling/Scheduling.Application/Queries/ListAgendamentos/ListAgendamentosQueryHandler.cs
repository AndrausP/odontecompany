using Contracts.Abstractions.Pagination;
using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Enums;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Queries.ListAgendamentos;

public sealed class ListAgendamentosQueryHandler : IRequestHandler<ListAgendamentosQuery, Result<PagedResult<AgendamentoDto>>>
{
    private readonly IAgendamentoRepository _agendamentoRepository;

    public ListAgendamentosQueryHandler(IAgendamentoRepository agendamentoRepository) => _agendamentoRepository = agendamentoRepository;

    public async Task<Result<PagedResult<AgendamentoDto>>> Handle(ListAgendamentosQuery request, CancellationToken cancellationToken)
    {
        AgendamentoStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<AgendamentoStatus>(request.Status, ignoreCase: true, out var parsedStatus))
                return Result.Failure<PagedResult<AgendamentoDto>>(DomainErrors.Agendamento.StatusInvalido);

            status = parsedStatus;
        }

        var (items, totalCount) = await _agendamentoRepository.ListAsync(
            request.ProfissionalId,
            request.PacienteId,
            status,
            request.DataInicio,
            request.DataFim,
            request.BranchId,
            request.Pagination.Page,
            request.Pagination.PageSize,
            cancellationToken);

        var dtoItems = items.Select(a => a.ToDto()).ToList();

        var pagedResult = PagedResult<AgendamentoDto>.Create(dtoItems, totalCount, request.Pagination.Page, request.Pagination.PageSize);

        return Result.Success(pagedResult);
    }
}
