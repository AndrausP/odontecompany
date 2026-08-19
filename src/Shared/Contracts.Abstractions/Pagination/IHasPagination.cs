namespace Contracts.Abstractions.Pagination;

/// <summary>
/// Marca uma query/input de Application que carrega parâmetros de paginação. Existe pra
/// permitir comportamento genérico (ex: pipeline behavior de validação/clamp de paginação)
/// sem cada módulo reinventar `Page`/`PageSize` com nomes diferentes.
/// </summary>
public interface IHasPagination
{
    int Page { get; }
    int PageSize { get; }
}
