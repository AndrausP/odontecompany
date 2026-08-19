using MediatR;
using Reporting.Contracts;
using SharedKernel;

namespace Reporting.Application.Queries.GetComissoesPorPeriodo;

/// <summary>
/// Uma query só serve "visão colaborador" e "visão empresa" (D3, docs/tasks/023-*.md) — quem
/// preenche <see cref="ProfissionalId"/>/<see cref="BranchId"/>/<see cref="Classe"/> é sempre o
/// controller (D5), nunca o cliente direto: um Dentista tem <see cref="ProfissionalId"/> forçado
/// pro próprio e os outros dois filtros descartados antes deste request existir.
/// </summary>
public sealed record GetComissoesPorPeriodoQuery(
    Guid OrganizationId,
    DateTime DataInicio,
    DateTime DataFim,
    Guid? BranchId,
    string? Classe,
    Guid? ProfissionalId) : IRequest<Result<ComissaoResumoDto>>;
