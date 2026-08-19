using Billing.Domain.Enums;
using Billing.Domain.Errors;
using SharedKernel;

namespace Billing.Domain.Entities;

/// <summary>
/// Fatura — particular ou de convênio. Raiz de agregado: garante a invariante "soma das
/// parcelas == valor total" na criação, e recalcula o próprio <see cref="Status"/> a partir do
/// status de cada parcela sempre que uma é paga (nunca deixa o status da fatura dessincronizar
/// do status real das parcelas). Convênio passa por Anti-Corruption Layer (<c>IConvenioAdapter</c>,
/// Billing.Application) — o formato externo do convênio nunca entra aqui no Domain.
/// </summary>
public class Fatura : AggregateRoot, IMustHaveOrganization
{
    private readonly List<Parcela> _parcelas = new();

    public Guid OrganizationId { get; private set; }
    public Guid PacienteId { get; private set; }
    public Guid? AgendamentoId { get; private set; }
    public Guid? ProfissionalId { get; private set; }
    public TipoFatura TipoFatura { get; private set; }
    public Guid? ConvenioId { get; private set; }
    public decimal ValorTotal { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public decimal? ComissaoDentistaPercentual { get; private set; }
    public StatusFatura Status { get; private set; }

    /// <summary>Número de protocolo/referência devolvido pelo convênio externo (via IConvenioAdapter) — null até o envio ser confirmado.</summary>
    public string? ProtocoloConvenio { get; private set; }

    public IReadOnlyList<Parcela> Parcelas => _parcelas.AsReadOnly();

    private Fatura() { } // EF Core

    private Fatura(
        Guid organizationId,
        Guid pacienteId,
        Guid? agendamentoId,
        Guid? profissionalId,
        TipoFatura tipoFatura,
        Guid? convenioId,
        decimal valorTotal,
        FormaPagamento formaPagamento,
        decimal? comissaoDentistaPercentual)
    {
        OrganizationId = organizationId;
        PacienteId = pacienteId;
        AgendamentoId = agendamentoId;
        ProfissionalId = profissionalId;
        TipoFatura = tipoFatura;
        ConvenioId = convenioId;
        ValorTotal = valorTotal;
        FormaPagamento = formaPagamento;
        ComissaoDentistaPercentual = comissaoDentistaPercentual;
        Status = StatusFatura.Pendente;
    }

    private const int NumeroMaximoParcelas = 12;

    public static Result<Fatura> CreateParticular(
        Guid organizationId,
        Guid pacienteId,
        Guid? agendamentoId,
        Guid? profissionalId,
        decimal valorTotal,
        int numeroParcelas,
        FormaPagamento formaPagamento,
        decimal? comissaoDentistaPercentual = null)
    {
        var validation = ValidarCriacao(organizationId, pacienteId, valorTotal, numeroParcelas);
        if (validation.IsFailure)
            return Result.Failure<Fatura>(validation.Error);

        var fatura = new Fatura(
            organizationId, pacienteId, agendamentoId, profissionalId,
            TipoFatura.Particular, convenioId: null, valorTotal, formaPagamento, comissaoDentistaPercentual);

        fatura.GerarParcelas(numeroParcelas);

        return Result.Success(fatura);
    }

    public static Result<Fatura> CreateConvenio(
        Guid organizationId,
        Guid pacienteId,
        Guid? agendamentoId,
        Guid? profissionalId,
        Guid convenioId,
        decimal valorTotal,
        decimal? comissaoDentistaPercentual = null)
    {
        var validation = ValidarCriacao(organizationId, pacienteId, valorTotal, numeroParcelas: 1);
        if (validation.IsFailure)
            return Result.Failure<Fatura>(validation.Error);

        if (convenioId == Guid.Empty)
            return Result.Failure<Fatura>(DomainErrors.Fatura.ConvenioObrigatorioParaFaturaDeConvenio);

        // Fatura de convênio é sempre 1 "parcela" internamente — o parcelamento real (se
        // houver) é regra do convênio externo, fora do controle desta plataforma.
        var fatura = new Fatura(
            organizationId, pacienteId, agendamentoId, profissionalId,
            TipoFatura.Convenio, convenioId, valorTotal, FormaPagamento.Boleto, comissaoDentistaPercentual);

        fatura.GerarParcelas(1);

        return Result.Success(fatura);
    }

    private static Result ValidarCriacao(Guid organizationId, Guid pacienteId, decimal valorTotal, int numeroParcelas)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure(DomainErrors.Fatura.OrganizationInvalido);

        if (pacienteId == Guid.Empty)
            return Result.Failure(DomainErrors.Fatura.PacienteInvalido);

        if (valorTotal <= 0)
            return Result.Failure(DomainErrors.Fatura.ValorInvalido);

        if (numeroParcelas is < 1 or > NumeroMaximoParcelas)
            return Result.Failure(DomainErrors.Fatura.NumeroParcelasInvalido);

        return Result.Success();
    }

