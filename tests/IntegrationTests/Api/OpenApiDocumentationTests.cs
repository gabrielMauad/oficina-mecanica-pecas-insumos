using System.Net;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Api;

[Collection("Integration")]
public class OpenApiDocumentationTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    public OpenApiDocumentationTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // As factories derivadas nao sao descartadas aqui: o descarte de cada uma pertence a factory
    // compartilhada (que as rastreia) e descarta-las antes interferia nos demais hosts de teste.
    private WebApplicationFactory<Program> ComOpenApi(bool habilitado) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddLogging(logging => logging.ClearProviders())).ConfigureAppConfiguration((_, cfg) =>
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OpenApi:Enabled"] = habilitado.ToString()
                })));

    [Fact(DisplayName = "OpenAPI habilitado — o documento declara o esquema Bearer JWT")]
    public async Task OpenApiHabilitado_DocumentoDeclaraEsquemaBearer()
    {
        var factory = ComOpenApi(true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var documento = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var bearer = documento.RootElement
            .GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact(DisplayName = "OpenAPI habilitado — operações do controller autorizado declaram requisito de segurança; a raiz não")]
    public async Task OpenApiHabilitado_OperacoesAutorizadasDeclaramSeguranca()
    {
        var factory = ComOpenApi(true);
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        using var documento = JsonDocument.Parse(json);
        var paths = documento.RootElement.GetProperty("paths");
        var operacoesDoController = paths.EnumerateObject()
            .Where(p => p.Name.StartsWith("/api/v1/pecas-insumos"))
            .SelectMany(p => p.Value.EnumerateObject())
            .Select(op => op.Value)
            .ToList();
        Assert.NotEmpty(operacoesDoController);
        Assert.All(operacoesDoController, op =>
            Assert.True(op.TryGetProperty("security", out var security) && security.GetArrayLength() > 0));
        Assert.False(paths.GetProperty("/").GetProperty("get").TryGetProperty("security", out _));
    }

    [Fact(DisplayName = "OpenAPI habilitado — a raiz redireciona para a UI do Scalar")]
    public async Task OpenApiHabilitado_RaizRedirecionaParaScalar()
    {
        var factory = ComOpenApi(true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/scalar", response.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "OpenAPI desabilitado — o documento não é exposto")]
    public async Task OpenApiDesabilitado_DocumentoNaoEhExposto()
    {
        var factory = ComOpenApi(false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
