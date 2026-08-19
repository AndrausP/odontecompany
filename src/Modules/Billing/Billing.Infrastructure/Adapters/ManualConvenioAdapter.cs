using Billing.Application.Interfaces;
using Billing.Domain.Entities;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Billing.Infrastructure.Adapters;

/// <summary>
/// Implementação de referência de <see cref="IConvenioAdapter"/> — NÃO integra com nenhuma
/// operadora real. Gera um protocolo local (não é confirmação de recebimento pela operadora) e
/// loga a "intenção de envio", pra manter o fluxo fim-a-fim funcional (persistência,
/// idempotência, RBAC, testes) sem depender de credenciais/API de nenhum convênio específico.
/// Dívida técnica documentada: cada convênio real (ex: Amil Dental, Odontoprev) precisa do
/// PRÓPRIO adaptador (protocolo, autenticação e payload são específicos de cada operadora) —
/// troca de implementação no DI (<c>Billing.Infrastructure.DependencyInjection</c>), sem tocar
/// Application/Domain (é exatamente o que a Anti-Corruption Layer existe pra permitir).
/// </summary>
public sealed class ManualConvenioAdapter : IConvenioAdapter
{
    private readonly ILogger<ManualConvenioAdapter> _logger;

    public ManualConvenioAdapter(ILogger<ManualConvenioAdapter> logger) => _logger = logger;

    public Task<Result<string>> EnviarFaturaAsync(Fatura fatura, Convenio convenio, CancellationToken ct = default)
    {
        var protocoloBruto = $"MANUAL-{convenio.CodigoExterno ?? convenio.Id.ToString("N")[..8]}-{fatura.Id:N}";
        var protocolo = protocoloBruto[..Math.Min(100, protocoloBruto.Length)];

        _logger.LogInformation(
            "Fatura {FaturaId} registrada pra envio manual ao convênio {ConvenioNome} (protocolo local {Protocolo}) — sem integração real, ver Billing.Infrastructure.Adapters.ManualConvenioAdapter.",
            fatura.Id, convenio.Nome, protocolo);

        return Task.FromResult(Result.Success(protocolo));
    }
}
