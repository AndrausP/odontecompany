namespace Records.Contracts;

public sealed record ProntuarioDto(
    Guid Id,
    Guid PacienteId,
    bool Ativo,
    IReadOnlyDictionary<int, string> Odontograma,
    IReadOnlyList<EvolucaoClinicaDto> Evolucoes,
    IReadOnlyList<AnexoDto> Anexos,
    DateTime CreatedAt);

public sealed record EvolucaoClinicaDto(
    Guid Id,
    Guid ProfissionalUserId,
    string TipoProcedimento,
    string DescricaoClinica,
    DateTime DataRegistro);

public sealed record AnexoDto(
    Guid Id,
    string NomeArquivo,
    string TipoConteudo,
    long TamanhoBytes,
    DateTime DataUpload);

/// <summary>Entrada da trilha de auditoria — exposta só pra Admin (view de compliance).</summary>
public sealed record AuditLogEntryDto(
    Guid Id,
    Guid ProntuarioId,
    Guid PacienteId,
    Guid UserId,
    string Acao,
    DateTime Timestamp);
