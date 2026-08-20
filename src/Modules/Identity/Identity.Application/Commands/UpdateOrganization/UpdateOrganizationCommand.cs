using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.UpdateOrganization;

/// <summary>Tela de configurações — edita os campos opcionais que a criação já aceitava (Cnpj/
/// Telefone/Endereco), item anunciado como "futuro" na task 039.</summary>
public sealed record UpdateOrganizationCommand(Guid OrganizationId, string Nome, string? Cnpj, string? Telefone, string? Endereco)
    : IRequest<Result<OrganizationDto>>;
