using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

/// <summary>
/// <see cref="User"/> é entidade GLOBAL desde a task 013 (não implementa mais
/// <see cref="SharedKernel.IMustHaveOrganization"/>) — não existe mais filtro de organization pra
/// contornar, então os métodos abaixo não precisam de variante "AcrossOrganizations".
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Checa unicidade GLOBAL de email (nunca se repete entre organizations).</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);
}
