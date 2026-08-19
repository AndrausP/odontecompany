using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Queries.GetMyInvites;

/// <summary>Convites pendentes pro email do usuário logado, em QUALQUER organization (task 016, item 4). Funciona sem organization ativa.</summary>
public sealed record GetMyInvitesQuery(Guid UserId) : IRequest<Result<List<PendingInviteDto>>>;
