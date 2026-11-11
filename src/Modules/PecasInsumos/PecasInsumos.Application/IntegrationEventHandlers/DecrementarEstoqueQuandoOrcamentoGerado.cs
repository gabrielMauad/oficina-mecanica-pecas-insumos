using MediatR;
using Microsoft.Extensions.Logging;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;
using SharedKernel.Application;

namespace PecasInsumos.Application.IntegrationEventHandlers;

public sealed class DecrementarEstoqueQuandoOrcamentoGerado : IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly IIntegrationEventBus _eventBus;
    private readonly ILogger<DecrementarEstoqueQuandoOrcamentoGerado> _logger;

    public DecrementarEstoqueQuandoOrcamentoGerado(
        ISender sender,
        IIntegrationEventBus eventBus,
        ILogger<DecrementarEstoqueQuandoOrcamentoGerado> logger)
    {
        _sender = sender;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(OrcamentoGeradoIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        var itens = integrationEvent.Pecas
            .Select(p => new ItemEstoque(p.PecaInsumoId, p.Quantidade))
            .ToList();

        var result = await _sender.Send(new BaixarEstoqueDoOrcamentoCommand(itens), ct);
        if (result.IsSuccess)
            return;

        _logger.LogWarning(
            "Baixa de estoque não realizada para o orçamento {OrcamentoId} da OS {OrdemServicoId}; nenhuma peça foi baixada: {Motivo}",
            integrationEvent.OrcamentoId,
            integrationEvent.OrdemServicoId,
            result.Error.Message);

        // Este handler roda no consumer, fora do TransactionBehavior do command — e o behavior não
        // publica eventos pendentes quando o command falha. Por isso a falha é publicada diretamente
        // no barramento. Nada foi persistido, então não há o que desfazer.
        await _eventBus.Publish(
            new EstoqueBaixaFalhouIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                integrationEvent.OrdemServicoId,
                integrationEvent.OrcamentoId,
                result.Error.Message),
            ct);
    }
}
