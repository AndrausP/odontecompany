using Contracts.Abstractions;

namespace Scheduling.Contracts;

/// <summary>Uma janela de tempo livre na agenda de um profissional num dia — resultado de <c>ListarDisponibilidadeQuery</c>.</summary>
public sealed record AvailabilitySlotDto(DateTime Inicio, DateTime Fim) : IModuleContract;
