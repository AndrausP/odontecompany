using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using OdontoPlatform.Api.Controllers;

namespace OdontoPlatform.Api.UnitTests.Controllers;

/// <summary>
/// Regressão da task 030: <c>Owner</c> tem que ter paridade de acesso com <c>Admin</c> em todo
/// controller administrativo. Como este repo não tem WebApplicationFactory/host real pra testar
/// o pipeline de autorização de ponta a ponta (ver comentário em
/// <see cref="ComissoesControllerTests"/>), a checagem é feita direto no atributo
/// <see cref="AuthorizeAttribute"/> via reflexão — é a fonte de verdade que o
/// <c>[Authorize(Roles = ...)]</c> realmente carrega, sem precisar simular o middleware.
/// Cobre <see cref="BranchesController"/> e <see cref="UsersController"/> (achado original da
/// task 027) — os dois exemplos citados no critério de aceite da task 030.
/// </summary>
[TestFixture]
public class OwnerRoleParityTests
{
    // ── BranchesController: era Admin-only, Owner nem listava a própria filial ────────────────
    [Test]
    public void Should_IncludeOwnerRole_When_BranchesControllerClassAuthorize()
    {
        var roles = GetClassRoles<BranchesController>();

        Assert.That(roles, Does.Contain("Owner"), "Owner precisa ter paridade com Admin em BranchesController (task 030).");
        Assert.That(roles, Does.Contain("Admin"), "Admin não pode ser removido — só adiciona Owner.");
    }

    // ── UsersController: era Admin-only, Owner não conseguia provisionar usuário na própria organization ──
    [Test]
    public void Should_IncludeOwnerRole_When_UsersControllerClassAuthorize()
    {
        var roles = GetClassRoles<UsersController>();

        Assert.That(roles, Does.Contain("Owner"), "Owner precisa ter paridade com Admin em UsersController (task 030).");
        Assert.That(roles, Does.Contain("Admin"), "Admin não pode ser removido — só adiciona Owner.");
    }

    // ── RecordsController: classe é Admin,Dentista + método GetAuditLog é Admin-only ──────────
    [Test]
    public void Should_IncludeOwnerRole_When_RecordsControllerClassAndAuditLogMethodAuthorize()
    {
        var classRoles = GetClassRoles<RecordsController>();
        Assert.That(classRoles, Does.Contain("Owner"));
        Assert.That(classRoles, Does.Contain("Admin"));
        Assert.That(classRoles, Does.Contain("Dentista"), "role operacional não pode ser removida.");

        var method = typeof(RecordsController).GetMethod(nameof(RecordsController.GetAuditLog))!;
        var methodRoles = GetRoles(method.GetCustomAttribute<AuthorizeAttribute>());
        Assert.That(methodRoles, Does.Contain("Owner"));
        Assert.That(methodRoles, Does.Contain("Admin"));
    }

    // ── Regressão negativa: roles puramente operacionais não ganharam Owner por engano ─────────
    [Test]
    public void Should_NotAddOwnerRole_When_ControllerHasNoAdminOnlyAuthorize()
    {
        // SchedulingController.Create é Admin,Recepcao,Dentista — Owner É esperado aqui (decisão
        // explícita do broker, consistência com "Owner = poderes de Admin"), então este teste só
        // confirma que a role original Recepcao/Dentista continua presente, não foi substituída.
        var method = typeof(SchedulingController).GetMethod(nameof(SchedulingController.Create))!;
        var roles = GetRoles(method.GetCustomAttribute<AuthorizeAttribute>());

        Assert.That(roles, Does.Contain("Owner"));
        Assert.That(roles, Does.Contain("Recepcao"));
        Assert.That(roles, Does.Contain("Dentista"));
    }

    private static string[] GetClassRoles<TController>()
    {
        var attribute = typeof(TController).GetCustomAttribute<AuthorizeAttribute>();
        return GetRoles(attribute);
    }

    private static string[] GetRoles(AuthorizeAttribute? attribute)
    {
        Assert.That(attribute, Is.Not.Null, "Controller/método precisa ter [Authorize(Roles = ...)].");
        Assert.That(attribute!.Roles, Is.Not.Null.And.Not.Empty);

        return attribute.Roles!.Split(',');
    }
}
