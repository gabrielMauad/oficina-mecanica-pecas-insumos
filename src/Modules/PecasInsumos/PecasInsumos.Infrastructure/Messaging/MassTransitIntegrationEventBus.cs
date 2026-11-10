using MassTransit;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace PecasInsumos.Infrastructure.Messaging;

/// <summary>
/// Adapter de <see cref="IIntegrationEventBus"/> sobre o MassTransit: publica o evento de integração
/// no tópico do seu tipo (SNS) para que os serviços assinantes o recebam em suas filas (SQS).
/// </summary>
public sealed class MassTransitIntegrationEventBus : IIntegrationEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventBus(IPublishEndpoint publishEndpoint) =>
        _publishEndpoint = publishEndpoint;

    public Task Publish<T>(T evento, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent =>
        _publishEndpoint.Publish(evento, cancellationToken);
}
