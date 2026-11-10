using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.AtualizarPecaInsumo;

public sealed class AtualizarPecaInsumoHandler : IRequestHandler<AtualizarPecaInsumoCommand, Result<PecaInsumo>>
{
    private readonly IPecaInsumoGateway _gateway;

    public AtualizarPecaInsumoHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public async Task<Result<PecaInsumo>> Handle(AtualizarPecaInsumoCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _gateway.ObterPorId(pecaInsumoId, cancellationToken);
        if (pecaInsumo is null)
            return PecaInsumoErrors.NaoEncontrada;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;

        bool hasChanges = false;
        if (command.PrecoUnitario is not null && command.PrecoUnitario != pecaInsumo.PrecoUnitario.Valor)
        {
            Result<PecaInsumo> atualizarPrecoResult = pecaInsumo.AtualizarPrecoUnitario(command.PrecoUnitario.Value);
            if (atualizarPrecoResult.IsFailure)
                return atualizarPrecoResult.Error;
            hasChanges = true;
        }
        if (command.Descricao is not null && command.Descricao != pecaInsumo.Descricao)
        {
            Result<PecaInsumo> atualizarDescricaoResult = pecaInsumo.AtualizarDescricao(command.Descricao);
            if (atualizarDescricaoResult.IsFailure)
                return atualizarDescricaoResult.Error;
            hasChanges = true;
        }

        if (hasChanges)
            await _gateway.Atualizar(pecaInsumo, cancellationToken);

        return pecaInsumo;
    }
}
