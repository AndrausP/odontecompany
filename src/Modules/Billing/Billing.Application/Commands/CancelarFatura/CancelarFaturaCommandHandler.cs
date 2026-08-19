using Billing.Application.Interfaces;
using Billing.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.CancelarFatura;

public sealed class CancelarFaturaCommandHandler : IRequestHandler<CancelarFaturaCommand, Result>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarFaturaCommandHandler(IFaturaRepository faturaRepository, IUnitOfWork unitOfWork)
    {
        _faturaRepository = faturaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelarFaturaCommand request, CancellationToken cancellationToken)
    {
        var fatura = await _faturaRepository.GetByIdAsync(request.FaturaId, cancellationToken);
        if (fatura is null)
            return Result.Failure(DomainErrors.Fatura.NaoEncontrada);

        var result = fatura.Cancelar();
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
