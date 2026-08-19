namespace Subscriptions.Domain.Enums;

/// <summary>3 planos vendidos na landing (sprint-11) — sem tabela/seed de "Plan" configurável no
/// banco de propósito: são conteúdo fixo do produto, não algo que um Admin cadastra. Se um plano
/// novo entrar no catálogo, isso é mudança de código (<see cref="Subscriptions.Domain.PlanCatalog"/>),
/// não dado.</summary>
public enum PlanTier
{
    Starter = 1,
    Profissional = 2,
    Rede = 3,
}
