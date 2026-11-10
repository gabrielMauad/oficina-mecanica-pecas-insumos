using Moq;
using PecasInsumos.Application.Commands.AdicionarPecaInsumo;
using PecasInsumos.Application.Gateways;
using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class AdicionarPecaInsumoHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock;
    private readonly AdicionarPecaInsumoHandler _handler;

    public AdicionarPecaInsumoHandlerTests()
    {
        _gatewayMock = new Mock<IPecaInsumoGateway>();
        _handler = new AdicionarPecaInsumoHandler(_gatewayMock.Object);
    }

    [Fact(DisplayName = "Cenário feliz")]
    public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new AdicionarPecaInsumoCommand(
            Nome: "Óleo de Motor",
            Descricao: "Óleo sintético 5W30",
            Preco: 49.90m,
            QuantidadeEmEstoque: 10,
            UnidadeDeMedida: "Litro"
        );

        _gatewayMock.Setup(x => x.ExistePorNome(command.Nome, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);

        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
        Assert.Equal(command.Nome, result.Value.Nome);
        Assert.Equal(command.Descricao, result.Value.Descricao);
        Assert.Equal(command.Preco, result.Value.PrecoUnitario.Valor);
        Assert.Equal(command.QuantidadeEmEstoque, result.Value.QuantidadeEmEstoque);
        Assert.Equal(command.UnidadeDeMedida, result.Value.UnidadeDeMedida.ToString());
        Assert.True(result.Value.Ativo);
        Assert.InRange(result.Value.CadastradoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        Assert.InRange(result.Value.AtualizadoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);

        _gatewayMock.Verify(x => x.Adicionar(It.Is<PecaInsumoEntity>(p => p.Nome == command.Nome), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Erro: Peça/insumo já existe")]
    public async Task Handle_ShouldReturnError_WhenNomeJaExiste()
    {
        // Arrange
        var command = new AdicionarPecaInsumoCommand(
            Nome: "Óleo de Motor",
            Descricao: "Óleo sintético 5W30",
            Preco: 49.90m,
            QuantidadeEmEstoque: 10,
            UnidadeDeMedida: "Litro"
        );

        _gatewayMock.Setup(x => x.ExistePorNome(command.Nome, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.NomeJaExiste", result.Error.Code);
        Assert.Equal("Já existe uma peça/insumo cadastrada com este nome.", result.Error.Message);
    }

    [Fact(DisplayName = "Replica erro do domínio: nome vazio")]
    public async Task Handle_ShouldReturnError_WhenNomeIsEmpty()
    {
        // Arrange
        var command = new AdicionarPecaInsumoCommand(
            Nome: "",
            Descricao: "Óleo sintético 5W30",
            Preco: 49.90m,
            QuantidadeEmEstoque: 10,
            UnidadeDeMedida: "Litro"
        );

        _gatewayMock.Setup(x => x.ExistePorNome(command.Nome, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.NomeVazio", result.Error.Code);
        Assert.Equal("Nome é obrigatório.", result.Error.Message);
    }

    [Fact(DisplayName = "Replica erro do domínio: quantidade em estoque negativa")]
    public async Task Handle_ShouldReturnError_WhenQuantidadeEmEstoqueIsNegative()
    {
        // Arrange
        var command = new AdicionarPecaInsumoCommand(
            Nome: "Óleo de Motor",
            Descricao: "Óleo sintético 5W30",
            Preco: 49.90m,
            QuantidadeEmEstoque: -1,
            UnidadeDeMedida: "Litro"
        );

        _gatewayMock.Setup(x => x.ExistePorNome(command.Nome, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.QuantidadeInvalida", result.Error.Code);
        Assert.Equal("Quantidade em estoque não pode ser negativa.", result.Error.Message);
    }

    [Fact(DisplayName = "Replica erro do domínio: preço negativo")]
    public async Task Handle_ShouldReturnError_WhenPrecoIsNegative()
    {
        // Arrange
        var command = new AdicionarPecaInsumoCommand(
            Nome: "Óleo de Motor",
            Descricao: "Óleo sintético 5W30",
            Preco: -1m,
            QuantidadeEmEstoque: 10,
            UnidadeDeMedida: "Litro"
        );

        _gatewayMock.Setup(x => x.ExistePorNome(command.Nome, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Dinheiro.Negativo", result.Error.Code);
        Assert.Equal("Preço não pode ser negativo.", result.Error.Message);
    }
}
