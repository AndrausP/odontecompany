using Records.Domain.Enums;
using Records.Domain.Errors;
using SharedKernel;

namespace Records.Domain.Entities;

/// <summary>
/// Prontuário eletrônico de um paciente — dado clínico sensível sob a LGPD. Um prontuário por
/// paciente por organization. Odontograma é um mapa simples dente(FDI) → status, armazenado como
/// JSONB (ver <see cref="Infrastructure.Persistence.Configurations.ProntuarioConfiguration"/>).
/// As entradas de evolução clínica (<see cref="EvolucaoClinica"/>) só são adicionadas, nunca
/// removidas ou editadas — histórico clínico é imutável por natureza. Anexos idem.
/// Nunca é apagado fisicamente — só desativado (mesmo padrão de <c>Patient.Desativar</c>).
/// </summary>
public class Prontuario : AggregateRoot, IMustHaveOrganization
{
    private readonly Dictionary<int, StatusDente> _odontograma = new();

    public Guid OrganizationId { get; private set; }
    public Guid PacienteId { get; private set; }
    public Guid CriadoPorUserId { get; private set; }
    public bool Ativo { get; private set; } = true;

    public IReadOnlyDictionary<int, StatusDente> Odontograma => _odontograma;

    private Prontuario() { } // EF Core

    private Prontuario(Guid organizationId, Guid pacienteId, Guid criadoPorUserId)
    {
        OrganizationId = organizationId;
        PacienteId = pacienteId;
        CriadoPorUserId = criadoPorUserId;
        Ativo = true;
    }

    public static Result<Prontuario> Create(Guid organizationId, Guid pacienteId, Guid criadoPorUserId)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Prontuario>(DomainErrors.Prontuario.OrganizationInvalido);

        if (pacienteId == Guid.Empty)
            return Result.Failure<Prontuario>(DomainErrors.Prontuario.PacienteInvalido);

        return Result.Success(new Prontuario(organizationId, pacienteId, criadoPorUserId));
    }

    /// <summary>
    /// Atualiza o status de um dente no odontograma (numeração FDI: 11-18, 21-28, 31-38,
    /// 41-48). Sobrescreve o status anterior daquele dente — o odontograma reflete o estado
    /// ATUAL, o histórico de mudança fica registrado como entrada de evolução clínica separada,
    /// não no odontograma em si.
    /// </summary>
    public void AtualizarDente(int numeroDente, StatusDente status)
    {
        _odontograma[numeroDente] = status;
        SetUpdatedAt();
    }

    /// <summary>Soft delete — nunca remove a linha do banco, nem as evoluções/anexos vinculados.</summary>
    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }
}
