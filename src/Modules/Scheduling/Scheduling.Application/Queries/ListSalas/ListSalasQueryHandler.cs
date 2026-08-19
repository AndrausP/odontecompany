using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListSalas;

public sealed class ListSalasQueryHandler : IRequestHandler<ListSalasQuery, Result<IReadOnlyList<SalaDto>>>
{
    private readonly ISalaRepository _salaRepository;

    public ListSalasQueryHandler(ISalaRepository salaRepository) => _salaRepository = salaRepository;

    public async Task<Result<IReadOnlyList<SalaDto>>> Handle(ListSalasQuery request, CancellationToken cancellationToken)
    {
        var salas = await _salaRepository.ListAsync(request.IncludeInactive, cancellationToken);
        return Result.Success<IReadOnlyList<SalaDto>>(salas.Select(s => s.ToDto()).ToList());
    }
}
