using Records.Domain.Enums;
using SharedKernel;

namespace Records.Domain.Entities;

/// <summary>
/// Trilha de auditoria de acesso a prontuário — LGPD exige registrar quem acessou (leu OU
/// escreveu) qual prontuário, quando e o quê. APPEND-ONLY por design: esta entidade não tem
/// nenhum método de mutação (só o construtor via <see cref="Registrar"/>) e o repositório
/// (<c>Records.Infrastructure.Audit.AuditLogWriter</c>) só expõe <c>AddAsync</c> — não existe
/// caminho de código pra Update/Delete. Endurecimento de produção pendente: revogar permissão
/// DELETE/UPDATE nesta tabela diretamente no Postgres (não configurável sem banco real
/// disponível neste ambiente — ver docs/tasks/005-prontuario-eletronico.md, riscos residuais).
/// Gravada em transação PRÓPRIA, separada da operação principal (ver AuditLogWriter) — assim
/// uma falha depois da leitura/escrita do prontuário não apaga o rastro de que o acesso
/// aconteceu.
/// </summary>
public class RecordsAuditLog : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public Guid ProntuarioId { get; private set; }
    public Guid PacienteId { get; private set; }
    public Guid UserId { get; private set; }
    public AcaoAuditoria Acao { get; private set; }
    public DateTime Timestamp { get; private set; }

    private RecordsAuditLog() { } // EF Core

    private RecordsAuditLog(Guid organizationId, Guid prontuarioId, Guid pacienteId, Guid userId, AcaoAuditoria acao)
    {
        OrganizationId = organizationId;
        ProntuarioId = prontuarioId;
        PacienteId = pacienteId;
        UserId = userId;
        Acao = acao;
        Timestamp = DateTime.UtcNow;
    }

    public static RecordsAuditLog Registrar(Guid organizationId, Guid prontuarioId, Guid pacienteId, Guid userId, AcaoAuditoria acao)
        => new(organizationId, prontuarioId, pacienteId, userId, acao);
}
