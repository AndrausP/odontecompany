namespace Records.Application.Interfaces;

/// <summary>
/// Porta de criptografia simétrica pra campos clínicos sensíveis em repouso. Implementação
/// concreta (AES-256-GCM) mora em Records.Infrastructure, consumida pelo EF Core via
/// ValueConverter — nunca chamada diretamente pela Application, que não deveria precisar saber
/// que um campo está cifrado (isso é detalhe de persistência, ver docs/knowledge/patterns.md).
/// Exposta aqui como porta pra permitir teste unitário isolado de round-trip encrypt/decrypt.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
