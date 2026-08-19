using Billing.Domain.Enums;
using SharedKernel;

namespace Billing.Domain.Entities;

/// <summary>
/// Parcela de uma fatura — não é AggregateRoot, sempre criada/persistida através de
/// <c>Fatura</c> (o agregado que garante a invariante "soma das parcelas == valor total").
/// </summary>
public class Parcela : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid FaturaId { get; private set; }
    public int NumeroParcela { get; private set; }
    public decimal ValorParcela { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public DateTime? DataPagamento { get; private set; }
    public StatusParcela Status { get; private set; }

    private Parcela() { } // EF Core

    internal Parcela(Guid organizationId, Guid faturaId, int numeroParcela, decimal valorParcela, DateTime dataVencimento)
    {
        OrganizationId = organizationId;
        FaturaId = faturaId;
        NumeroParcela = numeroParcela;
        ValorParcela = valorParcela;
        DataVencimento = dataVencimento;
        Status = StatusParcela.Pendente;
    }

    public Result RegistrarPagamento()
    {
        if (Status == StatusParcela.Paga)
            return Result.Failure(Errors.DomainErrors.Parcela.JaPaga);

        Status = StatusParcela.Paga;
        DataPagamento = DateTime.UtcNow;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Chamado por job periódico (fora de escopo desta task) pra marcar parcela vencida não paga.</summary>
    public void MarcarVencida()
    {
        if (Status == StatusParcela.Pendente && DataVencimento.Date < DateTime.UtcNow.Date)
        {
            Status = StatusParcela.Vencida;
            SetUpdatedAt();
        }
    }
}
