namespace Contracts.Abstractions.Pagination;

/// <summary>
/// Query params padrão de paginação. Página mínima é 1 (nunca 0 ou negativo — corrigido
/// silenciosamente pro padrão em vez de estourar erro de validação por causa de um int
/// mal-intencionado ou um front-end com bug de índice). PageSize é limitado pra evitar
/// listagem de 1 milhão de linhas porque alguém mandou `pageSize=999999` na query string.
/// </summary>
public sealed record PageRequest : IHasPagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private readonly int _page = 1;
    private readonly int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
