namespace SharedKernel;

/// <summary>
/// Resultado de uma operação de negócio — sucesso ou falha com <see cref="Error"/> tipado.
/// Nunca lance exception pra erro de negócio esperado (senha errada, email duplicado etc);
/// use este tipo. DTOs nunca são null: ou vêm dentro de um Result de sucesso, ou nem existem.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Resultado de sucesso não pode carregar um erro.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Resultado de falha precisa de um erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>Variante de <see cref="Result"/> que carrega um valor de sucesso do tipo <typeparamref name="T"/>.</summary>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar Value de um Result de falha.");

    protected internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}
