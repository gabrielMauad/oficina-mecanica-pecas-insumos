using MediatR;
using Microsoft.Extensions.Logging;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Application.Commands.DecrementarEstoque;
using SharedKernel.Application;

namespace PecasInsumos.Application.IntegrationEventHandlers;

public sealed class DecrementarEstoqueQuandoOrcamentoGerado : IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<DecrementarEstoqueQuandoOrcamentoGerado> _logger;

    public DecrementarEstoqueQuandoOrcamentoGerado(
        ISender sender,
        ILogger<DecrementarEstoqueQuandoOrcamentoGerado> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OrcamentoGeradoIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        foreach (var peca in integrationEvent.Pecas)
        {
            var result = await _sender.Send(new DecrementarEstoqueCommand(peca.PecaInsumoId, peca.Quantidade), ct);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Falha ao decrementar estoque da peça {PecaInsumoId} para o orçamento {OrcamentoId}: {Erro}. Peças restantes não foram processadas.",
                    peca.PecaInsumoId,
                    integrationEvent.OrcamentoId,
                    result.Error);
                return;
            }
        }
    }
}
