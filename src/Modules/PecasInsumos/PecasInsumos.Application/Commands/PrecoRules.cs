using FluentValidation;

namespace PecasInsumos.Application.Commands;

/// <summary>
/// Faixa do preço unitário aceita pela API: a coluna é numeric(10,2), ou seja, até 8 dígitos inteiros e
/// 2 casas decimais. Valores fora dela só falhariam no banco, como erro 500.
/// </summary>
internal static class PrecoRules
{
    public const decimal PrecoMaximo = 99_999_999.99m;

    public static IRuleBuilderOptions<T, decimal> PrecoUnitarioValido<T>(this IRuleBuilder<T, decimal> rule) =>
        rule
            .GreaterThanOrEqualTo(0)
            .WithMessage("O preço não deve ser negativo.")
            .LessThanOrEqualTo(PrecoMaximo)
            .WithMessage($"O preço não deve ser maior que {PrecoMaximo:0.00}.")
            .Must(valor => decimal.Round(valor, 2) == valor)
            .WithMessage("O preço deve ter no máximo 2 casas decimais.");
}
