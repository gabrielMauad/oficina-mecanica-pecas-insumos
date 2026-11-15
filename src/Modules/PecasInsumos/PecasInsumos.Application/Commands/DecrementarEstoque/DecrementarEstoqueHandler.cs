using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.DecrementarEstoque;

public sealed class DecrementarEstoqueHandler : IRequestHandler<DecrementarEstoqueCommand, Result<PecaInsumo>>
{
    private readonly IPecaInsumoGateway _gateway;

    public DecrementarEstoqueHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public async Task<Result<PecaInsumo>> Handle(DecrementarEstoqueCommand command, CancellationToken cancellationToken)
    {
        PecaInsumoId pecaInsumoId = new(command.PecaInsumoId);
        PecaInsumo? pecaInsumo = await _gateway.ObterPorId(pecaInsumoId, cancellationToken);

        if (pecaInsumo == null)
            return PecaInsumoErrors.NaoEncontrada;
        if (!pecaInsumo.Ativo)
            return PecaInsumoErrors.JaDesativado;

        // Valida a regra de domínio (quantidade positiva, saldo suficiente) sobre o saldo lido...
        Result<PecaInsumo> pecaInsumoResult = pecaInsumo.Decrementar(command.Quantidade);
        if (pecaInsumoResult.IsFailure)
            return pecaInsumoResult.Error;

        // ...mas quem altera o saldo é a operação atômica no banco, que reaplica a mesma condição sobre o
        // saldo corrente. Se outra movimentação consumiu o saldo no intervalo, nada é gravado.
        if (!await _gateway.DecrementarEstoque(pecaInsumoId, command.Quantidade, cancellationToken))
            return PecaInsumoErrors.EstoqueAlteradoPorOutraOperacao;

        return await _gateway.ObterPorId(pecaInsumoId, cancellationToken) ?? pecaInsumoResult.Value;
    }
}
