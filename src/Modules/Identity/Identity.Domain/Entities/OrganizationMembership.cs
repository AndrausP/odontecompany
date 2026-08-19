using Identity.Domain.Enums;
using Identity.Domain.Errors;
using SharedKernel;

namespace Identity.Domain.Entities;

/// <summary>
/// Afiliação de um <see cref="User"/> (entidade GLOBAL, task 013) a uma <see cref="Organization"/>,
/// com um papel (<see cref="Role"/>) próprio DESSA afiliação — o mesmo usuário pode ter papéis
/// diferentes em organizations diferentes. Único par (OrganizationId, UserId) — ver índice único
/// em <c>OrganizationMembershipConfiguration</c>. É esta entidade, não mais <see cref="User"/>, que
/// carrega <see cref="IMustHaveOrganization"/> e cai no filtro global de organization.
/// </summary>
public class OrganizationMembership : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public Role Role { get; private set; }
    public MembershipStatus Status { get; private set; }

    /// <summary>
    /// Branch (clínica física) a que esta afiliação está restrita — escopo de filial é por
    /// afiliação, não por pessoa (um usuário pode ser irrestrito numa org e restrito noutra).
    /// Null = enxerga todas as branches da organization.
    /// </summary>
    public Guid? BranchId { get; private set; }

    public bool IsAtivo => Status == MembershipStatus.Ativo;

    private OrganizationMembership() { } // EF Core

    private OrganizationMembership(Guid organizationId, Guid userId, Role role, Guid? branchId)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Role = role;
        Status = MembershipStatus.Ativo;
        BranchId = branchId;
    }

    public static Result<OrganizationMembership> Create(Guid organizationId, Guid userId, Role role, Guid? branchId = null)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<OrganizationMembership>(DomainErrors.Membership.OrganizationInvalido);

        if (userId == Guid.Empty)
            return Result.Failure<OrganizationMembership>(DomainErrors.Membership.UserInvalido);

        return Result.Success(new OrganizationMembership(organizationId, userId, role, branchId));
    }

    public void Desativar()
    {
        Status = MembershipStatus.Inativo;
        SetUpdatedAt();
    }
}
