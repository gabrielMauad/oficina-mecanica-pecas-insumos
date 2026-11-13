using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;

public sealed class BaixarEstoqueDoOrcamentoHandler
    : IRequestHandler<BaixarEstoqueDoOrcamentoCommand, Result<IReadOnlyList<PecaInsumo>>>
{
    private static readonly Error BaixaNaoRealizada = Error.Conflict(
        "PecaInsumo.BaixaDoOrcamentoNaoRealizada",
        "Não foi possível baixar o estoque do orçamento.");

    private readonly IPecaInsumoGateway _gateway;

    public BaixarEstoqueDoOrcamentoHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public Task<Result<IReadOnlyList<PecaInsumo>>> Handle(
        BaixarEstoqueDoOrcamentoCommand command,
        CancellationToken cancellationToken) =>
        AjusteDeEstoqueEmLote.Aplicar(
            _gateway,
            command.Itens,
            (peca, quantidade) => peca.Decrementar(quantidade),
            BaixaNaoRealizada,
            cancellationToken);
}
