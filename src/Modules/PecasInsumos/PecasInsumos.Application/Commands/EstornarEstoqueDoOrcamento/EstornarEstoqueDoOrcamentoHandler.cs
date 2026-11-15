using MediatR;
using PecasInsumos.Application.Gateways;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;

public sealed class EstornarEstoqueDoOrcamentoHandler
    : IRequestHandler<EstornarEstoqueDoOrcamentoCommand, Result<DesfechoMovimentacaoDoOrcamento>>
{
    private static readonly Error EstornoNaoRealizado = Error.Conflict(
        "PecaInsumo.EstornoDoOrcamentoNaoRealizado",
        "Não foi possível estornar o estoque do orçamento.");

    private readonly IPecaInsumoGateway _gateway;

    public EstornarEstoqueDoOrcamentoHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public Task<Result<DesfechoMovimentacaoDoOrcamento>> Handle(
        EstornarEstoqueDoOrcamentoCommand command,
        CancellationToken cancellationToken) =>
        AjusteDeEstoqueEmLote.Aplicar(
            _gateway,
            command.OrcamentoId,
            TipoMovimentacaoOrcamento.Estorno,
            command.Itens,
            EstornoNaoRealizado,
            cancellationToken);
}
