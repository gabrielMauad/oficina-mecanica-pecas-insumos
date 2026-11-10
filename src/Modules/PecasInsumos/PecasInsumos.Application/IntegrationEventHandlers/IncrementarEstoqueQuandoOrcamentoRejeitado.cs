using MediatR;
using Microsoft.Extensions.Logging;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Application.Commands.IncrementarEstoque;
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
        foreach (var peca in integrationEvent.Pecas)
        {
            var result = await _sender.Send(new IncrementarEstoqueCommand(peca.PecaInsumoId, peca.Quantidade), ct);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Falha ao estornar estoque da peça {PecaInsumoId} para o orçamento rejeitado {OrcamentoId}: {Erro}. Peças restantes não foram processadas.",
                    peca.PecaInsumoId,
                    integrationEvent.OrcamentoId,
                    result.Error);
                return;
            }
        }
    }
}
