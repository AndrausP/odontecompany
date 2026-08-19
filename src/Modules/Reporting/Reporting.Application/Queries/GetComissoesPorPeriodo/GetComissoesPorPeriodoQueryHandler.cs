using Billing.Contracts;
using Identity.Contracts;
using MediatR;
using Reporting.Contracts;
using Scheduling.Contracts;
using SharedKernel;
using Tenancy.Contracts;

namespace Reporting.Application.Queries.GetComissoesPorPeriodo;

/// <summary>
/// Composição em memória de 4 portas de leitura (task 022), sem NENHUM join cruzado entre
/// módulos: <see cref="IComissaoSummaryProvider"/> (comissão por profissional, regime de caixa),
/// <see cref="IProfissionalLookup"/> (nome/branch/ativo), <see cref="IMembershipLookup"/> (role) e
/// <see cref="IBranchLookup"/> (nome da filial). Filtros de <c>BranchId</c>/<c>Classe</c>/
/// <c>ProfissionalId</c> são aplicados em memória DEPOIS da junção — o volume por organization é
/// dezenas de profissionais, não milhares (R3, docs/tasks/023-*.md — dívida nomeada, mesma classe
/// da 008).
/// </summary>
public sealed class GetComissoesPorPeriodoQueryHandler : IRequestHandler<GetComissoesPorPeriodoQuery, Result<ComissaoResumoDto>>
{
    private const string BucketNaoAtribuidoNome = "Não atribuído";

    private readonly IComissaoSummaryProvider _comissaoSummaryProvider;
    private readonly IProfissionalLookup _profissionalLookup;
    private readonly IMembershipLookup _membershipLookup;
    private readonly IBranchLookup _branchLookup;

    public GetComissoesPorPeriodoQueryHandler(
        IComissaoSummaryProvider comissaoSummaryProvider,
        IProfissionalLookup profissionalLookup,
        IMembershipLookup membershipLookup,
        IBranchLookup branchLookup)
    {
        _comissaoSummaryProvider = comissaoSummaryProvider;
        _profissionalLookup = profissionalLookup;
        _membershipLookup = membershipLookup;
        _branchLookup = branchLookup;
    }

    public async Task<Result<ComissaoResumoDto>> Handle(GetComissoesPorPeriodoQuery request, CancellationToken cancellationToken)
    {
        // As 4 chamadas são independentes entre si — mesmo racional de paralelismo do
        // GetDashboardResumoQueryHandler.
        var comissoesTask = _comissaoSummaryProvider.ObterComissoesAsync(request.OrganizationId, request.DataInicio, request.DataFim, cancellationToken);
        var profissionaisTask = _profissionalLookup.ListarPorOrganizationAsync(request.OrganizationId, cancellationToken);
        var membershipsTask = _membershipLookup.ListarPorOrganizationAsync(request.OrganizationId, cancellationToken);
        var branchNomesTask = _branchLookup.ListarNomesAsync(request.OrganizationId, cancellationToken);

        await Task.WhenAll(comissoesTask, profissionaisTask, membershipsTask, branchNomesTask);

        var comissoes = await comissoesTask;
        var profissionais = await profissionaisTask;
        var memberships = await membershipsTask;
        var branchNomes = await branchNomesTask;

        var profissionalPorId = profissionais.ToDictionary(p => p.Id);
        // UserId é único dentro do organization retornado por ListarPorOrganizationAsync.
        var membershipPorUserId = memberships
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var diasNoPeriodo = CalcularDiasNoPeriodo(request.DataInicio, request.DataFim);

        var linhas = comissoes
            .Select(c => MontarLinha(c, profissionalPorId, membershipPorUserId, branchNomes, diasNoPeriodo))
            .ToList();

        if (request.ProfissionalId.HasValue)
        {
            linhas = linhas.Where(l => l.ProfissionalId == request.ProfissionalId.Value).ToList();

            // Profissional existe mas não teve comissão no período (ou não está vinculado a
            // nenhum profissional real, ex: Dentista sem Profissional cadastrado — controller
            // manda um Id que nunca vai bater aqui) → 200 zerado, nunca erro, nunca 404.
            if (linhas.Count == 0 && profissionalPorId.TryGetValue(request.ProfissionalId.Value, out var profissionalAlvo))
                linhas = [MontarLinhaZerada(profissionalAlvo, membershipPorUserId, branchNomes, diasNoPeriodo)];
        }

        if (request.BranchId.HasValue)
            linhas = linhas.Where(l => l.BranchId == request.BranchId.Value).ToList();

        if (!string.IsNullOrWhiteSpace(request.Classe))
            linhas = linhas.Where(l => string.Equals(l.Classe, request.Classe, StringComparison.OrdinalIgnoreCase)).ToList();

        var valorComissaoTotal = linhas.Sum(l => l.ValorComissao);
        var valorPagoTotal = linhas.Sum(l => l.ValorPago);
        var quantidadeFaturas = linhas.Sum(l => l.QuantidadeFaturas);

        var dto = new ComissaoResumoDto(
            request.OrganizationId,
            request.DataInicio,
            request.DataFim,
            diasNoPeriodo,
            valorComissaoTotal,
            valorPagoTotal,
            quantidadeFaturas,
            Arredondar(valorComissaoTotal / diasNoPeriodo),
            Arredondar(valorPagoTotal / diasNoPeriodo),
            linhas);

        return Result.Success(dto);
    }

