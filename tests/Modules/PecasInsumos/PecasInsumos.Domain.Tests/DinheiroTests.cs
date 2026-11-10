using SharedKernel.Domain;

namespace PecasInsumos.Domain.Tests;

public class DinheiroTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(1.11)]
    [InlineData(0)]
    [InlineData(0.11)]
    public void Criar_ComValoresValidos_RetornaDinheiro(decimal valor)
    {
        // Act
        var dinheiroResult = Dinheiro.Criar(valor);

        // Assert
        Assert.True(dinheiroResult.IsSuccess);
        Assert.False(dinheiroResult.IsFailure);
        Assert.Equal(Error.None, dinheiroResult.Error);
        Assert.Equal(valor, dinheiroResult.Value.Valor);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.0001)]
    public void Criar_ComValoresInvalidos_RetornaErro(decimal valor)
    {
        // Act
        var dinheiroResult = Dinheiro.Criar(valor);

        // Assert
        Assert.False(dinheiroResult.IsSuccess);
        Assert.True(dinheiroResult.IsFailure);
        Assert.Equal("Dinheiro.Negativo", dinheiroResult.Error.Code);
        Assert.Equal("Preço não pode ser negativo.", dinheiroResult.Error.Message);
    }
}


