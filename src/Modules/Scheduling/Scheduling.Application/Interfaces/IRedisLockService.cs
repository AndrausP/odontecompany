namespace Scheduling.Application.Interfaces;

/// <summary>
/// Lock distribuído curto no Redis — protege a corrida entre requisições concorrentes
/// confirmando o mesmo slot (profissional + horário) antes do commit no banco. RowVersion
/// (xmin) é a segunda linha de defesa, mais barata, que pega o que o lock eventualmente deixar
/// passar (ex: TTL do lock expirou no meio de uma requisição lenta).
/// </summary>
public interface IRedisLockService
{
    /// <summary>Tenta adquirir o lock. Retorna um token de posse (necessário pra liberar depois) ou <c>null</c> se não conseguiu.</summary>
    Task<string?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Libera o lock. Idempotente — chamar mesmo sem ter certeza que o lock ainda é seu não é erro (comparação de token cuida disso).</summary>
    Task ReleaseAsync(string key, string lockToken, CancellationToken ct = default);
}
