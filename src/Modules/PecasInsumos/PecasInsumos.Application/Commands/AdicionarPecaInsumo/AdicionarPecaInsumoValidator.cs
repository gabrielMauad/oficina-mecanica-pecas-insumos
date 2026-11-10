using FluentValidation;
using PecasInsumos.Domain;

namespace PecasInsumos.Application.Commands.AdicionarPecaInsumo;

public sealed class AdicionarPecaInsumoValidator : AbstractValidator<AdicionarPecaInsumoCommand>
{
    public AdicionarPecaInsumoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("O nome da peça/insumo é obrigatório.")
            .MaximumLength(200)
            .WithMessage("O nome da peça/insumo deve ter no máximo 200 caracteres.");
        RuleFor(x => x.Preco)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O preço da peça/insumo não deve ser negativo.");
        RuleFor(x => x.QuantidadeEmEstoque)
            .GreaterThanOrEqualTo(0)
            .WithMessage("A quantidade em estoque da peça/insumo não pode ser negativa.");
        RuleFor(x => x.UnidadeDeMedida)
            .NotEmpty()
            .WithMessage("A unidade de medida da peça/insumo é obrigatória.")
            .Must(valor => Enum.TryParse<UnidadeDeMedida>(valor, true, out var parsed)
                && Enum.IsDefined(parsed))
            .WithMessage("A unidade de medida da peça/insumo é inválida.");
    }
}