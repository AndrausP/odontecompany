using MediatR;
using Records.Contracts;
using SharedKernel;

namespace Records.Application.Commands.CreateProntuario;

/// <summary>OrganizationId e CriadoPorUserId nunca vêm do corpo da requisição — montados pelo controller a partir do JWT.</summary>
public sealed record CreateProntuarioCommand(
    Guid OrganizationId,
    Guid PacienteId,
    Guid CriadoPorUserId
) : IRequest<Result<ProntuarioDto>>;
