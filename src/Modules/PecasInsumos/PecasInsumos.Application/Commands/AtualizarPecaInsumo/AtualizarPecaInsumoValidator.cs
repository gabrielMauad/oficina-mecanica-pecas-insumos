using FluentValidation;

namespace PecasInsumos.Application.Commands.AtualizarPecaInsumo;

public sealed class AtualizarPecaInsumoValidator : AbstractValidator<AtualizarPecaInsumoCommand>
{
    public AtualizarPecaInsumoValidator()
    {
        RuleFor(x => x.PecaInsumoId)
            .NotEmpty().WithMessage("O Id da peça/insumo é obrigatório.");
        RuleFor(x => x.PrecoUnitario!.Value)
            .PrecoUnitarioValido()
            .When(x => x.PrecoUnitario is not null);
    }
}
