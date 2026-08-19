using Tenancy.Contracts;
using Tenancy.Domain.Entities;

namespace Tenancy.Application.Mapping;

public static class BranchMappingExtensions
{
    public static BranchDto ToDto(this Branch branch)
        => new(branch.Id, branch.OrganizationId, branch.Nome, branch.Endereco, branch.Telefone, branch.Ativo);
}
