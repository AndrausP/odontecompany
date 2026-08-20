using SharedKernel;

namespace Subscriptions.Domain.Errors;

public static class DomainErrors
{
    public static class Subscription
    {
        public static readonly Error OrganizationInvalido = new("Subscription.OrganizationInvalido", "Organization inválido.");
        public static readonly Error JaExiste = new("Subscription.JaExiste", "Esta organização já tem um plano ativo.");
        public static readonly Error NaoEncontrada = new("Subscription.NaoEncontrada", "Nenhum plano ativo pra esta organização.");
        public static readonly Error LimiteDeFiliaisAtingido = new(
            "Subscription.LimiteDeFiliaisAtingido",
            "O plano atual não permite criar mais unidades. Faça upgrade pra continuar.");

        /// <summary>Auditoria pré-venda — dívida técnica nomeada na task 039, fechada aqui: downgrade não podia deixar a organização com mais filiais ativas do que o plano novo permite.</summary>
        public static readonly Error DowngradeExcedeFiliaisAtivas = new(
            "Subscription.DowngradeExcedeFiliaisAtivas",
            "Esta organização tem mais unidades ativas do que o novo plano permite. Desative unidades antes de fazer downgrade.");
    }
}
