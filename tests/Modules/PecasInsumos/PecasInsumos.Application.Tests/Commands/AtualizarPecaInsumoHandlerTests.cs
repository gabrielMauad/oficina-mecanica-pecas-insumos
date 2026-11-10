using Moq;
using PecasInsumos.Application.Commands.AtualizarPecaInsumo;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class AtualizarPecaInsumoHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock;
    private readonly AtualizarPecaInsumoHandler _handler;

    public AtualizarPecaInsumoHandlerTests()
    {
        _gatewayMock = new Mock<IPecaInsumoGateway>();
        _handler = new AtualizarPecaInsumoHandler(_gatewayMock.Object);
    }

    [Theory(DisplayName = "Cenário feliz")]
    [InlineData("Descricao atualizada", null)]
    [InlineData(null, 1d)]
    [InlineData("Descricao atualizada", 1d)]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid(string? descricao, double? preco)
    {
        // Arrange
        decimal? precoDecimal = preco.HasValue ? (decimal)preco.Value : null;
        var command = new AtualizarPecaInsumoCommand(
            Guid.NewGuid(),
            descricao,
            precoDecimal
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", "Descricao inicial", 0, 5, UnidadeDeMedida.Unidade).Value;
        var descricaoEsperada = descricao ?? pecaInsumo.Descricao;
        var precoEsperado = precoDecimal ?? pecaInsumo.PrecoUnitario.Valor;

        _gatewayMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);

        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
        Assert.Equal(descricaoEsperada, result.Value.Descricao);
        Assert.Equal(precoEsperado, result.Value.PrecoUnitario.Valor);
        Assert.True(result.Value.Ativo);
        Assert.InRange(result.Value.AtualizadoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);

        _gatewayMock.Verify(x => x.Atualizar(It.Is<PecaInsumoEntity>(p => p.Descricao == descricaoEsperada && p.PrecoUnitario.Valor == precoEsperado), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: Peça/insumo não encontrada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoNotFound()
    {
        // Arrange
        var command = new AtualizarPecaInsumoCommand(
            Guid.NewGuid(),
            "Descricao",
            100
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.NaoEncontrada", result.Error.Code);
        Assert.Equal("Peça/insumo não encontrada.", result.Error.Message);
    }

    [Fact(DisplayName = "Erro: Peça/insumo desativada")]
    public async Task Handle_ShouldReturnError_WhenPecaInsumoDesativada()
    {
        // Arrange
        var command = new AtualizarPecaInsumoCommand(
            Guid.NewGuid(),
            "Descricao",
            100
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", command.Descricao, 0, 5, UnidadeDeMedida.Unidade).Value;
        pecaInsumo.Desativar();
        _gatewayMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.JaDesativada", result.Error.Code);
        Assert.Equal("A peça/insumo já está desativada.", result.Error.Message);
    }

    [Fact(DisplayName = "Erro: Replica erro de domínio")]
    public async Task Handle_ShouldReturnError_WhenDomainFails()
    {
        // Arrange
        var command = new AtualizarPecaInsumoCommand(
            Guid.NewGuid(),
            "Descricao",
            -1
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", command.Descricao, 0, 5, UnidadeDeMedida.Unidade).Value;
        _gatewayMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Dinheiro.Negativo", result.Error.Code);
        Assert.Equal("Preço não pode ser negativo.", result.Error.Message);
    }

    [Fact(DisplayName = "Idempotencia: peça/insumo sem alterações")]
    public async Task Handle_ShouldDoNothing_WhenPecaInsumoHasNoChanges()
    {
        // Arrange
        var command = new AtualizarPecaInsumoCommand(
            Guid.NewGuid(),
            "Descricao",
            100
        );
        var pecaInsumoId = new PecaInsumoId(command.PecaInsumoId);
        PecaInsumoEntity pecaInsumo = PecaInsumoEntity.Criar("Filtro de Óleo", command.Descricao, 100, 5, UnidadeDeMedida.Unidade).Value;
        _gatewayMock.Setup(x => x.ObterPorId(pecaInsumoId, It.IsAny<CancellationToken>())).ReturnsAsync(pecaInsumo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);

        _gatewayMock.Verify(x => x.Atualizar(It.Is<PecaInsumoEntity>(p => p.Nome == "Filtro de Óleo"), It.IsAny<CancellationToken>()), Times.Never);
    }
}
