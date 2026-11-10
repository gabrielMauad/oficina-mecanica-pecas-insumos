using Moq;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Application.Queries.ObterPecaInsumoPorId;
using PecasInsumos.Domain;
using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Queries;

public class ObterPecaInsumoPorIdHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock;
    private readonly ObterPecaInsumoPorIdHandler _handler;

    public ObterPecaInsumoPorIdHandlerTests()
    {
        _gatewayMock = new Mock<IPecaInsumoGateway>();
        _handler = new ObterPecaInsumoPorIdHandler(_gatewayMock.Object);
    }

    [Fact(DisplayName = "Cenário feliz")]
    public async Task Handle_ShouldReturnSuccess_WhenQueryIsValid()
    {
        // Arrange
        var query = new ObterPecaInsumoPorIdQuery(Guid.NewGuid());
        var pecaInsumoId = new PecaInsumoId(query.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição inicial", 49.90m, 5, UnidadeDeMedida.Unidade).Value;

        _gatewayMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);

        Assert.Same(pecaInsumo, result.Value);
        Assert.Equal(pecaInsumo.Id, result.Value.Id);
        Assert.Equal("Filtro de Óleo", result.Value.Nome);
        Assert.Equal("Descrição inicial", result.Value.Descricao);
        Assert.Equal(49.90m, result.Value.PrecoUnitario.Valor);
        Assert.Equal(5, result.Value.QuantidadeEmEstoque);
        Assert.Equal(UnidadeDeMedida.Unidade, result.Value.UnidadeDeMedida);
        Assert.True(result.Value.Ativo);
    }

    [Fact(DisplayName = "Erro: Peça/insumo não encontrada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoNotFound()
    {
        // Arrange
        var query = new ObterPecaInsumoPorIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.NaoEncontrada", result.Error.Code);
        Assert.Equal("Peça/insumo não encontrada.", result.Error.Message);
    }
}
