using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Domain.Tests;

public class PecaInsumoTests
{
    [Theory]
    [InlineData("Óleo de Motor", "Óleo 5W30", 50.0, 10, UnidadeDeMedida.Litro)]
    [InlineData("Filtro de Ar", null, 0, 0, UnidadeDeMedida.Unidade)]
    [InlineData("Pastilha de Freio", "", 0.01, 5, UnidadeDeMedida.Par)]
    [InlineData("Parafuso", "  ", 1.5, 100, UnidadeDeMedida.Unidade)]
    public void Criar_ComDadosValidos_RetornaPecaInsumo(string nome, string? descricao, decimal preco, int quantidade, UnidadeDeMedida unidade)
    {
        // Act
        var pecaInsumoResult = PecaInsumoEntity.Criar(nome, descricao, preco, quantidade, unidade);

        // Assert
        Assert.True(pecaInsumoResult.IsSuccess);
        Assert.False(pecaInsumoResult.IsFailure);
        Assert.Equal(Error.None, pecaInsumoResult.Error);
        Assert.Equal(nome, pecaInsumoResult.Value.Nome);
        Assert.Equal(descricao, pecaInsumoResult.Value.Descricao);
        Assert.Equal(preco, pecaInsumoResult.Value.PrecoUnitario.Valor);
        Assert.Equal(quantidade, pecaInsumoResult.Value.QuantidadeEmEstoque);
        Assert.Equal(unidade, pecaInsumoResult.Value.UnidadeDeMedida);
        Assert.True(pecaInsumoResult.Value.Ativo);
        Assert.InRange(pecaInsumoResult.Value.CadastradoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        Assert.InRange(pecaInsumoResult.Value.AtualizadoEm, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        Assert.NotEqual(Guid.Empty, pecaInsumoResult.Value.Id.Value);
    }

    [Theory]
    [InlineData("", "Descricao", 10.0, 1, "PecaInsumo.NomeVazio", "Nome é obrigatório.")]
    [InlineData("   ", "Descricao", 10.0, 1, "PecaInsumo.NomeVazio", "Nome é obrigatório.")]
    [InlineData("Óleo", null, -1, 1, "Dinheiro.Negativo", "Preço não pode ser negativo.")]
    [InlineData("Óleo", null, -0.0001, 1, "Dinheiro.Negativo", "Preço não pode ser negativo.")]
    [InlineData("Óleo", null, 10.0, -1, "PecaInsumo.QuantidadeInvalida", "Quantidade em estoque não pode ser negativa.")]
    public void Criar_ComDadosInvalidos_RetornaErro(string nome, string? descricao, decimal preco, int quantidade, string errorCode, string errorMessage)
    {
        // Act
        var pecaInsumoResult = PecaInsumoEntity.Criar(nome, descricao, preco, quantidade, UnidadeDeMedida.Unidade);

        // Assert
        Assert.False(pecaInsumoResult.IsSuccess);
        Assert.True(pecaInsumoResult.IsFailure);
        Assert.Equal(errorCode, pecaInsumoResult.Error.Code);
        Assert.Equal(errorMessage, pecaInsumoResult.Error.Message);
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(10, 15)]
    [InlineData(100, 105)]
    public void Incrementar_ComQuantidadeValida_AumentaEstoque(int quantidade, int estoqueEsperado)
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 1.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.Incrementar(quantidade);

        // Assert
        Assert.True(pecaInsumoResult.IsSuccess);
        Assert.Equal(estoqueEsperado, pecaInsumo.QuantidadeEmEstoque);
    }

    [Theory]
    [InlineData(0, "PecaInsumo.QuantidadeInvalida", "Quantidade a incrementar deve ser positiva.")]
    [InlineData(-1, "PecaInsumo.QuantidadeInvalida", "Quantidade a incrementar deve ser positiva.")]
    [InlineData(-10, "PecaInsumo.QuantidadeInvalida", "Quantidade a incrementar deve ser positiva.")]
    public void Incrementar_ComQuantidadeInvalida_RetornaErro(int quantidade, string errorCode, string errorMessage)
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 1.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.Incrementar(quantidade);

        // Assert
        Assert.False(pecaInsumoResult.IsSuccess);
        Assert.True(pecaInsumoResult.IsFailure);
        Assert.Equal(errorCode, pecaInsumoResult.Error.Code);
        Assert.Equal(errorMessage, pecaInsumoResult.Error.Message);
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(3, 2)]
    [InlineData(5, 0)]
    public void Decrementar_ComQuantidadeValida_DiminuiEstoque(int quantidade, int estoqueEsperado)
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 1.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.Decrementar(quantidade);

