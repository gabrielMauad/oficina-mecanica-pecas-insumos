using Moq;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class EstornarEstoqueDoOrcamentoHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock = new();
    private readonly EstornarEstoqueDoOrcamentoHandler _handler;

    public EstornarEstoqueDoOrcamentoHandlerTests() => _handler = new EstornarEstoqueDoOrcamentoHandler(_gatewayMock.Object);

    private static PecaInsumoEntity NovaPeca(string nome, int estoque) =>
        PecaInsumoEntity.Criar(nome, null, 10, estoque, UnidadeDeMedida.Unidade).Value;

    private void ExpoePeca(PecaInsumoEntity peca) =>
        _gatewayMock
            .Setup(g => g.ObterPorId(It.Is<PecaInsumoId>(id => id.Value == peca.Id.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(peca);

    [Fact(DisplayName = "Cenário feliz: estorna todas as peças e atualiza cada uma")]
    public async Task Handle_DeveEstornarTodasAsPecas()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        var vela = NovaPeca("Vela", 0);
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new EstornarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(vela.Id.Value, 5)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(13, filtro.QuantidadeEmEstoque);
        Assert.Equal(5, vela.QuantidadeEmEstoque);
        _gatewayMock.Verify(g => g.Atualizar(filtro, It.IsAny<CancellationToken>()), Times.Once);
        _gatewayMock.Verify(g => g.Atualizar(vela, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Segunda peça inexistente: falha e nada é atualizado")]
    public async Task Handle_QuandoSegundaPecaNaoExiste_NaoAtualizaNenhuma()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        ExpoePeca(filtro);
        var inexistente = Guid.NewGuid();
        var command = new EstornarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(inexistente, 1)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.EstornoDoOrcamentoNaoRealizado", result.Error.Code);
        Assert.Contains(inexistente.ToString(), result.Error.Message);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Peça desativada: falha e nada é atualizado")]
    public async Task Handle_QuandoPecaDesativada_NaoAtualizaNenhuma()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        var vela = NovaPeca("Vela", 10);
        vela.Desativar();
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new EstornarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 1), new ItemEstoque(vela.Id.Value, 1)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("desativada", result.Error.Message);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Mesma peça repetida: soma as quantidades e atualiza uma única vez")]
    public async Task Handle_QuandoPecaRepetida_SomaQuantidades()
    {
        var filtro = NovaPeca("Filtro de Óleo", 1);
        ExpoePeca(filtro);
        var command = new EstornarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(filtro.Id.Value, 4)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(8, filtro.QuantidadeEmEstoque);
        _gatewayMock.Verify(g => g.Atualizar(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Orçamento sem peças: sucesso sem tocar no estoque")]
    public async Task Handle_QuandoSemPecas_Sucesso()
    {
        var result = await _handler.Handle(new EstornarEstoqueDoOrcamentoCommand([]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
