namespace SharedKernel;

/// <summary>Erro de negócio tipado (código + mensagem), usado pelo <see cref="Result"/>/<see cref="Result{T}"/>.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
