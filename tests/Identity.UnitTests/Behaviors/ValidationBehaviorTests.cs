using Application.Common.Behaviors;
using FluentValidation;
using Identity.Application.Commands.Login;
using MediatR;

namespace Identity.UnitTests.Behaviors;

[TestFixture]
public class ValidationBehaviorTests
{
    [Test]
    public async Task Should_ReturnFailureResult_When_ValidationFails_InsteadOfThrowing()
    {
        var validators = new List<IValidator<LoginCommand>> { new LoginCommandValidator() };
        var behavior = new ValidationBehavior<LoginCommand, SharedKernel.Result<Identity.Application.DTOs.LoginResultDto>>(validators);

        var nextCalled = false;
        RequestHandlerDelegate<SharedKernel.Result<Identity.Application.DTOs.LoginResultDto>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(SharedKernel.Result.Success(new Identity.Application.DTOs.LoginResultDto("a", "b", 900)));
        };

        var result = await behavior.Handle(new LoginCommand("", ""), next, CancellationToken.None);

        Assert.That(nextCalled, Is.False, "handler não deveria ser chamado quando a validação falha");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Validation.Failure"));
    }

    [Test]
    public async Task Should_CallNext_When_ValidationPasses()
    {
        var validators = new List<IValidator<LoginCommand>> { new LoginCommandValidator() };
        var behavior = new ValidationBehavior<LoginCommand, SharedKernel.Result<Identity.Application.DTOs.LoginResultDto>>(validators);

        var expected = SharedKernel.Result.Success(new Identity.Application.DTOs.LoginResultDto("token", "refresh", 900));
        RequestHandlerDelegate<SharedKernel.Result<Identity.Application.DTOs.LoginResultDto>> next = () => Task.FromResult(expected);

        var result = await behavior.Handle(new LoginCommand("valido@clinica.com", "senha"), next, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.AccessToken, Is.EqualTo("token"));
    }
}
