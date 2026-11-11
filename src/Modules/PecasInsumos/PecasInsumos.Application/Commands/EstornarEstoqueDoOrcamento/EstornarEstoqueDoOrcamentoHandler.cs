using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;

public sealed class EstornarEstoqueDoOrcamentoHandler
    : IRequestHandler<EstornarEstoqueDoOrcamentoCommand, Result<IReadOnlyList<PecaInsumo>>>
{
    private static readonly Error EstornoNaoRealizado = Error.Conflict(
        "PecaInsumo.EstornoDoOrcamentoNaoRealizado",
        "Não foi possível estornar o estoque do orçamento.");

    private readonly IPecaInsumoGateway _gateway;

    public EstornarEstoqueDoOrcamentoHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public Task<Result<IReadOnlyList<PecaInsumo>>> Handle(
        EstornarEstoqueDoOrcamentoCommand command,
        CancellationToken cancellationToken) =>
        AjusteDeEstoqueEmLote.Aplicar(
            _gateway,
            command.Itens,
            (peca, quantidade) => peca.Incrementar(quantidade),
            EstornoNaoRealizado,
            cancellationToken);
}
