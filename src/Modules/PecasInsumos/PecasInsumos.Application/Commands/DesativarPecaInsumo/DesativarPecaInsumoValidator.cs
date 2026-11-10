using FluentValidation;

namespace PecasInsumos.Application.Commands.DesativarPecaInsumo;

public sealed class DesativarPecaInsumoValidator : AbstractValidator<DesativarPecaInsumoCommand>
{
    public DesativarPecaInsumoValidator()
    {
        RuleFor(x => x.PecaInsumoId)
            .NotEmpty()
            .WithMessage("O Id da peça/insumo é obrigatório.");
    }
}

