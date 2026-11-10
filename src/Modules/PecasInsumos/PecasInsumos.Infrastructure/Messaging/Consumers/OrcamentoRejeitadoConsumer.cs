using MassTransit;
using OficinaMecanica.Contracts.IntegrationEvents;
using SharedKernel.Application;
using SharedKernel.Application.Metrics;

namespace PecasInsumos.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer MassTransit de <see cref="OrcamentoRejeitadoIntegrationEvent"/>: apenas delega ao handler
/// da Application (<c>IncrementarEstoqueQuandoOrcamentoRejeitado</c>).
/// </summary>
public sealed class OrcamentoRejeitadoConsumer : IConsumer<OrcamentoRejeitadoIntegrationEvent>
{
    private readonly IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent> _handler;
    private readonly IntegracoesMetrics _metrics;

    public OrcamentoRejeitadoConsumer(
        IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent> handler,
        IntegracoesMetrics metrics)
    {
        _handler = handler;
        _metrics = metrics;
    }

    public async Task Consume(ConsumeContext<OrcamentoRejeitadoIntegrationEvent> context)
    {
        try
        {
            await _handler.Handle(context.Message, context.CancellationToken);
        }
        catch (Exception)
        {
            _metrics.RegistrarFalha(nameof(OrcamentoRejeitadoIntegrationEvent), _handler.GetType().Name);
            throw;
        }
    }
}
