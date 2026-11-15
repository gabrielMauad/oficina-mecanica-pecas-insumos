using Moq;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;
using PecasInsumos.Application.Gateways;

namespace PecasInsumos.Application.Tests.Commands;

public class EstornarEstoqueDoOrcamentoHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock = new();
    private readonly EstornarEstoqueDoOrcamentoHandler _handler;

    public EstornarEstoqueDoOrcamentoHandlerTests() => _handler = new EstornarEstoqueDoOrcamentoHandler(_gatewayMock.Object);

    private void GatewayRetorna(ResultadoMovimentacaoEmLote resultado) =>
        _gatewayMock
            .Setup(g => g.MovimentarEstoqueDoOrcamento(
                It.IsAny<Guid>(), It.IsAny<TipoMovimentacaoOrcamento>(),
                It.IsAny<IReadOnlyList<ItemEstoque>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);

    [Fact(DisplayName = "Cenário feliz: envia ao gateway um único estorno do orçamento com todos os itens")]
    public async Task Handle_DeveEstornarTodosOsItensEmUmaUnicaMovimentacao()
    {
        GatewayRetorna(new ResultadoMovimentacaoEmLote(false, []));
        var orcamentoId = Guid.NewGuid();
        var filtro = new ItemEstoque(Guid.NewGuid(), 3);
        var vela = new ItemEstoque(Guid.NewGuid(), 5);

        var result = await _handler.Handle(new EstornarEstoqueDoOrcamentoCommand(orcamentoId, [filtro, vela]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DesfechoMovimentacaoDoOrcamento.Aplicada, result.Value);
        _gatewayMock.Verify(g => g.MovimentarEstoqueDoOrcamento(
            orcamentoId,
            TipoMovimentacaoOrcamento.Estorno,
            It.Is<IReadOnlyList<ItemEstoque>>(i => i.Count == 2 && i.Contains(filtro) && i.Contains(vela)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Orçamento já estornado (evento repetido): sucesso com desfecho JaProcessada")]
    public async Task Handle_QuandoJaProcessado_RetornaJaProcessada()
    {
        GatewayRetorna(new ResultadoMovimentacaoEmLote(true, []));

        var result = await _handler.Handle(
            new EstornarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DesfechoMovimentacaoDoOrcamento.JaProcessada, result.Value);
    }

    [Fact(DisplayName = "Peça que não comporta o estorno: falha com o motivo da peça")]
    public async Task Handle_QuandoEstornoExcedeLimite_Falha()
    {
        var filtro = Guid.NewGuid();
        GatewayRetorna(new ResultadoMovimentacaoEmLote(false,
            [new FalhaMovimentacao(filtro, "Filtro de Óleo", MotivoFalhaMovimentacao.LimiteDeEstoque, int.MaxValue)]));

        var result = await _handler.Handle(
            new EstornarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(filtro, 1)]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.EstornoDoOrcamentoNaoRealizado", result.Error.Code);
        Assert.Contains("'Filtro de Óleo'", result.Error.Message);
        Assert.Contains("ultrapassar", result.Error.Message);
    }

    [Fact(DisplayName = "Peça desativada ou inexistente: falha citando cada uma")]
    public async Task Handle_QuandoPecaDesativadaOuInexistente_Falha()
    {
        var desativada = Guid.NewGuid();
        var inexistente = Guid.NewGuid();
        GatewayRetorna(new ResultadoMovimentacaoEmLote(false,
        [
            new FalhaMovimentacao(desativada, "Vela", MotivoFalhaMovimentacao.Desativada, 4),
            new FalhaMovimentacao(inexistente, null, MotivoFalhaMovimentacao.NaoEncontrada, 0)
        ]));

        var result = await _handler.Handle(
            new EstornarEstoqueDoOrcamentoCommand(Guid.NewGuid(), [new ItemEstoque(desativada, 1), new ItemEstoque(inexistente, 1)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("desativada", result.Error.Message);
        Assert.Contains("não encontrada", result.Error.Message);
    }

    [Fact(DisplayName = "Orçamento sem peças: sucesso sem tocar no gateway")]
    public async Task Handle_QuandoSemPecas_Sucesso()
    {
        var result = await _handler.Handle(new EstornarEstoqueDoOrcamentoCommand(Guid.NewGuid(), []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gatewayMock.VerifyNoOtherCalls();
    }
}
