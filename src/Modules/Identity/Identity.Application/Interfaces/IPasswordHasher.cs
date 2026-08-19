namespace Identity.Application.Interfaces;

/// <summary>Porta pro hash de senha (implementação Argon2id fica na Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
