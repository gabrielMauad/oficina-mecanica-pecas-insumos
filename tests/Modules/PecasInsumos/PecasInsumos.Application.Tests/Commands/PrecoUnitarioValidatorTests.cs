using PecasInsumos.Application.Commands.AdicionarPecaInsumo;
using PecasInsumos.Application.Commands.AtualizarPecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class PrecoUnitarioValidatorTests
{
    private static AdicionarPecaInsumoCommand Adicionar(decimal preco) =>
        new("Óleo de Motor", null, preco, 1, "Litro");

    [Theory(DisplayName = "Adicionar: preço dentro da faixa numeric(10,2) é aceito")]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(49.9)]
    [InlineData(99999999.99)]
    public void Adicionar_PrecoValido_Passa(double preco) =>
        Assert.True(new AdicionarPecaInsumoValidator().Validate(Adicionar((decimal)preco)).IsValid);

    [Fact(DisplayName = "Adicionar: preço negativo, acima de 99999999,99 ou com mais de 2 casas é rejeitado")]
    public void Adicionar_PrecoInvalido_Falha()
    {
        var validator = new AdicionarPecaInsumoValidator();

        Assert.False(validator.Validate(Adicionar(-0.01m)).IsValid);
        Assert.False(validator.Validate(Adicionar(100_000_000m)).IsValid);
        Assert.False(validator.Validate(Adicionar(decimal.MaxValue)).IsValid);
        Assert.False(validator.Validate(Adicionar(1.005m)).IsValid);
    }

    [Fact(DisplayName = "Atualizar: preço fora da faixa ou com mais de 2 casas é rejeitado; ausente é aceito")]
    public void Atualizar_Preco_Validacao()
    {
        var validator = new AtualizarPecaInsumoValidator();
        var id = Guid.NewGuid();

        Assert.True(validator.Validate(new AtualizarPecaInsumoCommand(id, "d", null)).IsValid);
        Assert.True(validator.Validate(new AtualizarPecaInsumoCommand(id, null, 99999999.99m)).IsValid);
        Assert.False(validator.Validate(new AtualizarPecaInsumoCommand(id, null, 100_000_000m)).IsValid);
        Assert.False(validator.Validate(new AtualizarPecaInsumoCommand(id, null, decimal.MaxValue)).IsValid);
        Assert.False(validator.Validate(new AtualizarPecaInsumoCommand(id, null, 1.234m)).IsValid);
        Assert.False(validator.Validate(new AtualizarPecaInsumoCommand(id, null, -1m)).IsValid);
    }
}
