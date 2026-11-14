using System.Text.Json;
using OficinaMecanica.Contracts.IntegrationEvents;

namespace PecasInsumos.Infrastructure.Tests.Contracts;

/// <summary>
/// Os eventos de integração trafegam entre serviços como JSON. Estes testes fixam o contrato de
/// serialização: o que um serviço publica precisa ser reconstruído sem perdas por quem consome.
/// </summary>
public class EventosDeIntegracaoContratoTests
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    private static T IdaEVolta<T>(T evento)
    {
        var json = JsonSerializer.Serialize(evento, Opcoes);
        return JsonSerializer.Deserialize<T>(json, Opcoes)!;
    }

    [Fact(DisplayName = "OrcamentoAprovado — serializa e desserializa sem perder campos")]
    public void OrcamentoAprovado_IdaEVolta()
    {
        var evento = new OrcamentoAprovadoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 349.90m);

        Assert.Equal(evento, IdaEVolta(evento));
    }

    [Fact(DisplayName = "PagamentoAprovado — serializa e desserializa sem perder campos")]
    public void PagamentoAprovado_IdaEVolta()
    {
        var evento = new PagamentoAprovadoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(evento, IdaEVolta(evento));
    }

    [Fact(DisplayName = "PagamentoRecusado — serializa e desserializa sem perder o motivo")]
    public void PagamentoRecusado_IdaEVolta()
    {
        var evento = new PagamentoRecusadoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Saldo insuficiente");

        var recebido = IdaEVolta(evento);

        Assert.Equal(evento, recebido);
        Assert.Equal("Saldo insuficiente", recebido.Motivo);
    }
}
