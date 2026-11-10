using System.Diagnostics.Metrics;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Infrastructure.Messaging.Consumers;
using SharedKernel.Application;
using SharedKernel.Application.Metrics;

namespace PecasInsumos.Infrastructure.Tests.Messaging;

public class OrcamentoConsumersTests
{
    private static OrcamentoGeradoIntegrationEvent NovoOrcamentoGerado() =>
        new(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            [new ItemPecaEventDto(Guid.NewGuid(), 2)]);

    private static OrcamentoRejeitadoIntegrationEvent NovoOrcamentoRejeitado() =>
        new(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            [new ItemPecaEventDto(Guid.NewGuid(), 3)]);

    private static ServiceProvider BuildProvider(
        Mock<IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>> geradoHandler,
        Mock<IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent>> rejeitadoHandler)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IntegracoesMetrics>();
        services.AddScoped(_ => geradoHandler.Object);
        services.AddScoped(_ => rejeitadoHandler.Object);
        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrcamentoGeradoConsumer>();
            x.AddConsumer<OrcamentoRejeitadoConsumer>();
        });
        return services.BuildServiceProvider(true);
    }

    [Fact(DisplayName = "OrcamentoGerado — consumer delega ao handler da Application com a mensagem recebida")]
    public async Task OrcamentoGerado_DeveDelegarAoHandler()
    {
        // Arrange
        var geradoHandler = new Mock<IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>>();
        var rejeitadoHandler = new Mock<IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent>>();
        await using var provider = BuildProvider(geradoHandler, rejeitadoHandler);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var evento = NovoOrcamentoGerado();

        // Act
        await harness.Bus.Publish(evento);

        // Assert
        var consumer = harness.GetConsumerHarness<OrcamentoGeradoConsumer>();
        Assert.True(await consumer.Consumed.Any<OrcamentoGeradoIntegrationEvent>());
        geradoHandler.Verify(h => h.Handle(
            It.Is<OrcamentoGeradoIntegrationEvent>(e => e.EventId == evento.EventId && e.Pecas.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        rejeitadoHandler.Verify(h => h.Handle(
            It.IsAny<OrcamentoRejeitadoIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "OrcamentoRejeitado — consumer delega ao handler da Application com a mensagem recebida")]
    public async Task OrcamentoRejeitado_DeveDelegarAoHandler()
    {
        // Arrange
        var geradoHandler = new Mock<IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>>();
        var rejeitadoHandler = new Mock<IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent>>();
        await using var provider = BuildProvider(geradoHandler, rejeitadoHandler);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var evento = NovoOrcamentoRejeitado();

        // Act
        await harness.Bus.Publish(evento);

        // Assert
        var consumer = harness.GetConsumerHarness<OrcamentoRejeitadoConsumer>();
        Assert.True(await consumer.Consumed.Any<OrcamentoRejeitadoIntegrationEvent>());
        rejeitadoHandler.Verify(h => h.Handle(
            It.Is<OrcamentoRejeitadoIntegrationEvent>(e => e.EventId == evento.EventId && e.Pecas.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        geradoHandler.Verify(h => h.Handle(
            It.IsAny<OrcamentoGeradoIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "OrcamentoGerado — falha do handler é propagada ao MassTransit (retry) e registrada na métrica")]
    public async Task OrcamentoGerado_DevePropagarFalhaDoHandler_ERegistrarMetrica()
    {
        // Arrange
        var handler = new Mock<IIntegrationEventHandler<OrcamentoGeradoIntegrationEvent>>();
        handler.Setup(h => h.Handle(It.IsAny<OrcamentoGeradoIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha técnica"));
        using var metrics = new IntegracoesMetrics();

        var falhas = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == metrics.Meter) l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
                if (tag.Key == "evento")
                    falhas.Add(tag.Value?.ToString() ?? string.Empty);
        });
        listener.Start();

        var consumer = new OrcamentoGeradoConsumer(handler.Object, metrics);
        var context = new Mock<ConsumeContext<OrcamentoGeradoIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(NovoOrcamentoGerado());
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context.Object));

        // Assert
        Assert.Equal("falha técnica", ex.Message);
        Assert.Equal([nameof(OrcamentoGeradoIntegrationEvent)], falhas);
    }

    [Fact(DisplayName = "OrcamentoRejeitado — falha do handler é propagada ao MassTransit (retry)")]
    public async Task OrcamentoRejeitado_DevePropagarFalhaDoHandler()
    {
        // Arrange
        var handler = new Mock<IIntegrationEventHandler<OrcamentoRejeitadoIntegrationEvent>>();
        handler.Setup(h => h.Handle(It.IsAny<OrcamentoRejeitadoIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha técnica"));
        using var metrics = new IntegracoesMetrics();
        var consumer = new OrcamentoRejeitadoConsumer(handler.Object, metrics);
        var context = new Mock<ConsumeContext<OrcamentoRejeitadoIntegrationEvent>>();
        context.SetupGet(c => c.Message).Returns(NovoOrcamentoRejeitado());
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context.Object));
    }
}
