using Moq;
using PecasInsumos.Application.Commands.DesativarPecaInsumo;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class DesativarPecaInsumoHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock;
    private readonly DesativarPecaInsumoHandler _handler;

    public DesativarPecaInsumoHandlerTests()
    {
        _gatewayMock = new Mock<IPecaInsumoGateway>();
        _handler = new DesativarPecaInsumoHandler(_gatewayMock.Object);
    }

    [Fact(DisplayName = "Cenário feliz")]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new DesativarPecaInsumoCommand(Guid.NewGuid());
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 5, UnidadeDeMedida.Unidade).Value;

        _gatewayMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);

        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
        Assert.Equal("Filtro de Óleo", result.Value.Nome);
        Assert.False(result.Value.Ativo);

        _gatewayMock.Verify(x => x.Atualizar(It.Is<PecaInsumoEntity>(p => p.Nome == "Filtro de Óleo" && !p.Ativo), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: Peça/insumo não encontrada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoNotFound()
    {
        // Arrange
        var command = new DesativarPecaInsumoCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.NaoEncontrada", result.Error.Code);
        Assert.Equal("Peça/insumo não encontrada.", result.Error.Message);

        _gatewayMock.Verify(x => x.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Idempotencia: peça/insumo já desativada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoAlreadyDesativada()
    {
        // Arrange
        var command = new DesativarPecaInsumoCommand(Guid.NewGuid());
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descrição", 10, 5, UnidadeDeMedida.Unidade).Value;
        pecaInsumo.Desativar();

        _gatewayMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.JaDesativada", result.Error.Code);
        Assert.Equal("A peça/insumo já está desativada.", result.Error.Message);

        _gatewayMock.Verify(x => x.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
