using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

/// <summary>Uma organization a que o usuário pertence, com o papel DESSA afiliação — task 017.</summary>
public sealed record OrganizationMembershipDto(Guid OrganizationId, string OrganizationName, Role Role, MembershipStatus Status, Guid? BranchId);
