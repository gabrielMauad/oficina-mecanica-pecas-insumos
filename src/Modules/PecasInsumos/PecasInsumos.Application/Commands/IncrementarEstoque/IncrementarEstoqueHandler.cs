using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.IncrementarEstoque;

public sealed class IncrementarEstoqueHandler : IRequestHandler<IncrementarEstoqueCommand, Result<PecaInsumo>>
{
    private readonly IPecaInsumoGateway _gateway;

    public IncrementarEstoqueHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public async Task<Result<PecaInsumo>> Handle(IncrementarEstoqueCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _gateway.ObterPorId(pecaInsumoId, cancellationToken);

        if (pecaInsumo == null)
            return PecaInsumoErrors.NaoEncontrada;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;

        Result<PecaInsumo> pecaInsumoResult = pecaInsumo.Incrementar(command.Quantidade);
        if (pecaInsumoResult.IsFailure)
            return pecaInsumoResult.Error;

        pecaInsumo = pecaInsumoResult.Value;

        await _gateway.Atualizar(pecaInsumo, cancellationToken);

        return pecaInsumo;
    }
}
