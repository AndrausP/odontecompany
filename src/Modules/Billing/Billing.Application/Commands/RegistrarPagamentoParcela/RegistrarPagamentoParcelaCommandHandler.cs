using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Contracts;
using Billing.Domain.Errors;
using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.RegistrarPagamentoParcela;

public sealed class RegistrarPagamentoParcelaCommandHandler : IRequestHandler<RegistrarPagamentoParcelaCommand, Result<FaturaDto>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarPagamentoParcelaCommandHandler(IFaturaRepository faturaRepository, IUnitOfWork unitOfWork)
    {
        _faturaRepository = faturaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FaturaDto>> Handle(RegistrarPagamentoParcelaCommand request, CancellationToken cancellationToken)
    {
        var fatura = await _faturaRepository.GetByIdAsync(request.FaturaId, cancellationToken);
        if (fatura is null)
            return Result.Failure<FaturaDto>(DomainErrors.Fatura.NaoEncontrada);

        var result = fatura.RegistrarPagamentoParcela(request.ParcelaId);
        if (result.IsFailure)
            return Result.Failure<FaturaDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(fatura.ToDto());
    }
}
