using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListProfissionais;

public sealed class ListProfissionaisQueryHandler : IRequestHandler<ListProfissionaisQuery, Result<IReadOnlyList<ProfissionalDto>>>
{
    private readonly IProfissionalRepository _profissionalRepository;

    public ListProfissionaisQueryHandler(IProfissionalRepository profissionalRepository) => _profissionalRepository = profissionalRepository;

    public async Task<Result<IReadOnlyList<ProfissionalDto>>> Handle(ListProfissionaisQuery request, CancellationToken cancellationToken)
    {
        var profissionais = await _profissionalRepository.ListAsync(request.IncludeInactive, cancellationToken);
        return Result.Success<IReadOnlyList<ProfissionalDto>>(profissionais.Select(p => p.ToDto()).ToList());
    }
}
