using Moq;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;
using PecasInsumos.Application.Gateways;

namespace PecasInsumos.Application.Tests.Commands;

public class BaixarEstoqueDoOrcamentoHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock = new();
    private readonly BaixarEstoqueDoOrcamentoHandler _handler;

    public BaixarEstoqueDoOrcamentoHandlerTests() => _handler = new BaixarEstoqueDoOrcamentoHandler(_gatewayMock.Object);

    private void GatewayRetorna(ResultadoMovimentacaoEmLote resultado) =>
        _gatewayMock
            .Setup(g => g.MovimentarEstoqueDoOrcamento(
                It.IsAny<Guid>(), It.IsAny<TipoMovimentacaoOrcamento>(),
                It.IsAny<IReadOnlyList<ItemEstoque>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);

    private static ResultadoMovimentacaoEmLote Sucesso => new(false, []);

    [Fact(DisplayName = "Cenário feliz: envia ao gateway uma única baixa do orçamento com todos os itens")]
    public async Task Handle_DeveBaixarTodosOsItensEmUmaUnicaMovimentacao()
    {
        GatewayRetorna(Sucesso);
        var orcamentoId = Guid.NewGuid();
        var filtro = new ItemEstoque(Guid.NewGuid(), 3);
        var vela = new ItemEstoque(Guid.NewGuid(), 5);

        var result = await _handler.Handle(new BaixarEstoqueDoOrcamentoCommand(orcamentoId, [filtro, vela]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DesfechoMovimentacaoDoOrcamento.Aplicada, result.Value);
        _gatewayMock.Verify(g => g.MovimentarEstoqueDoOrcamento(
            orcamentoId,
            TipoMovimentacaoOrcamento.Baixa,
            It.Is<IReadOnlyList<ItemEstoque>>(i => i.Count == 2 && i.Contains(filtro) && i.Contains(vela)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Orçamento já baixado (evento repetido): sucesso com desfecho JaProcessada")]
    public async Task Handle_QuandoJaProcessada_RetornaJaProcessada()
    {
        GatewayRetorna(new ResultadoMovimentacaoEmLote(true, []));

        var result = await _handler.Handle(
            new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DesfechoMovimentacaoDoOrcamento.JaProcessada, result.Value);
    }

    [Fact(DisplayName = "Peça sem estoque suficiente: falha com o motivo da peça")]
    public async Task Handle_QuandoPecaSemEstoque_Falha()
    {
        var vela = Guid.NewGuid();
        GatewayRetorna(new ResultadoMovimentacaoEmLote(false,
            [new FalhaMovimentacao(vela, "Vela", MotivoFalhaMovimentacao.EstoqueInsuficiente, 2)]));

        var result = await _handler.Handle(
            new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(vela, 4)]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.BaixaDoOrcamentoNaoRealizada", result.Error.Code);
        Assert.Contains("'Vela'", result.Error.Message);
        Assert.Contains("quantidade: 4", result.Error.Message);
        Assert.Contains("em estoque: 2", result.Error.Message);
    }

    [Fact(DisplayName = "Peça inexistente: falha citando o id")]
    public async Task Handle_QuandoPecaNaoExiste_Falha()
    {
        var inexistente = Guid.NewGuid();
        GatewayRetorna(new ResultadoMovimentacaoEmLote(false,
            [new FalhaMovimentacao(inexistente, null, MotivoFalhaMovimentacao.NaoEncontrada, 0)]));

        var result = await _handler.Handle(
            new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(inexistente, 1)]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(inexistente.ToString(), result.Error.Message);
        Assert.Contains("não encontrada", result.Error.Message);
    }

    [Fact(DisplayName = "Peça desativada: falha informando que está desativada")]
    public async Task Handle_QuandoPecaDesativada_Falha()
    {
        var vela = Guid.NewGuid();
        GatewayRetorna(new ResultadoMovimentacaoEmLote(false,
            [new FalhaMovimentacao(vela, "Vela", MotivoFalhaMovimentacao.Desativada, 10)]));

        var result = await _handler.Handle(
            new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(vela, 1)]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("'Vela'", result.Error.Message);
        Assert.Contains("desativada", result.Error.Message);
    }

    [Fact(DisplayName = "Várias peças com problema: o motivo lista todas")]
    public async Task Handle_QuandoVariasPecasComProblema_MotivoListaTodas()
    {
        var filtro = Guid.NewGuid();
        var vela = Guid.NewGuid();
        GatewayRetorna(new ResultadoMovimentacaoEmLote(false,
        [
            new FalhaMovimentacao(filtro, "Filtro de Óleo", MotivoFalhaMovimentacao.EstoqueInsuficiente, 1),
            new FalhaMovimentacao(vela, "Vela", MotivoFalhaMovimentacao.EstoqueInsuficiente, 1)
        ]));

        var result = await _handler.Handle(
            new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(filtro, 2), new ItemEstoque(vela, 2)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("'Filtro de Óleo'", result.Error.Message);
        Assert.Contains("'Vela'", result.Error.Message);
    }

    [Fact(DisplayName = "Mesma peça repetida: soma as quantidades numa única entrada")]
    public async Task Handle_QuandoPecaRepetida_SomaQuantidades()
    {
        GatewayRetorna(Sucesso);
        var filtro = Guid.NewGuid();

        var result = await _handler.Handle(
            new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(filtro, 3), new ItemEstoque(filtro, 4)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gatewayMock.Verify(g => g.MovimentarEstoqueDoOrcamento(
            It.IsAny<Guid>(),
            TipoMovimentacaoOrcamento.Baixa,
            It.Is<IReadOnlyList<ItemEstoque>>(i => i.Count == 1 && i[0] == new ItemEstoque(filtro, 7)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Soma da peça repetida acima de int.MaxValue: falha sem chamar o gateway")]
    public async Task Handle_QuandoSomaExcedeLimite_FalhaSemTocarNoGateway()
    {
        var filtro = Guid.NewGuid();

        var result = await _handler.Handle(
            new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(),
                [new ItemEstoque(filtro, int.MaxValue), new ItemEstoque(filtro, 1)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(filtro.ToString(), result.Error.Message);
        _gatewayMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Orçamento sem peças: sucesso sem tocar no gateway")]
    public async Task Handle_QuandoSemPecas_Sucesso()
    {
        var result = await _handler.Handle(new BaixarEstoqueDoOrcamentoCommand(Guid.NewGuid(), []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gatewayMock.VerifyNoOtherCalls();
    }
}
