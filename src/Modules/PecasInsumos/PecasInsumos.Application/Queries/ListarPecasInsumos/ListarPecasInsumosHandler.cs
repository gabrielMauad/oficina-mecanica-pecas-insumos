using MediatR;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.ListarPecasInsumos;

public sealed class ListarPecasInsumosHandler
    : IRequestHandler<ListarPecasInsumosQuery, Result<List<PecaInsumoListItem>>>
{
    private readonly IListarPecasInsumosQuery _query;
    public ListarPecasInsumosHandler(IListarPecasInsumosQuery query) => _query = query;

    public async Task<Result<List<PecaInsumoListItem>>> Handle(
        ListarPecasInsumosQuery request,
        CancellationToken cancellationToken)
    {
        return await _query.Listar(cancellationToken);
    }
}


