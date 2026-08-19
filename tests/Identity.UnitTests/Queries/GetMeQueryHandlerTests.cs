using Identity.Application.Interfaces;
using Identity.Application.Queries.GetMe;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;

namespace Identity.UnitTests.Queries;

/// <summary>Task 017 — GET /api/me. Organizações a que o usuário NÃO pertence nunca aparecem.</summary>
[TestFixture]
public class GetMeQueryHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IOrganizationMembershipRepository> _membershipRepository;
    private Mock<IOrganizationRepository> _organizationRepository;
    private Mock<IInviteRepository> _inviteRepository;
    private GetMeQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _membershipRepository = new Mock<IOrganizationMembershipRepository>();
        _organizationRepository = new Mock<IOrganizationRepository>();
        _inviteRepository = new Mock<IInviteRepository>();

        _handler = new GetMeQueryHandler(_userRepository.Object, _membershipRepository.Object, _organizationRepository.Object, _inviteRepository.Object);

        _inviteRepository.Setup(r => r.GetPendingByEmailAcrossOrganizationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        // Default: nenhuma organization resolvida, salvo quando o teste sobrescreve explicitamente
        // (handler chama GetByIdsAsync mesmo com lista de ids vazia, quando não há memberships).
        _organizationRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    [Test]
    public async Task Should_ReturnOnlyOrganizationsFromUserMemberships_When_UserBelongsToMultipleOrganizations()
    {
        var user = User.Create("Dra. Ana", "ana@clinica.com", "hash").Value;
        var orgMine = Organization.Create("Minha Clínica").Value;
        var membership = OrganizationMembership.Create(orgMine.Id, user.Id, Role.Owner).Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([membership]);
        _organizationRepository.Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(orgMine.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([orgMine]);

        var result = await _handler.Handle(new GetMeQuery(user.Id, orgMine.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Organizations, Has.Count.EqualTo(1));
        Assert.That(result.Value.Organizations[0].OrganizationId, Is.EqualTo(orgMine.Id));
        Assert.That(result.Value.Organizations[0].Role, Is.EqualTo(Role.Owner));
        Assert.That(result.Value.ActiveOrganizationId, Is.EqualTo(orgMine.Id));

        // A busca de organizations é SEMPRE derivada das memberships do PRÓPRIO usuário — nunca uma listagem geral.
        _membershipRepository.Verify(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnNullActiveOrganizationId_When_TokenHasNoOrganization()
    {
        var user = User.Create("Sem Org", "semorg@clinica.com", "hash").Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetMeQuery(user.Id, ActiveOrganizationId: null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.ActiveOrganizationId, Is.Null);
        Assert.That(result.Value.Organizations, Is.Empty);
    }

    [Test]
    public async Task Should_EmbedPendingInvites_When_UserHasPendingInvites()
    {
        var user = User.Create("Convidado", "convidado@clinica.com", "hash").Value;
        var orgConvite = Organization.Create("Clínica Convite").Value;
        var invite = Invite.Create(orgConvite.Id, user.Email, Role.Dentista, "hash-token", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _membershipRepository.Setup(r => r.GetActiveMembershipsForUserAcrossOrganizationsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _inviteRepository.Setup(r => r.GetPendingByEmailAcrossOrganizationsAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync([invite]);
        _organizationRepository.Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(orgConvite.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([orgConvite]);

        var result = await _handler.Handle(new GetMeQuery(user.Id, null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.PendingInvites, Has.Count.EqualTo(1));
        Assert.That(result.Value.PendingInvites[0].OrganizationName, Is.EqualTo("Clínica Convite"));
    }

    [Test]
    public async Task Should_ReturnUsuarioInativo_When_UserNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _handler.Handle(new GetMeQuery(Guid.NewGuid(), null), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("User.UsuarioInativo"));
    }
}
