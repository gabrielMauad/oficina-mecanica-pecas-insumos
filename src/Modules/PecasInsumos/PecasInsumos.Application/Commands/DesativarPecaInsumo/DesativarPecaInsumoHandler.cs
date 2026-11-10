using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.DesativarPecaInsumo;

public sealed class DesativarPecaInsumoHandler : IRequestHandler<DesativarPecaInsumoCommand, Result<PecaInsumo>>
{
    private readonly IPecaInsumoGateway _gateway;

    public DesativarPecaInsumoHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public async Task<Result<PecaInsumo>> Handle(DesativarPecaInsumoCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _gateway.ObterPorId(pecaInsumoId, cancellationToken);
        if (pecaInsumo is null)
            return PecaInsumoErrors.NaoEncontrada;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;
        pecaInsumo.Desativar();
        await _gateway.Atualizar(pecaInsumo, cancellationToken);

        return pecaInsumo;
    }
}
