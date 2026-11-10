using MassTransit;
using OficinaMecanica.Contracts.IntegrationEvents;
using SharedKernel.Application;
using SharedKernel.Application.Metrics;

namespace PecasInsumos.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer MassTransit de <see cref="OrcamentoGeradoIntegrationEvent"/>: apenas delega ao handler
/// da Application (<c>DecrementarEstoqueQuandoOrcamentoGerado</c>).
/// </summary>
public sealed class OrcamentoGeradoConsumer : IConsumer<OrcamentoGeradoIntegrationEvent>
{
    private readonly IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent> _handler;
    private readonly IntegracoesMetrics _metrics;

    public OrcamentoGeradoConsumer(
        IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent> handler,
        IntegracoesMetrics metrics)
    {
        _handler = handler;
        _metrics = metrics;
    }

    public async Task Consume(ConsumeContext<OrcamentoGeradoIntegrationEvent> context)
    {
        try
        {
            await _handler.Handle(context.Message, context.CancellationToken);
        }
        catch (Exception)
        {
            _metrics.RegistrarFalha(nameof(OrcamentoGeradoIntegrationEvent), _handler.GetType().Name);
            throw;
        }
    }
}
