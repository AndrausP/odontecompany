using MediatR;
using Records.Contracts;
using Records.Domain.Enums;
using SharedKernel;

namespace Records.Application.Commands.AddEvolucaoClinica;

public sealed record AddEvolucaoClinicaCommand(
    Guid OrganizationId,
    Guid ProntuarioId,
    Guid ProfissionalUserId,
    TipoProcedimento TipoProcedimento,
    string DescricaoClinica
) : IRequest<Result<EvolucaoClinicaDto>>;
