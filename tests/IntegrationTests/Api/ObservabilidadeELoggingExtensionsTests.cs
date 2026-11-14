using Api.Extensions;
using Api.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace IntegrationTests.Api;

/// <summary>
/// Os testes de integração rodam no ambiente "Testing", onde o OpenTelemetry fica fora do host
/// (não há coletor OTLP). Aqui o host é montado, sem ser iniciado, em outros ambientes para
/// validar o que realmente é registrado em execução normal.
/// </summary>
public class ObservabilidadeELoggingExtensionsTests
{
    private static WebApplicationBuilder NovoBuilder(string ambiente, string? nomeDoServico = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = ambiente,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Logging.ClearProviders();

        if (nomeDoServico is not null)
            builder.Configuration["OTEL_SERVICE_NAME"] = nomeDoServico;

        return builder;
    }

    private static Dictionary<string, object> AtributosDoRecurso(WebApplication app) =>
        app.Services.GetRequiredService<TracerProvider>().GetResource().Attributes
            .ToDictionary(a => a.Key, a => a.Value);

    [Fact(DisplayName = "Observabilidade — fora do ambiente Testing registra tracing e métricas")]
    public void Observabilidade_EmProducao_RegistraTracingEMetricas()
    {
        var builder = NovoBuilder(Environments.Production);

        builder.AddOficinaMecanicaObservability();
        using var app = builder.Build();

        Assert.NotNull(app.Services.GetService<TracerProvider>());
        Assert.NotNull(app.Services.GetService<MeterProvider>());
    }

    [Fact(DisplayName = "Observabilidade — no ambiente Testing o SDK fica fora do host")]
    public void Observabilidade_EmTesting_NaoRegistraNada()
    {
        var builder = NovoBuilder("Testing");

        builder.AddOficinaMecanicaObservability();
        using var app = builder.Build();

        Assert.Null(app.Services.GetService<TracerProvider>());
        Assert.Null(app.Services.GetService<MeterProvider>());
    }

    [Fact(DisplayName = "Observabilidade — sem OTEL_SERVICE_NAME usa o nome padrão do serviço")]
    public void Observabilidade_SemNomeConfigurado_UsaNomePadrao()
    {
        var builder = NovoBuilder(Environments.Production);

        builder.AddOficinaMecanicaObservability();
        using var app = builder.Build();

        var atributos = AtributosDoRecurso(app);
        Assert.Equal("oficina-mecanica-pecas-insumos", atributos["service.name"]);
        Assert.Equal("oficina-mecanica", atributos["service.namespace"]);
        Assert.Equal(Environments.Production, atributos["deployment.environment"]);
    }

    [Fact(DisplayName = "Observabilidade — OTEL_SERVICE_NAME sobrescreve o nome padrão")]
    public void Observabilidade_ComNomeConfigurado_UsaONomeConfigurado()
    {
        var builder = NovoBuilder(Environments.Production, nomeDoServico: "outro-servico");

        builder.AddOficinaMecanicaObservability();
        using var app = builder.Build();

        Assert.Equal("outro-servico", AtributosDoRecurso(app)["service.name"]);
    }

    [Fact(DisplayName = "Logging estruturado — registra o formatter JSON e o seleciona no console")]
    public void LoggingEstruturado_RegistraFormatterJson()
    {
        var builder = NovoBuilder(Environments.Production);

        builder.AddStructuredJsonLogging();
        using var app = builder.Build();

        Assert.Contains(app.Services.GetServices<ConsoleFormatter>(), f => f is TraceJsonConsoleFormatter);
        Assert.Equal(
            TraceJsonConsoleFormatter.FormatterName,
            app.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value.FormatterName);
    }

    [Fact(DisplayName = "Logging OTLP — fora do ambiente Testing adiciona o provedor do OpenTelemetry")]
    public void LoggingOtlp_EmProducao_AdicionaProvedorOpenTelemetry()
    {
        var builder = NovoBuilder(Environments.Production);

        builder.AddOtlpLogging();
        using var app = builder.Build();

        Assert.Contains(
            app.Services.GetServices<ILoggerProvider>(),
            p => p.GetType().Name == "OpenTelemetryLoggerProvider");
    }

    [Fact(DisplayName = "Logging OTLP — no ambiente Testing não adiciona o provedor")]
    public void LoggingOtlp_EmTesting_NaoAdicionaProvedor()
    {
        var builder = NovoBuilder("Testing");

        builder.AddOtlpLogging();
        using var app = builder.Build();

        Assert.DoesNotContain(
            app.Services.GetServices<ILoggerProvider>(),
            p => p.GetType().Name == "OpenTelemetryLoggerProvider");
    }
}
