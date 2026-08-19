namespace Records.Domain.Enums;

/// <summary>Tipo de acesso registrado na trilha de auditoria do prontuário — LGPD exige rastrear leitura e escrita.</summary>
public enum AcaoAuditoria
{
    Leitura,
    Criacao,
    AdicaoEvolucao,
    UploadAnexo
}
