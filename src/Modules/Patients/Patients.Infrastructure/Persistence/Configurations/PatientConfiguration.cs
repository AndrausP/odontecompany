using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Patients.Domain.Entities;
using Patients.Domain.ValueObjects;

namespace Patients.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.NomeCompleto).IsRequired().HasMaxLength(200);

        // Cpf é VO (Patients.Domain.ValueObjects.Cpf), persistido como os 11 dígitos puros —
        // conversão explícita porque o EF Core não sabe mapear tipo de domínio sozinho.
        builder.Property(p => p.Cpf)
            .HasConversion(cpf => cpf.Numero, value => Cpf.Create(value).Value)
            .HasColumnName("Cpf")
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(p => p.DataNascimento).IsRequired();
        builder.Property(p => p.Telefone).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.Endereco).HasMaxLength(500);
        builder.Property(p => p.ConsentimentoLgpd).IsRequired();
        builder.Property(p => p.DataConsentimentoLgpd);
        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        // Índice único COMPOSTO (OrganizationId, Cpf) — de propósito, não índice único global no Cpf.
        // CPF pode se repetir entre organizations diferentes: cada clínica é um cliente independente
        // da plataforma (diferente da decisão de email do Identity, que é global).
        builder.HasIndex(p => new { p.OrganizationId, p.Cpf }).IsUnique();
    }
}
