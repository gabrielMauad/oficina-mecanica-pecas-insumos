using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;
using PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;

namespace PecasInsumos.Application.Tests.Commands;

public class BaixaEEstornoDoOrcamentoValidatorTests
{
    [Fact(DisplayName = "Baixa: itens válidos e lista vazia passam")]
    public void Baixa_ItensValidosEListaVazia_Passam()
    {
        var validator = new BaixarEstoqueDoOrcamentoValidator();

        Assert.True(validator.Validate(new BaixarEstoqueDoOrcamentoCommand([new ItemEstoque(Guid.NewGuid(), 1)])).IsValid);
        Assert.True(validator.Validate(new BaixarEstoqueDoOrcamentoCommand([])).IsValid);
    }

    [Fact(DisplayName = "Baixa: id vazio e quantidade não positiva são rejeitados")]
    public void Baixa_ItemInvalido_Falha()
    {
        var validator = new BaixarEstoqueDoOrcamentoValidator();

        Assert.False(validator.Validate(new BaixarEstoqueDoOrcamentoCommand([new ItemEstoque(Guid.Empty, 1)])).IsValid);
        Assert.False(validator.Validate(new BaixarEstoqueDoOrcamentoCommand([new ItemEstoque(Guid.NewGuid(), 0)])).IsValid);
    }

    [Fact(DisplayName = "Estorno: itens válidos e lista vazia passam")]
    public void Estorno_ItensValidosEListaVazia_Passam()
    {
        var validator = new EstornarEstoqueDoOrcamentoValidator();

        Assert.True(validator.Validate(new EstornarEstoqueDoOrcamentoCommand([new ItemEstoque(Guid.NewGuid(), 1)])).IsValid);
        Assert.True(validator.Validate(new EstornarEstoqueDoOrcamentoCommand([])).IsValid);
    }

    [Fact(DisplayName = "Estorno: id vazio e quantidade não positiva são rejeitados")]
    public void Estorno_ItemInvalido_Falha()
    {
        var validator = new EstornarEstoqueDoOrcamentoValidator();

        Assert.False(validator.Validate(new EstornarEstoqueDoOrcamentoCommand([new ItemEstoque(Guid.Empty, 1)])).IsValid);
        Assert.False(validator.Validate(new EstornarEstoqueDoOrcamentoCommand([new ItemEstoque(Guid.NewGuid(), -1)])).IsValid);
    }
}
