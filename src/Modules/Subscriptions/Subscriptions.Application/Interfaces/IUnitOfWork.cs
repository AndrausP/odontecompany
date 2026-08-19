namespace Subscriptions.Application.Interfaces;

/// <summary>UoW simples — o DbContext do módulo já é a branch de trabalho, isso só formaliza a porta.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
