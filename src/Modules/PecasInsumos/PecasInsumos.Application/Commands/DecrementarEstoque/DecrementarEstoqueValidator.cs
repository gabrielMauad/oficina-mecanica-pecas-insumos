using FluentValidation;

namespace PecasInsumos.Application.Commands.DecrementarEstoque;

public sealed class DecrementarEstoqueValidator : AbstractValidator<DecrementarEstoqueCommand>
{
    public DecrementarEstoqueValidator()
    {
        RuleFor(x => x.PecaInsumoId)
            .NotEmpty()
            .WithMessage("Id da peça/insumo é obrigatório.");
        RuleFor(x => x.Quantidade)
            .GreaterThan(0)
            .WithMessage("Quantidade a decrementar deve ser maior que zero.");
    }
}

