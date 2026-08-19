namespace Contracts.Abstractions;

/// <summary>
/// Marker sem membros. Sinaliza que um DTO é contrato público de módulo — o formato exposto
/// através da fronteira (ex: projeto `*.Contracts` consumido por outro módulo ou pela API),
/// e não um DTO interno de Application que só o próprio módulo enxerga. Não carrega
/// comportamento de propósito: é rótulo de tipo, não infraestrutura.
/// </summary>
public interface IModuleContract
{
}
