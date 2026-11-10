using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Factory compartilhada por todos os testes de integração via [Collection("Integration")].
/// Sobe um container PostgreSQL real (Testcontainers) uma única vez por sessão de testes,
/// aplica migrations automaticamente (via Program.cs que já chama MigrateAsync no startup),
/// usa o transporte InMemory do MassTransit com o test harness e fornece helpers de autenticação.
/// </summary>
public sealed class OficinaMecanicaWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Credenciais de teste — mesmas injetadas na configuração do host
    private const string TestJwtSecret = "integration-test-jwt-secret-minimum-32-chars!!";

    // Mesmos valores configurados via Jwt:Issuer / Jwt:Audience / Jwt:ValidIssuers abaixo —
    // o token precisa ter um dos dois emissores aceitos e a audience da API (ADR-003).
    private const string AppIssuer = "oficina-mecanica-app";
    private const string ClienteAuthIssuer = "oficina-mecanica-auth";
    private const string Audience = "oficina-mecanica-api";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:15.1")
        .WithDatabase("pecas_insumos")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // IAsyncLifetime.InitializeAsync — xunit.v3 chama antes do primeiro teste da coleção
    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();
    }

    // Sobrescreve WebApplicationFactory.DisposeAsync e também satisfaz IAsyncLifetime.DisposeAsync
    // (ambos têm a mesma assinatura ValueTask DisposeAsync())
    public override async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Sobrescreve configurações sensíveis com valores de teste.
        // ConfigureAppConfiguration adiciona fontes APÓS os defaults (appsettings.json),
        // então estes valores têm prioridade.
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Connection string aponta para o container Testcontainers
                ["ConnectionStrings:Default"] = _db.GetConnectionString(),
                // JWT secret — mesmo valor usado para gerar e validar tokens nos testes
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = AppIssuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:ValidIssuers:0"] = AppIssuer,
                ["Jwt:ValidIssuers:1"] = ClienteAuthIssuer,
                // Mensageria em memória — sem SQS/SNS nos testes
                ["Mensageria:Transporte"] = "InMemory",
                // A exposição do OpenAPI/Scalar não depende mais do ambiente (ver
                // Api.Extensions.OpenApiExtensions), então é desligada explicitamente aqui
                // para manter a suíte de integração rápida e sem rotas de documentação.
                ["OpenApi:Enabled"] = "false",
            });
        });

        // Test harness do MassTransit sobre a configuração do próprio host: expõe ITestHarness
        // (publicar eventos e aguardar o consumo) mantendo os consumers reais do serviço.
        builder.ConfigureTestServices(services => services.AddMassTransitTestHarness());
    }

    /// <summary>
    /// Harness do MassTransit do host de teste (barramento em memória com os consumers reais).
    /// </summary>
    public ITestHarness Harness => Services.GetRequiredService<ITestHarness>();

    /// <summary>
    /// Cria um HttpClient com Authorization: Bearer {token} pré-configurado.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Gera um token de papel Oficina assinado com o mesmo segredo de teste, simulando o token
    /// emitido pelo serviço de autenticação (emissor "oficina-mecanica-app").
    /// </summary>
    public Task<string> GetAuthTokenAsync() =>
        Task.FromResult(GerarToken(AppIssuer, Guid.NewGuid(), "Oficina"));

    /// <summary>
    /// Gera um token de papel Cliente assinado com o mesmo segredo de teste, simulando o
    /// token que a Function Serverless (emissor "oficina-mecanica-auth") emitiria.
    /// </summary>
    public string GetClienteAuthToken(Guid clienteId) =>
        GerarToken(ClienteAuthIssuer, clienteId, "Cliente");

    /// <summary>
    /// Cria um HttpClient autenticado com um token de papel Cliente para o clienteId informado.
    /// </summary>
    public HttpClient CreateClienteAuthenticatedClient(Guid clienteId) =>
        CreateAuthenticatedClient(GetClienteAuthToken(clienteId));

    private static string GerarToken(string issuer, Guid sub, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, sub.ToString()),
            new Claim("role", role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Define a coleção "Integration" — todos os testes com [Collection("Integration")]
/// compartilham a mesma instância de OficinaMecanicaWebApplicationFactory.
/// xunit.v3 garante que os testes da coleção NÃO rodam em paralelo entre si.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection
    : ICollectionFixture<OficinaMecanicaWebApplicationFactory> { }
