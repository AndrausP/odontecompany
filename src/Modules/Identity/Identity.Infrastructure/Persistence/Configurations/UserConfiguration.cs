using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Ativo).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        // Email globalmente único — é o que permite ao login achar o usuário sem o cliente
        // precisar informar a organization (ver AuthController.Login). User é GLOBAL desde a task
        // 013 (sem OrganizationId/Role direto — ver OrganizationMembership).
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
