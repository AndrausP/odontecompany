using Records.Contracts;
using Records.Domain.Entities;

namespace Records.Application.Mapping;

public static class ProntuarioMappingExtensions
{
    public static ProntuarioDto ToDto(
        this Prontuario prontuario,
        IReadOnlyList<EvolucaoClinica> evolucoes,
        IReadOnlyList<AnexoMetadata> anexos)
        => new(
            prontuario.Id,
            prontuario.PacienteId,
            prontuario.Ativo,
            prontuario.Odontograma.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()),
            evolucoes.Select(e => e.ToDto()).ToList(),
            anexos.Select(a => a.ToDto()).ToList(),
            prontuario.CreatedAt);

    public static EvolucaoClinicaDto ToDto(this EvolucaoClinica evolucao)
        => new(
            evolucao.Id,
            evolucao.ProfissionalUserId,
            evolucao.TipoProcedimento.ToString(),
            evolucao.DescricaoClinica,
            evolucao.DataRegistro);

    public static AnexoDto ToDto(this AnexoMetadata anexo)
        => new(
            anexo.Id,
            anexo.NomeArquivo,
            anexo.TipoConteudo,
            anexo.TamanhoBytes,
            anexo.DataUpload);
}
