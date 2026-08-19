using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Contracts;
using Billing.Domain.Entities;
using MediatR;
using SharedKernel;

namespace Billing.Application.Commands.CreateConvenio;

public sealed class CreateConvenioCommandHandler : IRequestHandler<CreateConvenioCommand, Result<ConvenioDto>>
{
    private readonly IConvenioRepository _convenioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateConvenioCommandHandler(IConvenioRepository convenioRepository, IUnitOfWork unitOfWork)
    {
        _convenioRepository = convenioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConvenioDto>> Handle(CreateConvenioCommand request, CancellationToken cancellationToken)
    {
        var convenioResult = Convenio.Create(request.OrganizationId, request.Nome, request.CodigoExterno);
        if (convenioResult.IsFailure)
            return Result.Failure<ConvenioDto>(convenioResult.Error);

        var convenio = convenioResult.Value;

        await _convenioRepository.AddAsync(convenio, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<ConvenioDto>(new Error("Convenio.NomeJaCadastrado", "Já existe um convênio com este nome neste organization."));
        }

        return Result.Success(convenio.ToDto());
    }
}
