namespace Identity.Application.DTOs;

public sealed record SeedResultDto(Guid OrganizationId, string OrganizationNome, Guid AdminUserId, string AdminEmail);
