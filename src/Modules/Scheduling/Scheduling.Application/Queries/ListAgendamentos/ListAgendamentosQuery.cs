using Contracts.Abstractions.Pagination;
using MediatR;
using Scheduling.Contracts;
using SharedKernel;

namespace Scheduling.Application.Queries.ListAgendamentos;

/// <summary>
/// Listagem paginada, escopada por organization via filtro global do EF Core. Status vem como string
/// (formato de query string) e é convertido pro enum de domínio dentro do handler — mesma razão
/// de AgendamentoDto.Status ser string: a Application é quem conhece o enum, não a fronteira pública.
/// </summary>
/// <summary>
/// <see cref="BranchId"/> (Fase 5) é decidido pelo CONTROLLER a partir de
/// <c>ICurrentUserAccessor</c> — Recepcao sempre tem o próprio <c>BranchId</c> forçado aqui,
/// nunca escolhido livremente via query string (senão bastaria omitir o filtro pra ver a rede
/// inteira). Admin passa null e vê todas as branches.
/// </summary>
public sealed record ListAgendamentosQuery(
    PageRequest Pagination,
    Guid? ProfissionalId = null,
    Guid? PacienteId = null,
    string? Status = null,
    DateTime? DataInicio = null,
    DateTime? DataFim = null,
    Guid? BranchId = null
) : IRequest<Result<PagedResult<AgendamentoDto>>>;
