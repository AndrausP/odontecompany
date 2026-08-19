using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Billing.Domain.Enums;
using SharedKernel;

namespace Billing.Application.Commands.CreateFaturaFromConsultaConcluida;

/// <summary>
/// Idempotente por design: mensageria (MassTransit/RabbitMQ) pode reentregar a mesma mensagem
/// mais de uma vez (at-least-once delivery é a garantia padrão de fila) — reprocessar o mesmo
/// <see cref="CreateFaturaFromConsultaConcluidaCommand.AgendamentoId"/> nunca cria fatura
/// duplicada: se já existe, devolve sucesso com o Id já existente em vez de erro ou duplicata.
/// À vista (1 parcela), forma de pagamento default Pix — regra simples pra faturamento
/// automático; ajuste de forma de pagamento fica pro fluxo manual (CreateFaturaParticular).
/// </summary>
public sealed class CreateFaturaFromConsultaConcluidaCommandHandler
    : MediatR.IRequestHandler<CreateFaturaFromConsultaConcluidaCommand, Result<Guid>>
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFaturaFromConsultaConcluidaCommandHandler(IFaturaRepository faturaRepository, IUnitOfWork unitOfWork)
    {
        _faturaRepository = faturaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateFaturaFromConsultaConcluidaCommand request, CancellationToken cancellationToken)
    {
        if (await _faturaRepository.ExistsByAgendamentoIdAsync(request.AgendamentoId, cancellationToken))
        {
            // Idempotência: evento já processado antes (reentrega de fila). Não é erro.
            return Result.Success(request.AgendamentoId);
        }

        var faturaResult = Fatura.CreateParticular(
            request.OrganizationId, request.PacienteId, request.AgendamentoId, request.ProfissionalId,
            request.Valor, numeroParcelas: 1, FormaPagamento.Pix, comissaoDentistaPercentual: null);

        if (faturaResult.IsFailure)
            return Result.Failure<Guid>(faturaResult.Error);

        var fatura = faturaResult.Value;

        await _faturaRepository.AddAsync(fatura, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // Corrida entre o check acima e o commit — outra entrega concorrente da mesma
            // mensagem já commitou primeiro. Ainda assim não é erro: idempotência mantida.
            return Result.Success(request.AgendamentoId);
        }

        return Result.Success(fatura.Id);
    }
}
