using MediatR;
using SharedKernel;

namespace Scheduling.Application.Commands.LinkProfissionalUser;

/// <summary>
/// Vincula o usuário recém-autenticado a qualquer profissional pendente (sem <c>UserId</c>) com
/// este email na organização — chamado pelo controller (Bootstrap) logo após aceitar um convite
/// (task 042). Sempre sucesso, mesmo sem nenhum profissional pendente pra vincular: é um passo
/// best-effort, não um requisito do aceite do convite em si.
/// </summary>
public sealed record LinkProfissionalUserCommand(Guid OrganizationId, string Email, Guid UserId) : IRequest<Result>;
