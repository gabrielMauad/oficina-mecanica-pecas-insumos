using FluentValidation;

namespace PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;

public sealed class EstornarEstoqueDoOrcamentoValidator : AbstractValidator<EstornarEstoqueDoOrcamentoCommand>
{
    public EstornarEstoqueDoOrcamentoValidator()
    {
        RuleFor(x => x.Itens)
            .NotNull()
            .WithMessage("Itens do orçamento são obrigatórios.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.PecaInsumoId)
                .NotEmpty()
                .WithMessage("Id da peça/insumo é obrigatório.");
            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0)
                .WithMessage("Quantidade a incrementar deve ser maior que zero.");
        });
    }
}
