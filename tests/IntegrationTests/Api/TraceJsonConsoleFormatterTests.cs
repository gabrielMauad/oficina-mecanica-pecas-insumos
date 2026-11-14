using System.Diagnostics;
using System.Text.Json;
using Api.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationTests.Api;

public class TraceJsonConsoleFormatterTests
{
    private static JsonElement Escrever<TState>(
        LogLevel nivel,
        TState estado,
        Exception? excecao = null,
        IExternalScopeProvider? escopos = null,
        Func<TState, Exception?, string>? formatter = null)
    {
        var formatterDeLog = new TraceJsonConsoleFormatter();
        var saida = new StringWriter();
        var entrada = new LogEntry<TState>(
            nivel, "Categoria.Teste", new EventId(1), estado, excecao,
            formatter ?? ((s, _) => s?.ToString() ?? string.Empty));

        formatterDeLog.Write(in entrada, escopos, saida);

        var linha = saida.ToString().TrimEnd();
        return JsonDocument.Parse(linha).RootElement.Clone();
    }

    [Theory(DisplayName = "Nível do log — é escrito pelo nome")]
    [InlineData(LogLevel.Trace, "Trace")]
    [InlineData(LogLevel.Debug, "Debug")]
    [InlineData(LogLevel.Information, "Information")]
    [InlineData(LogLevel.Warning, "Warning")]
    [InlineData(LogLevel.Error, "Error")]
    [InlineData(LogLevel.Critical, "Critical")]
    [InlineData(LogLevel.None, "None")]
    public void Write_EscreveONomeDoNivel(LogLevel nivel, string esperado)
    {
        var json = Escrever(nivel, "mensagem");

        Assert.Equal(esperado, json.GetProperty("level").GetString());
        Assert.Equal("mensagem", json.GetProperty("message").GetString());
        Assert.Equal("Categoria.Teste", json.GetProperty("category").GetString());
    }

    [Fact(DisplayName = "Com Activity corrente — trace_id e span_id vêm da Activity")]
    public void Write_ComActivity_PreencheTraceId()
    {
        using var source = new ActivitySource("IntegrationTests.Formatter");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = source.StartActivity("operacao");
        Assert.NotNull(activity);

        var json = Escrever(LogLevel.Information, "msg");

        Assert.Equal(activity.TraceId.ToHexString(), json.GetProperty("trace_id").GetString());
        Assert.Equal(activity.SpanId.ToHexString(), json.GetProperty("span_id").GetString());
    }

    [Fact(DisplayName = "Sem Activity corrente — trace_id e span_id são null")]
    public void Write_SemActivity_TraceIdNulo()
    {
        var anterior = Activity.Current;
        Activity.Current = null;
        try
        {
            var json = Escrever(LogLevel.Information, "msg");

            Assert.Equal(JsonValueKind.Null, json.GetProperty("trace_id").ValueKind);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("span_id").ValueKind);
        }
        finally
        {
            Activity.Current = anterior;
        }
    }

    [Fact(DisplayName = "Mensagem vazia sem exceção — nada é escrito")]
    public void Write_MensagemVaziaSemExcecao_NaoEscreve()
    {
        var formatter = new TraceJsonConsoleFormatter();
        var saida = new StringWriter();
        var entrada = new LogEntry<string>(
            LogLevel.Information, "cat", new EventId(0), string.Empty, null, (s, _) => s);

        formatter.Write(in entrada, null, saida);

        Assert.Equal(string.Empty, saida.ToString());
    }

    [Fact(DisplayName = "Com exceção — escreve o bloco exception com tipo, mensagem e stack trace")]
    public void Write_ComExcecao_EscreveBlocoDeExcecao()
    {
        Exception excecao;
        try { throw new InvalidOperationException("falhou"); }
        catch (Exception e) { excecao = e; }

        var json = Escrever(LogLevel.Error, string.Empty, excecao);

        var bloco = json.GetProperty("exception");
        Assert.Equal(typeof(InvalidOperationException).FullName, bloco.GetProperty("type").GetString());
        Assert.Equal("falhou", bloco.GetProperty("message").GetString());
        Assert.False(string.IsNullOrEmpty(bloco.GetProperty("stack_trace").GetString()));
    }

    [Fact(DisplayName = "Propriedades do estado — escritas com o tipo JSON adequado, sem {OriginalFormat}")]
    public void Write_PropriedadesDoEstado_SaoEscritasTipadas()
    {
        var data = new DateTime(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);
        var dataOffset = new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);
        var guid = Guid.NewGuid();
        List<KeyValuePair<string, object>> estado =
        [
            new("{OriginalFormat}", "ignorado {Texto}"),
            new("Texto", "abc"),
            new("Flag", true),
            new("Inteiro", 42),
            new("Decimal", 1.5m),
            new("Data", data),
            new("DataOffset", dataOffset),
            new("Guid", guid),
        ];

        var json = Escrever(LogLevel.Information, estado, formatter: (_, _) => "mensagem formatada");

        Assert.False(json.TryGetProperty("{OriginalFormat}", out _));
        Assert.Equal("abc", json.GetProperty("Texto").GetString());
        Assert.True(json.GetProperty("Flag").GetBoolean());
        Assert.Equal(42, json.GetProperty("Inteiro").GetInt64());
        Assert.Equal(1.5, json.GetProperty("Decimal").GetDouble());
        Assert.Equal(data.ToString("O"), json.GetProperty("Data").GetString());
        Assert.Equal(dataOffset.ToString("O"), json.GetProperty("DataOffset").GetString());
        Assert.Equal(guid.ToString(), json.GetProperty("Guid").GetString());
    }

    [Fact(DisplayName = "Escopos — as propriedades dos escopos entram na linha de log")]
    public void Write_Escopos_EntramNaLinha()
    {
        var escopos = new LoggerExternalScopeProvider();
        using var escopoComPropriedades = escopos.Push(
            new List<KeyValuePair<string, object>> { new("OrdemServicoId", "os-123") });
        using var escopoTexto = escopos.Push("escopo-texto-ignorado");

        var json = Escrever(LogLevel.Information, "msg", escopos: escopos);

        Assert.Equal("os-123", json.GetProperty("OrdemServicoId").GetString());
    }
}
