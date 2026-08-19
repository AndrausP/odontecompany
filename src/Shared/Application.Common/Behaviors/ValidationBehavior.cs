
using System.Reflection;
using FluentValidation;
using MediatR;
using SharedKernel;

namespace Application.Common.Behaviors;

/// <summary>
/// Pipeline Behavior do MediatR: roda todos os FluentValidation validators registrados pro
/// request antes do handler. Se falhar, devolve Result/Result&lt;T&gt; de falha — NUNCA lança
/// ValidationException pra cima, porque todo Use Case (de qualquer módulo) responde em Result.
/// Único caso em que lança: se TResponse não for Result nem Result&lt;T&gt;.
///
/// Vive em Application.Common (Shared), não dentro de um módulo específico, de propósito:
/// é registrado UMA vez no host como IPipelineBehavior&lt;,&gt; genérico e aberto, cobrindo
/// TODO módulo que passa pelo mesmo pipeline MediatR. Se cada módulo (Identity, Patients, ...)
/// tivesse sua própria cópia, o host registraria N implementações do mesmo open generic — o
/// MediatR executaria a validação N vezes por request (uma por behavior registrado), mesmo bug
/// de duplicação que o padrão de organization query filter evita (ver Infrastructure.Common).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorMessage = string.Join(" | ", failures.Select(f => f.ErrorMessage));
        var error = new Error("Validation.Failure", errorMessage);

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Error) }, null)!
                .MakeGenericMethod(valueType);

            return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
        }

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        throw new ValidationException(failures);
    }
}
