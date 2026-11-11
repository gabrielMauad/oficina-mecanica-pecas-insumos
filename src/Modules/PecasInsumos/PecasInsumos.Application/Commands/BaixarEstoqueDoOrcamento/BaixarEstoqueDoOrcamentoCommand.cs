using PecasInsumos.Domain;
using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;

/// <summary>
/// Baixa, de forma atômica, o estoque de todas as peças de um orçamento. Se qualquer peça não puder
/// ser baixada, nada é alterado.
/// </summary>
public sealed record BaixarEstoqueDoOrcamentoCommand(
    IReadOnlyList<ItemEstoque> Itens
) : ICommand<IReadOnlyList<PecaInsumo>>;
