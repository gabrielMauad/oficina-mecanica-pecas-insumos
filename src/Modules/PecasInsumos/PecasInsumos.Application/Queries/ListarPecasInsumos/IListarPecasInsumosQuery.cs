namespace PecasInsumos.Application.Queries.ListarPecasInsumos;

public interface IListarPecasInsumosQuery
{
    Task<List<PecaInsumoListItem>> Listar(CancellationToken ct = default);
}

