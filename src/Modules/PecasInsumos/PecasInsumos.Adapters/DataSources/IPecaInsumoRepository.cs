using PecasInsumos.Adapters.DataSources.Records;

namespace PecasInsumos.Adapters.DataSources;

public interface IPecaInsumoRepository
{
    Task Adicionar(PecaInsumoRecord pecaInsumo, CancellationToken ct = default);
    Task<PecaInsumoRecord?> ObterPorId(Guid id, CancellationToken ct = default);
    Task<bool> ExistePorNome(string nome, CancellationToken ct = default);
    Task Atualizar(PecaInsumoRecord pecaInsumo, CancellationToken ct = default);
}
