using Patients.Domain.Errors;
using SharedKernel;

namespace Patients.Domain.ValueObjects;

/// <summary>
/// CPF validado (dígito verificador, mod 11) — VO imutável, igualdade estrutural. Armazena só
/// os 11 dígitos (sem pontuação); formatação de exibição é responsabilidade de quem consome
/// (Presentation/Contracts), não do domínio.
/// </summary>
public sealed class Cpf : ValueObject
{
    public string Numero { get; }

    private Cpf(string numero) => Numero = numero;

    public static Result<Cpf> Create(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return Result.Failure<Cpf>(DomainErrors.CpfErrors.Obrigatorio);

        var digits = new string(cpf.Where(char.IsDigit).ToArray());

        if (digits.Length != 11)
            return Result.Failure<Cpf>(DomainErrors.CpfErrors.Invalido);

        // Sequências como "00000000000"/"11111111111" passam matematicamente no cálculo do DV
        // (são o caso degenerado da fórmula), mas nunca são CPFs reais — descartadas explicitamente.
        if (digits.Distinct().Count() == 1)
            return Result.Failure<Cpf>(DomainErrors.CpfErrors.Invalido);

        if (!DigitosVerificadoresSaoValidos(digits))
            return Result.Failure<Cpf>(DomainErrors.CpfErrors.Invalido);

        return Result.Success(new Cpf(digits));
    }

    private static bool DigitosVerificadoresSaoValidos(string digits)
    {
        var primeiroDv = CalcularDigitoVerificador(digits, 9);
        if (primeiroDv != digits[9] - '0')
            return false;

        var segundoDv = CalcularDigitoVerificador(digits, 10);
        return segundoDv == digits[10] - '0';
    }

    private static int CalcularDigitoVerificador(string digits, int quantidadeDigitosBase)
    {
        var multiplicador = quantidadeDigitosBase + 1;
        var soma = 0;

        for (var i = 0; i < quantidadeDigitosBase; i++)
        {
            soma += (digits[i] - '0') * multiplicador;
            multiplicador--;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Numero;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Numero;
    }
}
