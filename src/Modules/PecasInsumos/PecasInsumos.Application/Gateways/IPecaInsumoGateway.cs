using PecasInsumos.Domain;

namespace PecasInsumos.Application.Gateways;

public interface IPecaInsumoGateway
{
    Task Adicionar(PecaInsumo pecaInsumo, CancellationToken ct = default);
    Task<PecaInsumo?> ObterPorId(PecaInsumoId id, CancellationToken ct = default);
    Task<bool> ExistePorNome(string nome, CancellationToken ct = default);
    Task Atualizar(PecaInsumo pecaInsumo, CancellationToken ct = default);
}
