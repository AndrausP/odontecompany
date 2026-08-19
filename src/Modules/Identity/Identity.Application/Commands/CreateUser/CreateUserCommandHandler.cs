using Identity.Application.DTOs;
using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Errors;
using MediatR;
using SharedKernel;
using Tenancy.Contracts;

namespace Identity.Application.Commands.CreateUser;

/// <summary>
/// Se <see cref="CreateUserCommand.BranchId"/> for informado, valida via
/// <c>Tenancy.Contracts.IBranchLookup</c> (nunca Tenancy.Domain/Infrastructure) — mesma
/// fronteira cross-module já usada em Scheduling com Patients.Contracts.IPatientLookup.
/// </summary>
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IBranchLookup _branchLookup;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IOrganizationMembershipRepository membershipRepository,
        IPasswordHasher passwordHasher,
        IBranchLookup branchLookup,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _membershipRepository = membershipRepository;
        _passwordHasher = passwordHasher;
        _branchLookup = branchLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateUserResultDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
            return Result.Failure<CreateUserResultDto>(DomainErrors.User.EmailJaCadastrado);

        if (request.BranchId is not null && !await _branchLookup.ExistsAsync(request.OrganizationId, request.BranchId.Value, cancellationToken))
            return Result.Failure<CreateUserResultDto>(DomainErrors.User.BranchInvalida);

        var passwordHash = _passwordHasher.Hash(request.Password);

        var userResult = User.Create(request.Nome, email, passwordHash);
        if (userResult.IsFailure)
            return Result.Failure<CreateUserResultDto>(userResult.Error);

        var user = userResult.Value;

        // User + Membership na org do chamador, no mesmo SaveChangesAsync — atomicidade vem do
        // DbContext ser a mesma unit-of-work pros dois Add (mesmo padrão do SeedFirstAdminCommandHandler).
        var membershipResult = OrganizationMembership.Create(request.OrganizationId, user.Id, request.Role, request.BranchId);
        if (membershipResult.IsFailure)
            return Result.Failure<CreateUserResultDto>(membershipResult.Error);

        var membership = membershipResult.Value;

        await _userRepository.AddAsync(user, cancellationToken);
        await _membershipRepository.AddAsync(membership, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // TOCTOU: duas requisições concorrentes com o mesmo email passaram pelo
            // EmailExistsAsync antes de qualquer uma commitar. O unique index do banco é quem
            // garante a integridade de verdade — aqui só traduzimos pra Result de negócio em
            // vez de deixar a exception (500 cru) subir pro controller.
            return Result.Failure<CreateUserResultDto>(DomainErrors.User.EmailJaCadastrado);
        }

        return Result.Success(new CreateUserResultDto(user.Id, user.Nome, user.Email, membership.Role, membership.BranchId));
    }
}
