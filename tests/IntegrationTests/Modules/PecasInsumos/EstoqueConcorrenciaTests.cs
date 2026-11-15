using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;
using PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using PecasInsumos.Infrastructure.Messaging.Consumers;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace IntegrationTests.Modules.PecasInsumos;

/// <summary>
/// Concorrência, idempotência e validações de borda do estoque contra o PostgreSQL real: as movimentações
/// são disparadas em paralelo, cada uma em seu próprio escopo (como os consumers e as requisições HTTP).
/// </summary>
[Collection("Integration")]
public class EstoqueConcorrenciaTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly OficinaMecanicaWebApplicationFactory _factory;

    public EstoqueConcorrenciaTests(OficinaMecanicaWebApplicationFactory factory) => _factory = factory;

    // ── Baixa / estorno do orçamento (comandos em paralelo) ──

    [Fact(DisplayName = "Baixas concorrentes de orçamentos distintos: nunca baixa além do saldo (sem overselling)")]
    public async Task BaixasConcorrentes_NaoBaixamAlemDoSaldo()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ =>
            Task.Run(() => EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(pecaId, 1)])))));

        Assert.Equal(10, resultados.Count(r => r.IsSuccess));
        Assert.Equal(30, resultados.Count(r => r.IsFailure));
        Assert.Equal(0, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Baixas concorrentes: saldo final = inicial - baixas efetivas (sem saldo fantasma)")]
    public async Task BaixasConcorrentes_SaldoFinalConfereComBaixasEfetivas()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 8);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            Task.Run(() => EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(pecaId, 1)])))));

        Assert.All(resultados, r => Assert.True(r.IsSuccess));
        Assert.Equal(0, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Baixa de orçamento com várias peças concorrendo: tudo ou nada e sem deadlock")]
    public async Task BaixasConcorrentesComVariasPecas_SaoTudoOuNada()
    {
        using var client = await NovoClientAsync();
        var a = await CriarPecaAsync(client, estoque: 5);
        var b = await CriarPecaAsync(client, estoque: 3);

        // Metade dos orçamentos lista as peças em ordem inversa para provocar travas cruzadas se não houver ordem fixa.
        var resultados = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(),
                i % 2 == 0 ? [new ItemEstoque(a, 1), new ItemEstoque(b, 1)] : [new ItemEstoque(b, 1), new ItemEstoque(a, 1)])))));

        int sucessos = resultados.Count(r => r.IsSuccess);
        Assert.Equal(3, sucessos);
        Assert.Equal(5 - sucessos, (await ObterPecaAsync(client, a)).QuantidadeEmEstoque);
        Assert.Equal(3 - sucessos, (await ObterPecaAsync(client, b)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Baixa com uma peça sem saldo: nenhuma peça é baixada e o orçamento não fica registrado")]
    public async Task Baixa_ComPecaSemSaldo_NaoBaixaNadaENaoRegistra()
    {
        using var client = await NovoClientAsync();
        var a = await CriarPecaAsync(client, estoque: 10);
        var b = await CriarPecaAsync(client, estoque: 1);
        var orcamentoId = Guid.NewGuid();

        var falha = await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(a, 3), new ItemEstoque(b, 2)]));

        Assert.True(falha.IsFailure);
        Assert.Equal(10, (await ObterPecaAsync(client, a)).QuantidadeEmEstoque);
        Assert.Equal(1, (await ObterPecaAsync(client, b)).QuantidadeEmEstoque);

        // Se a falha tivesse deixado o orçamento registrado, a nova tentativa seria ignorada como repetição.
        var sucesso = await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(a, 3), new ItemEstoque(b, 1)]));

        Assert.True(sucesso.IsSuccess);
        Assert.Equal(DesfechoMovimentacaoDoOrcamento.Aplicada, sucesso.Value);
        Assert.Equal(7, (await ObterPecaAsync(client, a)).QuantidadeEmEstoque);
        Assert.Equal(0, (await ObterPecaAsync(client, b)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Baixa de peça desativada falha e não altera o estoque")]
    public async Task Baixa_ComPecaDesativada_Falha()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        (await client.DeleteAsync($"/api/v1/pecas-insumos/{pecaId}")).EnsureSuccessStatusCode();

        var resultado = await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(pecaId, 1)]));

        Assert.True(resultado.IsFailure);
        Assert.Contains("desativada", resultado.Error.Message);
        Assert.Equal(10, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Mesma baixa entregue várias vezes em paralelo: baixa o estoque uma única vez")]
    public async Task BaixaRepetidaEmParalelo_BaixaUmaUnicaVez()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 20);
        var orcamentoId = Guid.NewGuid();

        var resultados = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            Task.Run(() => EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 4)])))));

        Assert.All(resultados, r => Assert.True(r.IsSuccess));
        Assert.Equal(1, resultados.Count(r => r.Value == DesfechoMovimentacaoDoOrcamento.Aplicada));
        Assert.Equal(16, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Mesmo estorno entregue várias vezes em paralelo: devolve o estoque uma única vez")]
    public async Task EstornoRepetidoEmParalelo_DevolveUmaUnicaVez()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        var orcamentoId = Guid.NewGuid();
        Assert.True((await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 2)]))).IsSuccess);
        Assert.Equal(8, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);

        var resultados = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            Task.Run(() => EnviarAsync(new EstornarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 2)])))));

        Assert.All(resultados, r => Assert.True(r.IsSuccess));
        Assert.Equal(1, resultados.Count(r => r.Value == DesfechoMovimentacaoDoOrcamento.Aplicada));
        Assert.Equal(10, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Estornos concorrentes de orçamentos distintos: nenhum incremento é perdido")]
    public async Task EstornosConcorrentes_NaoPerdemIncrementos()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 50);
        var orcamentos = Enumerable.Range(0, 25).Select(_ => Guid.NewGuid()).ToList();
        foreach (var orcamentoId in orcamentos)
            Assert.True((await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 2)]))).IsSuccess);
        Assert.Equal(0, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);

        await Task.WhenAll(orcamentos.Select(orcamentoId =>
            Task.Run(() => EnviarAsync(new EstornarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 2)])))));

        Assert.Equal(50, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Estorno que ultrapassaria int.MaxValue falha sem alterar o estoque")]
    public async Task Estorno_QueUltrapassaLimite_Falha()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: int.MaxValue);
        var orcamentoId = Guid.NewGuid();
        Assert.True((await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 1)]))).IsSuccess);
        (await client.PatchAsJsonAsync($"/api/v1/pecas-insumos/{pecaId}/estoque/entrada", new { Quantidade = 1 })).EnsureSuccessStatusCode();

        var resultado = await EnviarAsync(new EstornarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 1)]));

        Assert.True(resultado.IsFailure);
        Assert.Equal(int.MaxValue, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Estorno sem baixa registrada não altera o estoque")]
    public async Task Estorno_SemBaixa_NaoAlteraEstoque()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);

        var resultado = await EnviarAsync(new EstornarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(pecaId, 3)]));

        Assert.True(resultado.IsSuccess);
        Assert.Equal(DesfechoMovimentacaoDoOrcamento.SemBaixaRegistrada, resultado.Value);
        Assert.Equal(10, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Estorno antes da baixa: a baixa posterior é ignorada e não publica falha")]
    public async Task EstornoAntesDaBaixa_BaixaPosteriorEhIgnorada()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        var orcamentoId = Guid.NewGuid();
        var harness = _factory.Harness;
        await harness.Start();
        var consumidor = harness.GetConsumerHarness<OrcamentoGeradoConsumer>();

        Assert.True((await EnviarAsync(new EstornarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 3)]))).IsSuccess);
        await harness.Bus.Publish(new OrcamentoGeradoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), orcamentoId, [new ItemPecaEventDto(pecaId, 3)]));

        Assert.True(await AguardarAsync(() => consumidor.Consumed
            .Select<OrcamentoGeradoIntegrationEvent>(e => e.Context.Message.OrcamentoId == orcamentoId).Any()));
        Assert.Equal(10, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
        Assert.False(harness.Published
            .Select<EstoqueBaixaFalhouIntegrationEvent>(e => e.Context.Message.OrcamentoId == orcamentoId).Any());
    }

    [Fact(DisplayName = "Corrida paralela baixa x estorno do mesmo orçamento termina com o estoque inalterado")]
    public async Task CorridaBaixaXEstorno_MantemEstoque()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);

        for (int rodada = 0; rodada < 15; rodada++)
        {
            var orcamentoId = Guid.NewGuid();
            var itens = new[] { new ItemEstoque(pecaId, 4) };

            var baixa = Task.Run(() => EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, itens)));
            var estorno = Task.Run(() => EnviarAsync(new EstornarEstoqueDoOrcamentoCommand(orcamentoId, itens)));
            await Task.WhenAll(baixa, estorno);

            Assert.True(baixa.Result.IsSuccess, $"rodada {rodada}: baixa");
            Assert.True(estorno.Result.IsSuccess, $"rodada {rodada}: estorno");
            Assert.Equal(10, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
        }
    }

    [Fact(DisplayName = "Estorno de peça desativada depois da baixa devolve o saldo")]
    public async Task Estorno_DePecaDesativada_DevolveSaldo()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        var orcamentoId = Guid.NewGuid();
        Assert.True((await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 4)]))).IsSuccess);
        (await client.DeleteAsync($"/api/v1/pecas-insumos/{pecaId}")).EnsureSuccessStatusCode();

        var resultado = await EnviarAsync(new EstornarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 4)]));

        Assert.True(resultado.IsSuccess);
        Assert.Equal(DesfechoMovimentacaoDoOrcamento.Aplicada, resultado.Value);
        var peca = await ObterPecaAsync(client, pecaId);
        Assert.False(peca.Ativo);
        Assert.Equal(10, peca.QuantidadeEmEstoque);
    }

    // ── Eventos de integração entregues em duplicidade ──

    [Fact(DisplayName = "OrcamentoGerado entregue 2x: estoque baixado uma única vez e nenhuma falha publicada")]
    public async Task OrcamentoGeradoDuplicado_BaixaUmaUnicaVez()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        var harness = _factory.Harness;
        await harness.Start();
        var consumidor = harness.GetConsumerHarness<OrcamentoGeradoConsumer>();
        var orcamentoId = Guid.NewGuid();
        var evento = new OrcamentoGeradoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), orcamentoId, [new ItemPecaEventDto(pecaId, 3)]);

        await harness.Bus.Publish(evento);
        await harness.Bus.Publish(evento with { EventId = Guid.NewGuid() });

        Assert.True(await AguardarAsync(() => consumidor.Consumed
            .Select<OrcamentoGeradoIntegrationEvent>(e => e.Context.Message.OrcamentoId == orcamentoId).Count() == 2));
        Assert.Equal(7, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
        Assert.False(harness.Published
            .Select<EstoqueBaixaFalhouIntegrationEvent>(e => e.Context.Message.OrcamentoId == orcamentoId).Any());
    }

    [Fact(DisplayName = "OrcamentoRejeitado entregue 2x: estoque estornado uma única vez")]
    public async Task OrcamentoRejeitadoDuplicado_EstornaUmaUnicaVez()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        var harness = _factory.Harness;
        await harness.Start();
        var consumidor = harness.GetConsumerHarness<OrcamentoRejeitadoConsumer>();
        var orcamentoId = Guid.NewGuid();
        Assert.True((await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [new ItemEstoque(pecaId, 2)]))).IsSuccess);
        var evento = new OrcamentoRejeitadoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), orcamentoId, [new ItemPecaEventDto(pecaId, 2)]);

        await harness.Bus.Publish(evento);
        await harness.Bus.Publish(evento with { EventId = Guid.NewGuid() });

        Assert.True(await AguardarAsync(() => consumidor.Consumed
            .Select<OrcamentoRejeitadoIntegrationEvent>(e => e.Context.Message.OrcamentoId == orcamentoId).Count() == 2));
        Assert.Equal(10, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "OrcamentoGerado de peça desativada: publica EstoqueBaixaFalhou")]
    public async Task OrcamentoGerado_DePecaDesativada_PublicaFalha()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        (await client.DeleteAsync($"/api/v1/pecas-insumos/{pecaId}")).EnsureSuccessStatusCode();
        var harness = _factory.Harness;
        await harness.Start();
        var orcamentoId = Guid.NewGuid();

        await harness.Bus.Publish(new OrcamentoGeradoIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), orcamentoId, [new ItemPecaEventDto(pecaId, 1)]));

        Assert.True(await AguardarAsync(() => harness.Published
            .Select<EstoqueBaixaFalhouIntegrationEvent>(e => e.Context.Message.OrcamentoId == orcamentoId).Any()));
        var falha = harness.Published
            .Select<EstoqueBaixaFalhouIntegrationEvent>(e => e.Context.Message.OrcamentoId == orcamentoId)
            .Single().Context.Message;
        Assert.Contains("desativada", falha.Motivo);
        Assert.Equal(10, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    // ── Entrada/saída manual concorrentes ──

    [Fact(DisplayName = "Entradas manuais concorrentes: nenhuma entrada é perdida")]
    public async Task EntradasManuaisConcorrentes_NaoPerdemEntradas()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 0);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ =>
            Task.Run(() => client.PatchAsJsonAsync($"/api/v1/pecas-insumos/{pecaId}/estoque/entrada", new { Quantidade = 2 }))));

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(50, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Saídas manuais concorrentes: nunca deixam o estoque negativo")]
    public async Task SaidasManuaisConcorrentes_NaoDeixamEstoqueNegativo()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ =>
            Task.Run(() => client.PatchAsJsonAsync($"/api/v1/pecas-insumos/{pecaId}/estoque/saida", new { Quantidade = 1 }))));

        Assert.Equal(10, respostas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(15, respostas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(0, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "Atualizar preço com um saldo lido antes não desfaz a baixa feita nesse intervalo")]
    public async Task AtualizarCadastro_NaoSobrescreveSaldo()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);

        using var escopoAntigo = _factory.Services.CreateScope();
        var gateway = escopoAntigo.ServiceProvider.GetRequiredService<IPecaInsumoGateway>();
        PecaInsumo lidaAntes = (await gateway.ObterPorId(new PecaInsumoId(pecaId)))!;

        Assert.True((await EnviarAsync(new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(pecaId, 4)]))).IsSuccess);

        lidaAntes.AtualizarPrecoUnitario(99.90m);
        await gateway.Atualizar(lidaAntes);
        await escopoAntigo.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        var peca = await ObterPecaAsync(client, pecaId);
        Assert.Equal(6, peca.QuantidadeEmEstoque);
        Assert.Equal(99.90m, peca.PrecoUnitario);
    }

    // ── Validações de borda ──

    [Fact(DisplayName = "PATCH entrada: +1 em int.MaxValue retorna 422 e preserva o saldo")]
    public async Task Entrada_QueEstouraInt_Retorna422()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: int.MaxValue);

        var resposta = await client.PatchAsJsonAsync($"/api/v1/pecas-insumos/{pecaId}/estoque/entrada", new { Quantidade = 1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(int.MaxValue, (await ObterPecaAsync(client, pecaId)).QuantidadeEmEstoque);
    }

    [Theory(DisplayName = "POST peça: preço fora de numeric(10,2) retorna 422 (e não 500)")]
    [InlineData("100000000")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("1.005")]
    public async Task Criar_ComPrecoForaDaFaixa_Retorna422(string preco)
    {
        using var client = await NovoClientAsync();
        var json = $$"""{"nome":"{{TestData.Nome("Peça preço")}}","descricao":null,"preco":{{preco}},"quantidadeEmEstoque":1,"unidadeDeMedida":"Unidade"}""";

        var resposta = await client.PostAsync("/api/v1/pecas-insumos", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact(DisplayName = "POST peça: preço máximo 99999999.99 é aceito")]
    public async Task Criar_ComPrecoMaximo_Retorna201()
    {
        using var client = await NovoClientAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/pecas-insumos", new
        {
            Nome = TestData.Nome("Peça preço máximo"),
            Descricao = (string?)null,
            Preco = 99999999.99m,
            QuantidadeEmEstoque = 1,
            UnidadeDeMedida = "Unidade"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact(DisplayName = "PATCH preço: valor fora de numeric(10,2) retorna 422 (e não 500)")]
    public async Task AtualizarPreco_ForaDaFaixa_Retorna422()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 1);

        var resposta = await client.PatchAsJsonAsync($"/api/v1/pecas-insumos/{pecaId}/preco", new { Preco = 100_000_000m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Theory(DisplayName = "POST peça: unidade de medida em minúsculas é aceita e devolvida normalizada")]
    [InlineData("litro", "Litro")]
    [InlineData("QUILOGRAMA", "Quilograma")]
    public async Task Criar_ComUnidadeEmQualquerCaixa_Retorna201(string enviada, string esperada)
    {
        using var client = await NovoClientAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/pecas-insumos", new
        {
            Nome = TestData.Nome("Peça unidade"),
            Descricao = (string?)null,
            Preco = 10m,
            QuantidadeEmEstoque = 1,
            UnidadeDeMedida = enviada
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<UnidadeDto>(JsonOptions);
        Assert.Equal(esperada, corpo!.UnidadeDeMedida);
    }

    [Fact(DisplayName = "GET disponibilidade de peça desativada: disponivel=false mesmo com estoque")]
    public async Task Disponibilidade_DePecaDesativada_EhFalse()
    {
        using var client = await NovoClientAsync();
        var pecaId = await CriarPecaAsync(client, estoque: 10);
        (await client.DeleteAsync($"/api/v1/pecas-insumos/{pecaId}")).EnsureSuccessStatusCode();

        var resposta = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}/disponibilidade?quantidade=1");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<DisponibilidadeDto>(JsonOptions);
        Assert.False(corpo!.Disponivel);
    }

    // ── Helpers ──

    private async Task<Result<DesfechoMovimentacaoDoOrcamento>> EnviarAsync(
        IRequest<Result<DesfechoMovimentacaoDoOrcamento>> comando)
    {
        using var escopo = _factory.Services.CreateScope();
        return await escopo.ServiceProvider.GetRequiredService<ISender>().Send(comando);
    }

    private async Task<HttpClient> NovoClientAsync() =>
        _factory.CreateAuthenticatedClient(await _factory.GetAuthTokenAsync());

    private static async Task<Guid> CriarPecaAsync(HttpClient client, int estoque)
    {
        var resposta = await client.PostAsJsonAsync("/api/v1/pecas-insumos", new
        {
            Nome = TestData.Nome("Peça Concorrencia"),
            Descricao = (string?)null,
            Preco = 10m,
            QuantidadeEmEstoque = estoque,
            UnidadeDeMedida = "Unidade"
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<PecaIdDto>(JsonOptions))!.PecaInsumoId;
    }

    private static async Task<PecaDto> ObterPecaAsync(HttpClient client, Guid pecaId)
    {
        var resposta = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<PecaDto>(JsonOptions))!;
    }

    private static async Task<bool> AguardarAsync(Func<bool> condicao)
    {
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (!condicao() && DateTime.UtcNow < limite)
            await Task.Delay(50);

        return condicao();
    }

    private sealed record PecaIdDto(Guid PecaInsumoId);
    private sealed record PecaDto(Guid Id, decimal PrecoUnitario, int QuantidadeEmEstoque, bool Ativo);
    private sealed record UnidadeDto(string UnidadeDeMedida);
    private sealed record DisponibilidadeDto(bool Disponivel, decimal PrecoUnitario);
}
