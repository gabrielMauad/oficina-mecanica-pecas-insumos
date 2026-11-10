using PecasInsumos.Contracts.Dtos;

namespace PecasInsumos.Contracts.Queries;

public interface IPecaInsumoQuery
{
    Task<PecaInsumoResumoDto?> ObterPorId(Guid id, CancellationToken ct = default);
}
