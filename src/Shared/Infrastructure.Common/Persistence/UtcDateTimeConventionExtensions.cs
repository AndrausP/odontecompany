using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Common.Persistence;

/// <summary>
/// Npgsql (>=6) só aceita <see cref="DateTime"/> com Kind=Utc pra coluna "timestamp with time
/// zone" — qualquer DateTime vindo de model binding de JSON (ex.: "2024-06-05" num
/// CreatePatientRequest) chega com Kind=Unspecified e derruba o SaveChanges com
/// <c>ArgumentException: Cannot write DateTime with Kind=Unspecified</c> (achado ao validar os
/// endpoints — sprint-11, ver docs/decisions.md). Em vez de `DateTime.SpecifyKind` espalhado em
/// cada handler (frágil, esquece um lugar e quebra de novo), a convenção abaixo força TODA
/// propriedade DateTime do modelo a Kind=Utc na escrita — mesma correção aplicada nos 7
/// DbContexts do monólito.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toProvider => toProvider.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(toProvider, DateTimeKind.Utc)
        : toProvider.ToUniversalTime(),
    fromProvider => DateTime.SpecifyKind(fromProvider, DateTimeKind.Utc));

public sealed class UtcNullableDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    toProvider => toProvider.HasValue
        ? toProvider.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(toProvider.Value, DateTimeKind.Utc)
            : toProvider.Value.ToUniversalTime()
        : toProvider,
    fromProvider => fromProvider.HasValue ? DateTime.SpecifyKind(fromProvider.Value, DateTimeKind.Utc) : fromProvider);

public static class UtcDateTimeConventionExtensions
{
    public static void ApplyUtcDateTimeConversion(this ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
    }
}
