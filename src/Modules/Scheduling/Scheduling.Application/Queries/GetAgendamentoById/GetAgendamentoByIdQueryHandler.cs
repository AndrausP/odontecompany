using MediatR;
using Scheduling.Application.Interfaces;
using Scheduling.Application.Mapping;
using Scheduling.Contracts;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Application.Queries.GetAgendamentoById;

public sealed class GetAgendamentoByIdQueryHandler : IRequestHandler<GetAgendamentoByIdQuery, Result<AgendamentoDto>>
{
    private readonly IAgendamentoRepository _agendamentoRepository;

    public GetAgendamentoByIdQueryHandler(IAgendamentoRepository agendamentoRepository) => _agendamentoRepository = agendamentoRepository;

    public async Task<Result<AgendamentoDto>> Handle(GetAgendamentoByIdQuery request, CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoRepository.GetByIdAsync(request.Id, cancellationToken);

        return agendamento is null
            ? Result.Failure<AgendamentoDto>(DomainErrors.Agendamento.NaoEncontrado)
            : Result.Success(agendamento.ToDto());
    }
}
