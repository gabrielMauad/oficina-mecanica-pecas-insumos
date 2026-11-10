using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using SharedKernel.Application;
using SharedKernel.Application.Metrics;
using SharedKernel.Domain;

namespace SharedKernel.Application.Tests;

public class InMemoryIntegrationEventBusTests
{
    private sealed record EventoTeste(Guid EventId, DateTime OcorridoEm) : IIntegrationEvent;

    private sealed class HandlerQueFalha : IIntegrationEventHandler<EventoTeste>
    {
        public Task Handle(EventoTeste integrationEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Falha simulada no handler.");
    }

    private sealed class HandlerQueTemSucesso : IIntegrationEventHandler<EventoTeste>
    {
        public int Chamadas { get; private set; }

        public Task Handle(EventoTeste integrationEvent, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            return Task.CompletedTask;
        }
    }

    private static InMemoryIntegrationEventBus CriarBus(
        IntegracoesMetrics metrics,
        params IIntegrationEventHandler<EventoTeste>[] handlers)
    {
        var services = new ServiceCollection();
        foreach (var handler in handlers)
            services.AddSingleton<IIntegrationEventHandler<EventoTeste>>(handler);

        var provider = services.BuildServiceProvider();
        return new InMemoryIntegrationEventBus(provider, metrics);
    }

    [Fact(DisplayName = "Cenário feliz: handler executa com sucesso → nenhuma falha é contabilizada")]
    public async Task Publish_HandlerComSucesso_NaoRegistraFalha()
    {
        var metrics = new IntegracoesMetrics();
        using var collector = new MetricCollector<long>(metrics.Meter, IntegracoesMetrics.FalhasInstrumentName);
        var handler = new HandlerQueTemSucesso();
        var bus = CriarBus(metrics, handler);
        var evento = new EventoTeste(Guid.NewGuid(), DateTime.UtcNow);

        await bus.Publish(evento, CancellationToken.None);

        Assert.Equal(1, handler.Chamadas);
        Assert.Empty(collector.GetMeasurementSnapshot());
    }

    [Fact(DisplayName = "Erro: handler lança exceção → contabiliza falha com tags evento/handler e relança a exceção")]
    public async Task Publish_HandlerLancaExcecao_RegistraFalhaERelancaExcecao()
    {
        var metrics = new IntegracoesMetrics();
        using var collector = new MetricCollector<long>(metrics.Meter, IntegracoesMetrics.FalhasInstrumentName);
        var bus = CriarBus(metrics, new HandlerQueFalha());
        var evento = new EventoTeste(Guid.NewGuid(), DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.Publish(evento, CancellationToken.None));

        var medicao = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(1, medicao.Value);
        Assert.Equal(nameof(EventoTeste), medicao.Tags["evento"]);
        Assert.Equal(nameof(HandlerQueFalha), medicao.Tags["handler"]);
    }

    [Fact(DisplayName = "Erro: primeiro handler falha → segundo handler não é executado e exceção é propagada")]
    public async Task Publish_PrimeiroHandlerFalha_NaoExecutaSegundoHandler()
    {
        var metrics = new IntegracoesMetrics();
        using var collector = new MetricCollector<long>(metrics.Meter, IntegracoesMetrics.FalhasInstrumentName);
        var handlerComSucesso = new HandlerQueTemSucesso();
        var bus = CriarBus(metrics, new HandlerQueFalha(), handlerComSucesso);
        var evento = new EventoTeste(Guid.NewGuid(), DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.Publish(evento, CancellationToken.None));

        Assert.Equal(0, handlerComSucesso.Chamadas);
        Assert.Single(collector.GetMeasurementSnapshot());
    }
}
