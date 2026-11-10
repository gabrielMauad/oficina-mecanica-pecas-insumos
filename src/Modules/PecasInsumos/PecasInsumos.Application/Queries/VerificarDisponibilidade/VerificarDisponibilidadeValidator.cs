using FluentValidation;

namespace PecasInsumos.Application.Queries.VerificarDisponibilidade;

public sealed class VerificarDisponibilidadeValidator : AbstractValidator<VerificarDisponibilidadeQuery>
{
    public VerificarDisponibilidadeValidator()
    {
        RuleFor(x => x.PecaInsumoId)
            .NotEmpty().WithMessage("O ID da peça/insumo é obrigatório.");
        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
    }
}