        // Assert
        Assert.True(pecaInsumoResult.IsSuccess);
        Assert.Equal(estoqueEsperado, pecaInsumo.QuantidadeEmEstoque);
    }

    [Theory]
    [InlineData(0, "PecaInsumo.QuantidadeInvalida", "Quantidade a decrementar deve ser positiva.")]
    [InlineData(-1, "PecaInsumo.QuantidadeInvalida", "Quantidade a decrementar deve ser positiva.")]
    public void Decrementar_ComQuantidadeInvalida_RetornaErro(int quantidade, string errorCode, string errorMessage)
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 1.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.Decrementar(quantidade);

        // Assert
        Assert.False(pecaInsumoResult.IsSuccess);
        Assert.True(pecaInsumoResult.IsFailure);
        Assert.Equal(errorCode, pecaInsumoResult.Error.Code);
        Assert.Equal(errorMessage, pecaInsumoResult.Error.Message);
    }

    [Fact]
    public void Decrementar_MaisQueEstoqueDisponivel_RetornaErro()
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 1.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.Decrementar(6);

        // Assert
        Assert.False(pecaInsumoResult.IsSuccess);
        Assert.True(pecaInsumoResult.IsFailure);
        Assert.Equal("PecaInsumo.EstoqueInsuficiente", pecaInsumoResult.Error.Code);
        Assert.Equal("Quantidade em estoque não pode ficar negativa.", pecaInsumoResult.Error.Message);
    }

    [Fact]
    public void Decrementar_AteZero_ZeraEstoque()
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 1.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        pecaInsumo.Decrementar(5);

        // Assert
        Assert.Equal(0, pecaInsumo.QuantidadeEmEstoque);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    [InlineData(999.99)]
    public void AtualizarPrecoUnitario_ComPrecoValido_AtualizaPreco(decimal novoPreco)
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 10.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.AtualizarPrecoUnitario(novoPreco);

        // Assert
        Assert.True(pecaInsumoResult.IsSuccess);
        Assert.Equal(novoPreco, pecaInsumo.PrecoUnitario.Valor);
    }

    [Theory]
    [InlineData(-1, "Dinheiro.Negativo", "Preço não pode ser negativo.")]
    [InlineData(-0.0001, "Dinheiro.Negativo", "Preço não pode ser negativo.")]
    public void AtualizarPrecoUnitario_ComPrecoNegativo_RetornaErro(decimal novoPreco, string errorCode, string errorMessage)
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 10.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.AtualizarPrecoUnitario(novoPreco);

        // Assert
        Assert.False(pecaInsumoResult.IsSuccess);
        Assert.True(pecaInsumoResult.IsFailure);
        Assert.Equal(errorCode, pecaInsumoResult.Error.Code);
        Assert.Equal(errorMessage, pecaInsumoResult.Error.Message);
    }

    [Fact]
    public void AtualizarDescricao_ComNovaDescricao_AtualizaDescricao()
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", "desc antiga", 10.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        var pecaInsumoResult = pecaInsumo.AtualizarDescricao("desc nova");

        // Assert
        Assert.True(pecaInsumoResult.IsSuccess);
        Assert.Equal("desc nova", pecaInsumo.Descricao);
    }

    [Fact]
    public void Desativar_PecaAtiva_SetaAtivoComoFalso()
    {
        // Arrange
        var pecaInsumo = PecaInsumoEntity.Criar("Parafuso", null, 10.0m, 5, UnidadeDeMedida.Unidade).Value;

        // Act
        pecaInsumo.Desativar();

        // Assert
        Assert.False(pecaInsumo.Ativo);
    }
}
