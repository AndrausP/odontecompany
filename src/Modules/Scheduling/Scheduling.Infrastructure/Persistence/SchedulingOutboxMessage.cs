namespace Scheduling.Infrastructure.Persistence;

/// <summary>
/// Outbox pattern (docs/decisions.md): garante entrega eventual de eventos de integração —
/// gravado NA MESMA transação do agregado que disparou o evento (ver
/// SchedulingDbContext.SaveChangesAsync), nunca em uma chamada separada. Pura infraestrutura,
/// não é entidade de domínio (não implementa BaseEntity/Entity do SharedKernel) — não carrega
/// regra de negócio nenhuma, só encanamento de mensageria.
///
/// PENDÊNCIA (documentada a pedido do Architect/Tech Lead): não existe worker/BackgroundService
/// publicando estas mensagens via MassTransit/RabbitMQ ainda — isso é infraestrutura de runtime
/// que não dá pra testar sem broker rodando neste ambiente. Ver docs/knowledge (Writer) para o
/// item de pendência formal.
/// </summary>
public sealed class SchedulingOutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
    public DateTime? PublishedOn { get; private set; }

    private SchedulingOutboxMessage() { } // EF Core

    private SchedulingOutboxMessage(string type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
    }

    public static SchedulingOutboxMessage Create(string type, string payload) => new(type, payload);

    public void MarcarPublicado() => PublishedOn = DateTime.UtcNow;
}