    private static ComissaoLinhaDto MontarLinha(
        ComissaoPorProfissionalDto comissao,
        IReadOnlyDictionary<Guid, ProfissionalResumoDto> profissionalPorId,
        IReadOnlyDictionary<Guid, MembershipResumoDto> membershipPorUserId,
        IReadOnlyDictionary<Guid, string> branchNomes,
        int diasNoPeriodo)
    {
        var profissional = comissao.ProfissionalId.HasValue
            ? profissionalPorId.GetValueOrDefault(comissao.ProfissionalId.Value)
            : null;

        var (nome, branchId, branchNome, classe) = ResolverDadosDoProfissional(profissional, membershipPorUserId, branchNomes);

        return new ComissaoLinhaDto(
            comissao.ProfissionalId,
            nome,
            branchId,
            branchNome,
            classe,
            comissao.ValorPagoNoPeriodo,
            comissao.ValorComissao,
            comissao.QuantidadeFaturas,
            Arredondar(comissao.ValorComissao / diasNoPeriodo));
    }

    private static ComissaoLinhaDto MontarLinhaZerada(
        ProfissionalResumoDto profissional,
        IReadOnlyDictionary<Guid, MembershipResumoDto> membershipPorUserId,
        IReadOnlyDictionary<Guid, string> branchNomes,
        int diasNoPeriodo)
    {
        var (nome, branchId, branchNome, classe) = ResolverDadosDoProfissional(profissional, membershipPorUserId, branchNomes);
        _ = diasNoPeriodo; // média diária de zero é zero, sem precisar dividir.
        return new ComissaoLinhaDto(profissional.Id, nome, branchId, branchNome, classe, 0m, 0m, 0, 0m);
    }

    private static (string Nome, Guid? BranchId, string? BranchNome, string? Classe) ResolverDadosDoProfissional(
        ProfissionalResumoDto? profissional,
        IReadOnlyDictionary<Guid, MembershipResumoDto> membershipPorUserId,
        IReadOnlyDictionary<Guid, string> branchNomes)
    {
        if (profissional is null)
            return (BucketNaoAtribuidoNome, null, null, null);

        var branchNome = profissional.BranchId.HasValue
            ? branchNomes.GetValueOrDefault(profissional.BranchId.Value)
            : null;

        var classe = profissional.UserId.HasValue && membershipPorUserId.TryGetValue(profissional.UserId.Value, out var membership)
            ? membership.Role
            : null;

        return (profissional.Nome, profissional.BranchId, branchNome, classe);
    }

    // Período inclusivo nas duas pontas — mínimo 1 dia, nunca divide por zero.
    private static int CalcularDiasNoPeriodo(DateTime dataInicio, DateTime dataFim)
        => Math.Max((dataFim.Date - dataInicio.Date).Days + 1, 1);

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.ToEven);
}
