using MediatR;
using Microsoft.Extensions.Logging;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;
using SharedKernel.Application;

namespace PecasInsumos.Application.IntegrationEventHandlers;

public sealed class IncrementarEstoqueQuandoOrcamentoRejeitado : IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<IncrementarEstoqueQuandoOrcamentoRejeitado> _logger;

    public IncrementarEstoqueQuandoOrcamentoRejeitado(
        ISender sender,
        ILogger<IncrementarEstoqueQuandoOrcamentoRejeitado> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OrcamentoRejeitadoIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        var itens = integrationEvent.Pecas
            .Select(p => new ItemEstoque(p.PecaInsumoId, p.Quantidade))
            .ToList();

        var result = await _sender.Send(new EstornarEstoqueDoOrcamentoCommand(integrationEvent.OrcamentoId, itens), ct);
        if (result.IsFailure)
        {
            _logger.LogError(
                "Falha ao estornar o estoque do orçamento rejeitado {OrcamentoId} da OS {OrdemServicoId}; nenhuma peça foi estornada: {Erro}",
                integrationEvent.OrcamentoId,
                integrationEvent.OrdemServicoId,
                result.Error.Message);
            return;
        }

        if (result.Value == DesfechoMovimentacaoDoOrcamento.JaProcessada)
        {
            _logger.LogInformation(
                "Estorno de estoque do orçamento {OrcamentoId} da OS {OrdemServicoId} já havia sido realizado; evento repetido ignorado",
                integrationEvent.OrcamentoId,
                integrationEvent.OrdemServicoId);
        }
    }
}
