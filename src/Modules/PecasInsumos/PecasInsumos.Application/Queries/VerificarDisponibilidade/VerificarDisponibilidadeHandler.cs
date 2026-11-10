using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.VerificarDisponibilidade;

public sealed class VerificarDisponibilidadeHandler
    : IRequestHandler<VerificarDisponibilidadeQuery, Result<DisponibilidadePecaInsumo>>
{
    private readonly IPecaInsumoGateway _gateway;

    public VerificarDisponibilidadeHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public async Task<Result<DisponibilidadePecaInsumo>> Handle(
        VerificarDisponibilidadeQuery request,
        CancellationToken cancellationToken)
    {
        PecaInsumo? pecaInsumo = await _gateway.ObterPorId(new PecaInsumoId(request.PecaInsumoId), cancellationToken);
        if (pecaInsumo is null)
            return PecaInsumoErrors.NaoEncontrada;

        return new DisponibilidadePecaInsumo(
            pecaInsumo.QuantidadeEmEstoque >= request.Quantidade,
            pecaInsumo.PrecoUnitario.Valor);
    }
}
