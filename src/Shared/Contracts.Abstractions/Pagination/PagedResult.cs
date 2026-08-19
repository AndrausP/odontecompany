namespace Contracts.Abstractions.Pagination;

/// <summary>
/// Resultado paginado genérico — retorno padrão de qualquer listagem exposta entre módulos
/// (Contracts) ou pela API. Não é específico de domínio nenhum: qualquer módulo (Patients,
/// Scheduling, Identity...) devolve isto pra listagens, sem reinventar paginação cada um do
/// seu jeito.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) =>
        new()
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
}
