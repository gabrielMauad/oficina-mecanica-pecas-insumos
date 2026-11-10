using FluentValidation;

namespace PecasInsumos.Application.Queries.ObterPecaInsumoPorId;

public sealed class ObterPecaInsumoPorIdValidator : AbstractValidator<ObterPecaInsumoPorIdQuery>
{
    public ObterPecaInsumoPorIdValidator()
    {
        RuleFor(x => x.PecaInsumoId)
            .NotEmpty().WithMessage("O ID da peça/insumo é obrigatório.");
    }
}

