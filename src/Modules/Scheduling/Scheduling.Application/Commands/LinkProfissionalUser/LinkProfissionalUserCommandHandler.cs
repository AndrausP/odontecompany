using MediatR;
using Scheduling.Application.Interfaces;
using SharedKernel;

namespace Scheduling.Application.Commands.LinkProfissionalUser;

public sealed class LinkProfissionalUserCommandHandler : IRequestHandler<LinkProfissionalUserCommand, Result>
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LinkProfissionalUserCommandHandler(IProfissionalRepository profissionalRepository, IUnitOfWork unitOfWork)
    {
        _profissionalRepository = profissionalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LinkProfissionalUserCommand request, CancellationToken cancellationToken)
    {
        var pendentes = await _profissionalRepository.ListPendentesPorEmailAcrossOrganizationsAsync(request.OrganizationId, request.Email, cancellationToken);
        if (pendentes.Count == 0)
            return Result.Success(); // nenhum profissional esperando este email — não é erro, a maioria dos convites não é de dentista

        foreach (var profissional in pendentes)
            profissional.VincularUsuario(request.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
