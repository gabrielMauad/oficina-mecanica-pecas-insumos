using FluentValidation;

namespace PecasInsumos.Application.Commands.AtualizarPecaInsumo;

public sealed class AtualizarPecaInsumoValidator : AbstractValidator<AtualizarPecaInsumoCommand>
{
    public AtualizarPecaInsumoValidator()
    {
        RuleFor(x => x.PecaInsumoId)
            .NotEmpty().WithMessage("O Id da peça/insumo é obrigatório.");
        RuleFor(x => x.PrecoUnitario)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O preço unitário não deve ser negativo.")
            .When(x => x.PrecoUnitario is not null);
    }
}
