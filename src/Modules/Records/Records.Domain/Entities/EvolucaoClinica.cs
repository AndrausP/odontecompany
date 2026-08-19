using Records.Domain.Enums;
using Records.Domain.Errors;
using SharedKernel;

namespace Records.Domain.Entities;

/// <summary>
/// Entrada de evolução clínica de um prontuário — imutável após criada (histórico clínico
/// nunca é editado ou removido, só acumulado). <see cref="DescricaoClinica"/> é dado clínico
/// sensível: fica em texto puro aqui no Domain (a criptografia em repouso é responsabilidade
/// da camada de Infrastructure, via <c>ValueConverter</c> no EF Core — Domain não precisa saber
/// que o campo é cifrado no banco, ver docs/knowledge/patterns.md).
/// Não é AggregateRoot — é criada/persistida sempre através de <c>Prontuario</c> (o repositório
/// de Prontuario que grava a coleção completa).
/// </summary>
public class EvolucaoClinica : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid ProntuarioId { get; private set; }
    public Guid ProfissionalUserId { get; private set; }
    public TipoProcedimento TipoProcedimento { get; private set; }
    public string DescricaoClinica { get; private set; } = string.Empty;
    public DateTime DataRegistro { get; private set; }

    private EvolucaoClinica() { } // EF Core

    private EvolucaoClinica(
        Guid organizationId,
        Guid prontuarioId,
        Guid profissionalUserId,
        TipoProcedimento tipoProcedimento,
        string descricaoClinica)
    {
        OrganizationId = organizationId;
        ProntuarioId = prontuarioId;
        ProfissionalUserId = profissionalUserId;
        TipoProcedimento = tipoProcedimento;
        DescricaoClinica = descricaoClinica;
        DataRegistro = DateTime.UtcNow;
    }

    public static Result<EvolucaoClinica> Create(
        Guid organizationId,
        Guid prontuarioId,
        Guid profissionalUserId,
        TipoProcedimento tipoProcedimento,
        string descricaoClinica)
    {
        if (profissionalUserId == Guid.Empty)
            return Result.Failure<EvolucaoClinica>(DomainErrors.Evolucao.ProfissionalInvalido);

        if (string.IsNullOrWhiteSpace(descricaoClinica))
            return Result.Failure<EvolucaoClinica>(DomainErrors.Evolucao.DescricaoObrigatoria);

        return Result.Success(new EvolucaoClinica(
            organizationId, prontuarioId, profissionalUserId, tipoProcedimento, descricaoClinica.Trim()));
    }
}
