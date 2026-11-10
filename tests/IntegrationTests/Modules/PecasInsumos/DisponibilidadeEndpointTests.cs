using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.PecasInsumos;

[Collection("Integration")]
public class DisponibilidadeEndpointTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DisponibilidadeEndpointTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> CriarPecaAsync(HttpClient client, int estoque, decimal preco)
    {
        var response = await client.PostAsJsonAsync("/api/v1/pecas-insumos", new
        {
            Nome = TestData.Nome("Peça Disponibilidade"),
            Descricao = (string?)null,
            Preco = preco,
            QuantidadeEmEstoque = estoque,
            UnidadeDeMedida = "Unidade"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PecaIdDto>(JsonOptions);
        return body!.PecaInsumoId;
    }

    [Theory(DisplayName = "GET /pecas-insumos/{id}/disponibilidade — 200 com disponivel conforme o estoque")]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public async Task Disponibilidade_DeveRetornar200_ComDisponivelConformeEstoque(int quantidade, bool esperado)
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());
        var pecaId = await CriarPecaAsync(client, estoque: 10, preco: 25.50m);

        // Act
        var response = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}/disponibilidade?quantidade={quantidade}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DisponibilidadeResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(esperado, body.Disponivel);
        Assert.Equal(25.50m, body.PrecoUnitario);
    }

    [Fact(DisplayName = "GET /pecas-insumos/{id}/disponibilidade — corpo usa as propriedades disponivel e precoUnitario")]
    public async Task Disponibilidade_DeveSerializarEmCamelCase()
    {
        // Arrange
        using var client = _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());
        var pecaId = await CriarPecaAsync(client, estoque: 3, preco: 10m);

        // Act
        var response = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}/disponibilidade?quantidade=1");

        // Assert
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("disponivel", out var disponivel));
        Assert.True(disponivel.GetBoolean());
        Assert.True(json.RootElement.TryGetProperty("precoUnitario", out _));
    }

    [Fact(DisplayName = "GET /pecas-insumos/{id}/disponibilidade — 404 quando a peça não existe")]
    public async Task Disponibilidade_DeveRetornar404_QuandoPecaNaoExiste()
    {
        using var client = _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());

        var response = await client.GetAsync($"/api/v1/pecas-insumos/{Guid.NewGuid()}/disponibilidade?quantidade=1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory(DisplayName = "GET /pecas-insumos/{id}/disponibilidade — 422 quando a quantidade não é positiva")]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Disponibilidade_DeveRetornar422_QuandoQuantidadeInvalida(int quantidade)
    {
        using var client = _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());
        var pecaId = await CriarPecaAsync(client, estoque: 10, preco: 5m);

        var response = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}/disponibilidade?quantidade={quantidade}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact(DisplayName = "GET /pecas-insumos/{id}/disponibilidade — 401 sem token")]
    public async Task Disponibilidade_DeveRetornar401_SemToken()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/pecas-insumos/{Guid.NewGuid()}/disponibilidade?quantidade=1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "GET /pecas-insumos/{id}/disponibilidade — 403 para papel Cliente (exige Oficina)")]
    public async Task Disponibilidade_DeveRetornar403_ParaPapelCliente()
    {
        using var client = _factory.CreateClienteAuthenticatedClient(Guid.NewGuid());

        var response = await client.GetAsync($"/api/v1/pecas-insumos/{Guid.NewGuid()}/disponibilidade?quantidade=1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record PecaIdDto(Guid PecaInsumoId);

    private sealed record DisponibilidadeResponse(bool Disponivel, decimal PrecoUnitario);
}
