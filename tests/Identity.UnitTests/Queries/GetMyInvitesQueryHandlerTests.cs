using Identity.Application.Interfaces;
using Identity.Application.Queries.GetMyInvites;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;

namespace Identity.UnitTests.Queries;

/// <summary>Task 016/017 — isolamento: convites de organizations DIFERENTES aparecem juntos (cross-organization é o ponto do endpoint).</summary>
[TestFixture]
public class GetMyInvitesQueryHandlerTests
{
    private Mock<IUserRepository> _userRepository;
    private Mock<IInviteRepository> _inviteRepository;
    private Mock<IOrganizationRepository> _organizationRepository;
    private GetMyInvitesQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _userRepository = new Mock<IUserRepository>();
        _inviteRepository = new Mock<IInviteRepository>();
        _organizationRepository = new Mock<IOrganizationRepository>();

        _handler = new GetMyInvitesQueryHandler(_userRepository.Object, _inviteRepository.Object, _organizationRepository.Object);
    }

    [Test]
    public async Task Should_ReturnInvitesFromMultipleOrganizations_When_UserHasPendingInvitesAcrossOrgs()
    {
        var user = User.Create("Convidado", "convidado@clinica.com", "hash").Value;
        var orgA = Organization.Create("Clínica A").Value;
        var orgB = Organization.Create("Clínica B").Value;
        var inviteA = Invite.Create(orgA.Id, user.Email, Role.Dentista, "hash-a", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;
        var inviteB = Invite.Create(orgB.Id, user.Email, Role.Recepcao, "hash-b", Guid.NewGuid(), TimeSpan.FromDays(7)).Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _inviteRepository.Setup(r => r.GetPendingByEmailAcrossOrganizationsAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync([inviteA, inviteB]);
        _organizationRepository
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(orgA.Id) && ids.Contains(orgB.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([orgA, orgB]);

        var result = await _handler.Handle(new GetMyInvitesQuery(user.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Count, Is.EqualTo(2));
        Assert.That(result.Value.Select(d => d.OrganizationId), Is.EquivalentTo(new[] { orgA.Id, orgB.Id }));
        Assert.That(result.Value.First(d => d.OrganizationId == orgA.Id).OrganizationName, Is.EqualTo("Clínica A"));
        Assert.That(result.Value.First(d => d.OrganizationId == orgB.Id).OrganizationName, Is.EqualTo("Clínica B"));
    }

    [Test]
    public async Task Should_ReturnEmptyList_When_NoPendingInvites()
    {
        var user = User.Create("Sem Convite", "semconvite@clinica.com", "hash").Value;

        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _inviteRepository.Setup(r => r.GetPendingByEmailAcrossOrganizationsAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _handler.Handle(new GetMyInvitesQuery(user.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
        _organizationRepository.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
