using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.ObterPecaInsumoPorId;

public sealed class ObterPecaInsumoPorIdHandler : IRequestHandler<ObterPecaInsumoPorIdQuery, Result<PecaInsumo>>
{
    private readonly IPecaInsumoGateway _gateway;

    public ObterPecaInsumoPorIdHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public async Task<Result<PecaInsumo>> Handle(ObterPecaInsumoPorIdQuery request, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(request.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _gateway.ObterPorId(pecaInsumoId, cancellationToken);
        if (pecaInsumo is null)
            return PecaInsumoErrors.NaoEncontrada;
        return pecaInsumo;
    }
}
