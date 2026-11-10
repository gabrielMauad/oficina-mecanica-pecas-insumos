using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.HealthChecks;

[Collection("Integration")]
public class HealthCheckEndpointsTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HealthCheckEndpointsTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // GET /healthz/live
    // =========================================================================

    [Fact(DisplayName = "GET /healthz/live — retorna 200 sem autenticação e sem checks de dependências")]
    public async Task Live_DeveRetornar200_SemAutenticacaoENenhumCheckDeDependencia()
    {
        // Arrange — client sem token
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthReportDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Empty(body.Checks);
    }

    // =========================================================================
    // GET /healthz/ready
    // =========================================================================

    [Fact(DisplayName = "GET /healthz/ready — retorna 200 sem autenticação com o check do postgresql saudável")]
    public async Task Ready_DeveRetornar200_ComCheckDoPostgreSqlSaudavel()
    {
        // Arrange — client sem token; banco disponível via Testcontainers
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthReportDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Contains(body.Checks, c => c.Name == "postgresql" && c.Status == "Healthy");
    }

    // =========================================================================
    // GET /healthz
    // =========================================================================

    [Fact(DisplayName = "GET /healthz — retorna 200 sem autenticação agregando todos os checks")]
    public async Task Healthz_DeveRetornar200_AgregandoTodosOsChecks()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthReportDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Contains(body.Checks, c => c.Name == "postgresql");
    }

    private sealed record HealthReportDto(string Status, double TotalDurationMs, List<HealthCheckEntryDto> Checks);

    private sealed record HealthCheckEntryDto(string Name, string Status, string? Description, double DurationMs);
}
