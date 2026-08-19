using Billing.Domain.Entities;
using SharedKernel;

namespace Billing.Application.Interfaces;

/// <summary>
/// Anti-Corruption Layer (ACL) — porta genérica que todo adaptador de convênio implementa.
/// Cada convênio tem seu PRÓPRIO adaptador (protocolo, autenticação e formato de payload
/// variam por operadora); o formato externo NUNCA vaza pro Domain — o adaptador traduz a
/// resposta externa pra um <see cref="Result{T}"/> com um identificador de protocolo genérico
/// (string), único dado que a Application/Domain precisam saber.
/// Implementação de referência: <c>Billing.Infrastructure.Adapters.ManualConvenioAdapter</c> —
/// registro manual de envio (sem integração real com nenhuma operadora ainda). Trocar por
/// adaptador real por convênio quando a integração existir, sem tocar Application/Domain.
/// </summary>
public interface IConvenioAdapter
{
    /// <summary>Envia a fatura pro convênio externo. Devolve o número de protocolo/referência externa em caso de sucesso.</summary>
    Task<Result<string>> EnviarFaturaAsync(Fatura fatura, Convenio convenio, CancellationToken ct = default);
}
