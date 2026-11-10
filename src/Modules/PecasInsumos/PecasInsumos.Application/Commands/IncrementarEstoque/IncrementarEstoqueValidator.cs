using FluentValidation;

namespace PecasInsumos.Application.Commands.IncrementarEstoque;

public sealed class IncrementarEstoqueValidator : AbstractValidator<IncrementarEstoqueCommand>
{
    public IncrementarEstoqueValidator()
    {
        RuleFor(x => x.PecaInsumoId)
            .NotEmpty()
            .WithMessage("Id da peça/insumo é obrigatório.");
        RuleFor(x => x.Quantidade)
            .GreaterThan(0)
            .WithMessage("Quantidade a incrementar deve ser maior que zero.");
    }
}

