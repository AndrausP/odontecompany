using Scheduling.Domain.Errors;
using SharedKernel;

namespace Scheduling.Domain.Entities;

/// <summary>Sala/consultório onde um agendamento acontece. CapacidadeMaxima é opcional (nem toda clínica controla isso).</summary>
public class Sala : Entity, IMustHaveOrganization
{
    public Guid OrganizationId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int? CapacidadeMaxima { get; private set; }

    /// <summary>Branch (clínica física) a que esta sala pertence — mesmo racional de <c>Profissional.BranchId</c> (Fase 5, módulo Tenancy). Null = compatível com comportamento anterior à Fase 5.</summary>
    public Guid? BranchId { get; private set; }
    public bool Ativa { get; private set; } = true;

    private Sala() { } // EF Core

    private Sala(Guid organizationId, string nome, int? capacidadeMaxima, Guid? branchId)
    {
        OrganizationId = organizationId;
        Nome = nome;
        CapacidadeMaxima = capacidadeMaxima;
        BranchId = branchId;
        Ativa = true;
    }

    public static Result<Sala> Criar(Guid organizationId, string nome, int? capacidadeMaxima = null, Guid? branchId = null)
    {
        if (organizationId == Guid.Empty)
            return Result.Failure<Sala>(DomainErrors.Agendamento.OrganizationInvalido);

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Sala>(DomainErrors.Sala.NomeObrigatorio);

        if (capacidadeMaxima is <= 0)
            return Result.Failure<Sala>(DomainErrors.Sala.CapacidadeInvalida);

        return Result.Success(new Sala(organizationId, nome.Trim(), capacidadeMaxima, branchId));
    }

    public void Desativar()
    {
        Ativa = false;
        SetUpdatedAt();
    }

    public void Ativar()
    {
        Ativa = true;
        SetUpdatedAt();
    }

    /// <summary>Edição de cadastro (tela de Configurações) — mesma validação de <see cref="Criar"/>.</summary>
    public Result AtualizarDados(string nome, int? capacidadeMaxima, Guid? branchId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure(DomainErrors.Sala.NomeObrigatorio);

        if (capacidadeMaxima is <= 0)
            return Result.Failure(DomainErrors.Sala.CapacidadeInvalida);

        Nome = nome.Trim();
        CapacidadeMaxima = capacidadeMaxima;
        BranchId = branchId;
        SetUpdatedAt();

        return Result.Success();
    }
}
