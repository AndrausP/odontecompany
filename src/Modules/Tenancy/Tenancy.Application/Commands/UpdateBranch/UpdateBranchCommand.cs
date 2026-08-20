using MediatR;
using SharedKernel;
using Tenancy.Contracts;

namespace Tenancy.Application.Commands.UpdateBranch;

/// <summary>Tela de configurações — edita os campos que a criação já aceitava (Endereco/Telefone).
/// <paramref name="OrganizationId"/> vem do token (nunca da rota) — defesa contra IDOR: sem isso,
/// um Owner/Admin de uma organization conseguia editar branch de outra organization só sabendo o
/// guid (achado registrado em docs/decisions.md, task 040).</summary>
public sealed record UpdateBranchCommand(Guid BranchId, Guid OrganizationId, string Nome, string? Endereco, string? Telefone)
    : IRequest<Result<BranchDto>>;