    /// <summary>
    /// Divide o valor total em N parcelas iguais, jogando o resto de arredondamento na ÚLTIMA
    /// parcela — evita perda de centavos (soma das parcelas sempre bate exatamente com
    /// ValorTotal, nunca R$0,01 a menos por causa de divisão com resto).
    /// </summary>
    private void GerarParcelas(int numeroParcelas)
    {
        var valorParcelaBase = Math.Round(ValorTotal / numeroParcelas, 2, MidpointRounding.ToZero);
        var somaParcelasAnteriores = 0m;

        for (var i = 1; i <= numeroParcelas; i++)
        {
            var valorParcela = i < numeroParcelas ? valorParcelaBase : ValorTotal - somaParcelasAnteriores;
            somaParcelasAnteriores += valorParcela;

            _parcelas.Add(new Parcela(OrganizationId, Id, i, valorParcela, DateTime.UtcNow.AddMonths(i - 1).Date));
        }
    }

    public Result RegistrarPagamentoParcela(Guid parcelaId)
    {
        var parcela = _parcelas.FirstOrDefault(p => p.Id == parcelaId);
        if (parcela is null)
            return Result.Failure(DomainErrors.Parcela.NaoEncontrada);

        var result = parcela.RegistrarPagamento();
        if (result.IsFailure)
            return result;

        RecalcularStatus();
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Status da fatura é SEMPRE derivado do status real das parcelas — nunca setado direto de fora.</summary>
    private void RecalcularStatus()
    {
        if (_parcelas.All(p => p.Status == StatusParcela.Paga))
            Status = StatusFatura.Paga;
        else if (_parcelas.Any(p => p.Status == StatusParcela.Paga))
            Status = StatusFatura.ParcialmentePaga;
        else if (_parcelas.Any(p => p.Status == StatusParcela.Vencida))
            Status = StatusFatura.Vencida;
        else
            Status = StatusFatura.Pendente;
    }

    public Result Cancelar()
    {
        if (Status == StatusFatura.Cancelada)
            return Result.Failure(DomainErrors.Fatura.JaCancelada);

        if (Status == StatusFatura.Paga)
            return Result.Failure(DomainErrors.Fatura.NaoPodeCancelarFaturaPaga);

        Status = StatusFatura.Cancelada;
        SetUpdatedAt();

        return Result.Success();
    }

    /// <summary>Valor da comissão do dentista sobre esta fatura — 0 se não houver percentual configurado.</summary>
    public decimal CalcularValorComissao()
        => ComissaoDentistaPercentual is null or 0 ? 0 : Math.Round(ValorTotal * ComissaoDentistaPercentual.Value / 100, 2);

    /// <summary>
    /// Valor da comissão do dentista proporcional a um valor efetivamente pago (regime de CAIXA)
    /// — usado pra somar comissão por parcela paga no período (task 022), diferente de
    /// <see cref="CalcularValorComissao"/>, que é sobre o <see cref="ValorTotal"/> inteiro da
    /// fatura (regime de competência). 0 se não houver percentual configurado.
    /// </summary>
    public decimal CalcularComissaoSobre(decimal valorPago)
        => ComissaoDentistaPercentual is null or 0 ? 0 : Math.Round(valorPago * ComissaoDentistaPercentual.Value / 100, 2);

    /// <summary>Registra o protocolo devolvido pelo convênio externo após envio bem-sucedido via IConvenioAdapter (ACL).</summary>
    public void RegistrarProtocoloConvenio(string protocolo)
    {
        ProtocoloConvenio = protocolo;
        SetUpdatedAt();
    }
}
