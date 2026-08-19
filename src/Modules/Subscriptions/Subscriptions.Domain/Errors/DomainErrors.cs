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
    }
}
