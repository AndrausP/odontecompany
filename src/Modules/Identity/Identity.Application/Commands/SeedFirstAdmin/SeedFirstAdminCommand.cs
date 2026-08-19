using Identity.Application.DTOs;
using MediatR;
using SharedKernel;

namespace Identity.Application.Commands.SeedFirstAdmin;

public sealed record SeedFirstAdminCommand(string OrganizationNome, string AdminNome, string AdminEmail, string AdminPassword)
    : IRequest<Result<SeedResultDto>>;
