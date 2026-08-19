using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

public sealed record CreateUserResultDto(Guid Id, string Nome, string Email, Role Role, Guid? BranchId);
