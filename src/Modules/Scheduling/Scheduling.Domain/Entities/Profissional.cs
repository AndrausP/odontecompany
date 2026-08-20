using Scheduling.Domain.Enums;
using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Domain.Entities;

/// <summary>
/// Profissional da agenda (dentista, higienista etc). Entidade LOCAL ao módulo Scheduling — não
/// é o mesmo conceito que <c>Identity.Domain.Entities.User</c>. <see cref="UserId"/> é referência
/// solta e opcional ao usuário de acesso correspondente (se existir), sem FK/dependência de
/// projeto: Scheduling.Domain não referencia Identity.Domain (decisão registrada em docs/decisions.md).
/// </summary>
public class Profissional : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Especialidade { get; private set; } = string.Empty;
    public TipoContrato TipoContrato { get; private set; }

    /// <summary>Comissão padrão sugerida pra faturas deste profissional — puramente informativa
    /// aqui (não é lida automaticamente por Billing nesta rodada, cada Fatura ainda define o
    /// próprio percentual). Opcional.</summary>
    public decimal? PercentualComissaoDefault { get; private set; }

    /// <summary>
    /// Email de contato — se informado na criação, dispara um convite (Role.Dentista) pra esse
    /// email (orquestrado pelo controller, task 042). Continua preenchido mesmo depois de
    /// <see cref="UserId"/> vinculado (histórico de contato), não é "limpo" no aceite.
    /// </summary>
    public string? Email { get; private set; }

    public Guid? UserId { get; private set; }

    /// <summary>
    /// Branch (clínica física) a que este profissional pertence — nível opcional da hierarquia
    /// Organization→Branch→Recurso (Fase 5, módulo Tenancy). Null = profissional "solto" no organization,
    /// sem vínculo de branch (compatível com o comportamento anterior à Fase 5). Referência
    /// solta (Guid?), sem FK de projeto — mesmo racional de <see cref="UserId"/>: Scheduling.Domain
    /// não referencia Tenancy.Domain.
    /// </summary>
    public Guid? BranchId { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Profissional() { } // EF Core

    private Profissional(
        Guid organizationId, string nome, string especialidade, TipoContrato tipoContrato,
        decimal? percentualComissaoDefault, string? email, Guid? userId, Guid? branchId)
    {
        OrganizationId = organizationId;
        Nome = nome;
        Especialidade = especialidade;
        TipoContrato = tipoContrato;
        PercentualComissaoDefault = percentualComissaoDefault;
        Email = email;
        UserId = userId;
        BranchId = branchId;
        Ativo = true;
    }

    public static Result<Profissional> Criar(
        Guid organizationId, string nome, string especialidade, TipoContrato tipoContrato,
        decimal? percentualComissaoDefault = null, string? email = null, Guid? userId = null, Guid? branchId = null)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Profissional>(DomainErrors.Agendamento.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Profissional>(DomainErrors.Profissional.NomeObrigatorio);

        if (string.IsNullOrWhiteSpace(especialidade))
            return Result.Failure<Profissional>(DomainErrors.Profissional.EspecialidadeObrigatoria);

        return Result.Success(new Profissional(
            organizationId,
            nome.Trim(),
            especialidade.Trim(),
            tipoContrato,
            percentualComissaoDefault,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            userId,
            branchId));
    }

    public void Desativar()
    {
        Ativo = false;
        SetUpdatedAt();
    }

    public void Ativar()
    {
        Ativo = true;
        SetUpdatedAt();
    }

    /// <summary>
    /// Vincula o usuário de acesso correspondente — chamado quando o convite mandado pro
    /// <see cref="Email"/> deste profissional é aceito (task 042). Idempotente/defensivo: não
    /// sobrescreve um vínculo já existente (primeiro aceite vence; um segundo convite pro mesmo
    /// email não rouba o vínculo de quem já é o usuário real deste profissional).
    /// </summary>
    public void VincularUsuario(Guid userId)
    {
        if (UserId is not null)
            return;

        UserId = userId;
        SetUpdatedAt();
    }
}
