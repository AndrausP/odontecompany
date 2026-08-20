using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListProcedimentos;

public sealed class ListProcedimentosQueryHandler : IRequestHandler<ListProcedimentosQuery, Result<IReadOnlyList<ProcedimentoDto>>>
{
    private readonly IProcedimentoRepository _procedimentoRepository;

    public ListProcedimentosQueryHandler(IProcedimentoRepository procedimentoRepository) => _procedimentoRepository = procedimentoRepository;

    public async Task<Result<IReadOnlyList<ProcedimentoDto>>> Handle(ListProcedimentosQuery request, CancellationToken cancellationToken)
    {
        var procedimentos = await _procedimentoRepository.ListAsync(request.IncludeInactive, cancellationToken);
        return Result.Success<IReadOnlyList<ProcedimentoDto>>(procedimentos.Select(p => p.ToDto()).ToList());
    }
}
