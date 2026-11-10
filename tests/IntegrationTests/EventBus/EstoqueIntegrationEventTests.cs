using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Infrastructure.Messaging;
using SharedKernel.Application;

namespace IntegrationTests.EventBus;

[Collection("Integration")]
public class EstoqueIntegrationEventTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EstoqueIntegrationEventTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "IIntegrationEventBus do container é o adapter MassTransit (não o bus in-memory)")]
    public void IntegrationEventBus_DeveSerOAdapterMassTransit()
    {
        _ = _factory.Server; // garante que o host foi construído

        using var scope = _factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();

        Assert.IsType<MassTransitIntegrationEventBus>(bus);
    }

    [Fact(DisplayName = "OrcamentoGerado publicado no barramento → estoque é baixado no banco")]
    public async Task OrcamentoGerado_DeveBaixarEstoque()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());
        const int estoqueInicial = 10;
        const int quantidadeUsada = 3;
        var pecaId = await CriarPecaAsync(client, TestData.Nome("Vela de Ignição NGK EventBus"), 12.50m, estoqueInicial);
        var harness = _factory.Harness;
        await harness.Start();

        var evento = new OrcamentoGeradoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            [new ItemPecaEventDto(pecaId, quantidadeUsada)]);

        // Act
        await harness.Bus.Publish(evento);

        // Assert
        var peca = await AguardarEstoqueAsync(client, pecaId, estoqueInicial - quantidadeUsada);
        Assert.Equal(estoqueInicial - quantidadeUsada, peca.QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "OrcamentoRejeitado publicado no barramento → estoque é estornado no banco")]
    public async Task OrcamentoRejeitado_DeveEstornarEstoque()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());
        const int estoqueInicial = 20;
        const int quantidadeEstornada = 5;
        var pecaId = await CriarPecaAsync(client, TestData.Nome("Correia Dentada Gates EventBus"), 95.00m, estoqueInicial);
        var harness = _factory.Harness;
        await harness.Start();

        var evento = new OrcamentoRejeitadoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            [new ItemPecaEventDto(pecaId, quantidadeEstornada)]);

        // Act
        await harness.Bus.Publish(evento);

        // Assert
        var peca = await AguardarEstoqueAsync(client, pecaId, estoqueInicial + quantidadeEstornada);
        Assert.Equal(estoqueInicial + quantidadeEstornada, peca.QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "OrcamentoGerado seguido de OrcamentoRejeitado → estoque volta ao valor inicial")]
    public async Task OrcamentoGeradoEOrcamentoRejeitado_DeveRestaurarEstoque()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());
        const int estoqueInicial = 15;
        const int quantidade = 4;
        var pecaId = await CriarPecaAsync(client, TestData.Nome("Pastilha de Freio EventBus"), 80.00m, estoqueInicial);
        var harness = _factory.Harness;
        await harness.Start();
        var pecas = new[] { new ItemPecaEventDto(pecaId, quantidade) };
        var ordemServicoId = Guid.NewGuid();
        var orcamentoId = Guid.NewGuid();

        var gerado = new OrcamentoGeradoIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, ordemServicoId, orcamentoId, pecas);
        var rejeitado = new OrcamentoRejeitadoIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, ordemServicoId, orcamentoId, pecas);

        // Act
        await harness.Bus.Publish(gerado);
        var aposBaixa = await AguardarEstoqueAsync(client, pecaId, estoqueInicial - quantidade);

        await harness.Bus.Publish(rejeitado);
        var aposEstorno = await AguardarEstoqueAsync(client, pecaId, estoqueInicial);

        // Assert
        Assert.Equal(estoqueInicial - quantidade, aposBaixa.QuantidadeEmEstoque);
        Assert.Equal(estoqueInicial, aposEstorno.QuantidadeEmEstoque);
    }

    // ── Helpers privados ──

    /// <summary>
    /// O consumo é assíncrono (barramento): consulta o estoque pela API até o valor esperado ou até o
    /// limite de espera. Não usa a espera por inatividade do harness, que já pode ter expirado quando
    /// outros testes da coleção rodaram antes (o host de teste é compartilhado).
    /// </summary>
    private static async Task<PecaDto> AguardarEstoqueAsync(HttpClient client, Guid pecaId, int esperado)
    {
        var limite = DateTime.UtcNow.AddSeconds(15);
        var peca = await ObterPecaAsync(client, pecaId);
        while (peca.QuantidadeEmEstoque != esperado && DateTime.UtcNow < limite)
        {
            await Task.Delay(50);
            peca = await ObterPecaAsync(client, pecaId);
        }

        return peca;
    }

    private static async Task<PecaDto> ObterPecaAsync(HttpClient client, Guid pecaId)
    {
        var response = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PecaDto>(JsonOptions))!;
    }

    private static async Task<Guid> CriarPecaAsync(HttpClient client, string nome, decimal preco, int estoque)
    {
        var response = await client.PostAsJsonAsync("/api/v1/pecas-insumos", new
        {
            Nome = nome,
            Descricao = (string?)null,
            Preco = preco,
            QuantidadeEmEstoque = estoque,
            UnidadeDeMedida = "Unidade"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PecaIdDto>(JsonOptions);
        return body!.PecaInsumoId;
    }

    private sealed record PecaIdDto(Guid PecaInsumoId);

    private sealed record PecaDto(
        Guid Id,
        string Nome,
        decimal PrecoUnitario,
        int QuantidadeEmEstoque,
        string UnidadeDeMedida,
        bool Ativo);
}
