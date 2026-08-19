using MediatR;
using Records.Contracts;
using SharedKernel;

namespace Records.Application.Queries.GetProntuarioByPacienteId;

/// <summary>RequestingUserId é obrigatório — toda leitura de prontuário é registrada na trilha de auditoria (LGPD).</summary>
public sealed record GetProntuarioByPacienteIdQuery(Guid PacienteId, Guid RequestingUserId) : IRequest<Result<ProntuarioDto>>;
