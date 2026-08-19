namespace Records.Infrastructure.Security;

/// <summary>
/// Chave simétrica pra criptografia de campo (AES-256-GCM) dos dados clínicos sensíveis em
/// repouso. <see cref="KeyBase64"/> é DEV-ONLY quando lido de appsettings — produção precisa de
/// key vault/KMS gerenciado (Azure Key Vault, AWS KMS, etc.), troca de fonte sem tocar em
/// <see cref="AesEncryptionService"/> (só o provedor de configuração muda). Documentado como
/// dívida técnica conhecida em docs/tasks/005-prontuario-eletronico.md.
/// </summary>
public sealed class RecordsEncryptionOptions
{
    public const string SectionName = "Records:Encryption";

    /// <summary>
    /// Material de chave em base64 — qualquer tamanho, <see cref="AesEncryptionService"/> deriva
    /// os 32 bytes reais via SHA-256 (evita erro de "chave com tamanho errado" se alguém
    /// configurar um valor que não seja exatamente 32 bytes). Valor default é só pra dev local.
    /// </summary>
    public string KeyBase64 { get; set; } = "CHANGE-ME-records-encryption-key-dev-only";
}
