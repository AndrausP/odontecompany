using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Records.Application.Interfaces;
using Records.Domain.Entities;

namespace Records.Infrastructure.Persistence.Configurations;

/// <summary>
/// DIFERENTE das outras configurations do módulo: recebe <see cref="IEncryptionService"/> no
/// construtor pra cifrar <c>DescricaoClinica</c> em repouso via <c>HasConversion</c>. Por causa
/// disso, esta classe NÃO é descoberta por <c>ApplyConfigurationsFromAssembly</c> (que só
/// instancia configurations com construtor sem parâmetro via reflection) — é aplicada
/// explicitamente em <see cref="RecordsDbContext.OnModelCreating"/>, junto com as demais.
/// </summary>
public sealed class EvolucaoClinicaConfiguration : IEntityTypeConfiguration<EvolucaoClinica>
{
    private readonly IEncryptionService _encryptionService;

    public EvolucaoClinicaConfiguration(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<EvolucaoClinica> builder)
    {
        builder.ToTable("EvolucoesClinicas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrganizationId).IsRequired();
        builder.Property(e => e.ProntuarioId).IsRequired();
        builder.Property(e => e.ProfissionalUserId).IsRequired();
        builder.Property(e => e.TipoProcedimento).IsRequired().HasConversion<string>().HasMaxLength(40);

        // Dado clínico sensível — cifrado em repouso (AES-256-GCM via IEncryptionService).
        // Domain nunca vê o ciphertext: EvolucaoClinica.DescricaoClinica sempre é texto puro em
        // memória, a conversão acontece só na fronteira com o banco (Infrastructure).
        builder.Property(e => e.DescricaoClinica)
            .HasConversion(
                plaintext => _encryptionService.Encrypt(plaintext),
                ciphertext => _encryptionService.Decrypt(ciphertext))
            .HasColumnName("DescricaoClinicaCifrada")
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.DataRegistro).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.OrganizationId, e.ProntuarioId });
    }
}
