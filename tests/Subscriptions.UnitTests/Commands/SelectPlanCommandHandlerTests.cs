using Moq;
using Subscriptions.Application.Commands.SelectPlan;
using Subscriptions.Application.Interfaces;
using Subscriptions.Domain.Entities;
using Subscriptions.Domain.Enums;
using Tenancy.Contracts;

namespace Subscriptions.UnitTests.Commands;

/// <summary>Auditoria pré-venda — downgrade de plano passou a validar limite de filial (dívida técnica nomeada na task 039).</summary>
[TestFixture]
public class SelectPlanCommandHandlerTests
{
    private Mock<ISubscriptionRepository> _subscriptionRepository = null!;
    private Mock<IBranchLookup> _branchLookup = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private SelectPlanCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _subscriptionRepository = new Mock<ISubscriptionRepository>();
        _branchLookup = new Mock<IBranchLookup>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new SelectPlanCommandHandler(_subscriptionRepository.Object, _branchLookup.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Should_CreateSubscription_When_OrganizationHasNoPlanYet()
    {
        var organizationId = Guid.NewGuid();
        _subscriptionRepository.Setup(r => r.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var result = await _handler.Handle(new SelectPlanCommand(organizationId, PlanTier.Starter), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Tier, Is.EqualTo(nameof(PlanTier.Starter)));
        // Primeira escolha (organização recém-criada) não confere filial — não tem nenhuma ainda.
        _branchLookup.Verify(b => b.CountAtivasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ChangeTier_When_NewLimiteFiliaisCoversActiveBranches()
    {
        var organizationId = Guid.NewGuid();
        var existing = Subscription.Create(organizationId, PlanTier.Starter).Value;
        _subscriptionRepository.Setup(r => r.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _branchLookup.Setup(b => b.CountAtivasAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        // Upgrade Starter(1)→Profissional(3): 2 filiais ativas cabem no limite novo.
        var result = await _handler.Handle(new SelectPlanCommand(organizationId, PlanTier.Profissional), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(existing.Tier, Is.EqualTo(PlanTier.Profissional));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_ReturnDowngradeExcedeFiliaisAtivas_When_ActiveBranchCountExceedsNewTierLimit()
    {
        var organizationId = Guid.NewGuid();
        var existing = Subscription.Create(organizationId, PlanTier.Profissional).Value;
        _subscriptionRepository.Setup(r => r.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        // 3 filiais ativas, Starter só permite 1 — downgrade tem que bloquear.
        _branchLookup.Setup(b => b.CountAtivasAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await _handler.Handle(new SelectPlanCommand(organizationId, PlanTier.Starter), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Subscription.DowngradeExcedeFiliaisAtivas"));
        Assert.That(existing.Tier, Is.EqualTo(PlanTier.Profissional), "não deve trocar o tier quando bloqueado");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
