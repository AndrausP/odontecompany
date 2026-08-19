using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Records.Domain.Entities;

namespace Records.Infrastructure.Persistence.Configurations;

public sealed class AnexoMetadataConfiguration : IEntityTypeConfiguration<AnexoMetadata>
{
    public void Configure(EntityTypeBuilder<AnexoMetadata> builder)
    {
        builder.ToTable("AnexosMetadata");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OrganizationId).IsRequired();
        builder.Property(a => a.ProntuarioId).IsRequired();
        builder.Property(a => a.UploadedByUserId).IsRequired();
        builder.Property(a => a.NomeArquivo).IsRequired().HasMaxLength(255);
        builder.Property(a => a.TipoConteudo).IsRequired().HasMaxLength(100);
        builder.Property(a => a.TamanhoBytes).IsRequired();
        builder.Property(a => a.CaminhoArmazenamento).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.DataUpload).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.OrganizationId, a.ProntuarioId });
    }
}
