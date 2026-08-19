using MediatR;
using SharedKernel;
using Subscriptions.Application.Interfaces;

namespace Subscriptions.Application.Commands.StartCheckout;

public sealed class StartCheckoutCommandHandler : IRequestHandler<StartCheckoutCommand, Result<CheckoutSessionResult>>
{
    private readonly IPaymentGatewayService _paymentGateway;

    public StartCheckoutCommandHandler(IPaymentGatewayService paymentGateway)
    {
        _paymentGateway = paymentGateway;
    }

    public async Task<Result<CheckoutSessionResult>> Handle(StartCheckoutCommand request, CancellationToken cancellationToken)
    {
        var session = await _paymentGateway.CriarCheckoutSessionAsync(request.OrganizationId, request.Tier, cancellationToken);
        return Result.Success(session);
    }
}
