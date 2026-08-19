using Scheduling.Contracts;
using Scheduling.Domain.Entities;

namespace Scheduling.Application.Mapping;

/// <summary>Mapeamento Domain → Contracts. Mora na Application (não em Scheduling.Contracts) — Contracts não pode depender de Scheduling.Domain.</summary>
public static class AgendamentoMappingExtensions
{
    public static AgendamentoDto ToDto(this Agendamento agendamento) => new(
        agendamento.Id,
        agendamento.PacienteId,
        agendamento.ProfissionalId,
        agendamento.SalaId,
        agendamento.Periodo.Inicio,
        agendamento.Periodo.Fim,
        agendamento.Status.ToString(),
        agendamento.MotivoCancelamento,
        agendamento.ValorConsulta,
        agendamento.CreatedAt,
        agendamento.UpdatedAt);

    public static ProfissionalDto ToDto(this Profissional profissional) => new(
        profissional.Id, profissional.Nome, profissional.Especialidade, profissional.UserId, profissional.BranchId, profissional.Ativo);

    public static SalaDto ToDto(this Sala sala) => new(
        sala.Id, sala.Nome, sala.CapacidadeMaxima, sala.BranchId, sala.Ativa);
}
