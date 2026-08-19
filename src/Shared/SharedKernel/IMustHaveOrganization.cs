namespace SharedKernel;

/// <summary>
/// Marca uma entidade como pertencente a um organization. Fica no SharedKernel (não em
/// Infrastructure.Common) de propósito: é um conceito de domínio ("essa entidade é
/// escopada por organization"), não um detalhe de EF Core — e Domain só pode depender do
/// SharedKernel, nunca de Infrastructure. Quem sabe o que fazer com isso (filtro global
/// de query) é o Infrastructure.Common.
/// </summary>
public interface IMustHaveOrganization
{
    Guid OrganizationId { get; }
}
