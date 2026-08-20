using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Domain.Entities;

/// <summary>
/// Catálogo de procedimentos/serviços da clínica (ex.: "Limpeza", "Extração", "Canal") — nova
/// feature (task 044). Vive no módulo Scheduling (não Billing): é o mesmo racional de
/// <see cref="Profissional"/>/<see cref="Sala"/> — recurso de referência da Agenda, não fatura em
/// si. <see cref="ValorPadrao"/>/<see cref="DuracaoPadraoMinutos"/> são sugestões (o frontend
/// pré-preenche o formulário de agendamento/conclusão com eles), nunca aplicados automaticamente
/// — o valor final de uma consulta continua sendo o que o handler de conclusão recebe, e a
/// duração final continua sendo o período que o usuário escolher.
/// </summary>
public class Procedimento : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public decimal? ValorPadrao { get; private set; }
    public int? DuracaoPadraoMinutos { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Procedimento() { } // EF Core

    private Procedimento(Guid organizationId, string nome, decimal? valorPadrao, int? duracaoPadraoMinutos)
    {
        OrganizationId = organizationId;
        Nome = nome;
        ValorPadrao = valorPadrao;
        DuracaoPadraoMinutos = duracaoPadraoMinutos;
        Ativo = true;
    }

    public static Result<Procedimento> Criar(Guid organizationId, string nome, decimal? valorPadrao = null, int? duracaoPadraoMinutos = null)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Procedimento>(DomainErrors.Agendamento.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Procedimento>(DomainErrors.Procedimento.NomeObrigatorio);

        if (valorPadrao is < 0)
            return Result.Failure<Procedimento>(DomainErrors.Procedimento.ValorInvalido);

        if (duracaoPadraoMinutos is <= 0)
            return Result.Failure<Procedimento>(DomainErrors.Procedimento.DuracaoInvalida);

        return Result.Success(new Procedimento(organizationId, nome.Trim(), valorPadrao, duracaoPadraoMinutos));
    }

    public Result AtualizarDados(string nome, decimal? valorPadrao, int? duracaoPadraoMinutos)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure(DomainErrors.Procedimento.NomeObrigatorio);

        if (valorPadrao is < 0)
            return Result.Failure(DomainErrors.Procedimento.ValorInvalido);

        if (duracaoPadraoMinutos is <= 0)
            return Result.Failure(DomainErrors.Procedimento.DuracaoInvalida);

        Nome = nome.Trim();
        ValorPadrao = valorPadrao;
        DuracaoPadraoMinutos = duracaoPadraoMinutos;
        SetUpdatedAt();

        return Result.Success();
    }

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }
}
