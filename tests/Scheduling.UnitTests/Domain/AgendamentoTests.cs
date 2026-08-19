using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Scheduling.Domain.Events;

namespace Scheduling.UnitTests.Domain;

[TestFixture]
public class AgendamentoTests
{
    private static readonly DateTime ValidInicio = new(2026, 8, 20, 9, 0, 0);
    private static readonly DateTime ValidFim = new(2026, 8, 20, 9, 30, 0);

    private static Agendamento CriarAgendamentoValido(Guid? organizationId = null)
        => Agendamento.Criar(organizationId ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim).Value;

    [Test]
    public void Should_CreateAgendamento_When_AllDataIsValid()
    {
        var result = Agendamento.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Status, Is.EqualTo(AgendamentoStatus.Agendado));
    }

    [Test]
    public void Should_ReturnFailure_When_OrganizationIdIsEmpty()
    {
        var result = Agendamento.Criar(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidFim);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.OrganizationInvalido"));
    }

    [Test]
    public void Should_ReturnFailure_When_PeriodoIsInvalid()
    {
        var result = Agendamento.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ValidInicio, ValidInicio.AddMinutes(5));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("PeriodoHorario.DuracaoMinimaNaoAtingida"));
    }

    // ── Confirmar: só a partir de Agendado ────────────────────────────────────────────────────
    [Test]
    public void Should_Confirm_When_StatusIsAgendado()
    {
        var agendamento = CriarAgendamentoValido();

        var result = agendamento.Confirmar();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(agendamento.Status, Is.EqualTo(AgendamentoStatus.Confirmado));
        Assert.That(agendamento.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public void Should_ReturnFailure_When_ConfirmingAgendamentoThatIsAlreadyConfirmado()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Confirmar();

        var result = agendamento.Confirmar();

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SoConfirmaSeAgendado"));
    }

    [Test]
    public void Should_ReturnFailure_When_ConfirmingCanceledAgendamento()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Cancelar();

        var result = agendamento.Confirmar();

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SoConfirmaSeAgendado"));
    }

    // ── Cancelar: a partir de Agendado ou Confirmado ──────────────────────────────────────────
    [Test]
    public void Should_Cancel_When_StatusIsAgendado()
    {
        var agendamento = CriarAgendamentoValido();

        var result = agendamento.Cancelar("paciente desmarcou");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(agendamento.Status, Is.EqualTo(AgendamentoStatus.Cancelado));
        Assert.That(agendamento.MotivoCancelamento, Is.EqualTo("paciente desmarcou"));
    }

    [Test]
    public void Should_Cancel_When_StatusIsConfirmado()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Confirmar();

        var result = agendamento.Cancelar();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(agendamento.Status, Is.EqualTo(AgendamentoStatus.Cancelado));
    }

    [Test]
    public void Should_ReturnFailure_When_CancelingAlreadyCanceledAgendamento()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Cancelar();

        var result = agendamento.Cancelar();

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SoCancelaSeAgendadoOuConfirmado"));
    }

    // ── MarcarConcluido: só a partir de Confirmado, e dispara o domain event ─────────────────
    [Test]
    public void Should_MarkAsConcluido_And_RaiseDomainEvent_When_StatusIsConfirmado()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Confirmar();

        var result = agendamento.MarcarConcluido(150.00m);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(agendamento.Status, Is.EqualTo(AgendamentoStatus.Concluido));
        Assert.That(agendamento.ValorConsulta, Is.EqualTo(150.00m));

        Assert.That(agendamento.DomainEvents, Has.Count.EqualTo(1));
        var domainEvent = agendamento.DomainEvents.Single();
        Assert.That(domainEvent, Is.InstanceOf<ConsultaConcluidaEvent>());

        var consultaConcluida = (ConsultaConcluidaEvent)domainEvent;
        Assert.That(consultaConcluida.AgendamentoId, Is.EqualTo(agendamento.Id));
        Assert.That(consultaConcluida.Valor, Is.EqualTo(150.00m));
    }

    [Test]
    public void Should_ReturnFailure_When_MarkingAsConcluido_But_StatusIsAgendado()
    {
        var agendamento = CriarAgendamentoValido();

        var result = agendamento.MarcarConcluido(150.00m);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.SoConcluiSeConfirmado"));
        Assert.That(agendamento.DomainEvents, Is.Empty, "não deve disparar evento quando a transição falha");
    }

    [Test]
    public void Should_ReturnFailure_When_MarkingAsConcluido_With_ValorZeroOrNegative()
    {
        var agendamento = CriarAgendamentoValido();
        agendamento.Confirmar();

        var result = agendamento.MarcarConcluido(0m);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo("Agendamento.ValorInvalido"));
        Assert.That(agendamento.DomainEvents, Is.Empty);
    }
}
