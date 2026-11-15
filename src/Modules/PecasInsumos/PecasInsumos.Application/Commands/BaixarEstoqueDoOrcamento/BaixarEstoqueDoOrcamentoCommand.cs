using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;

/// <summary>
/// Baixa, de forma atômica, o estoque de todas as peças de um orçamento. Se qualquer peça não puder
/// ser baixada, nada é alterado. Idempotente por orçamento: repetir o comando para o mesmo orçamento não
/// baixa o estoque de novo.
/// </summary>
public sealed record BaixarEstoqueDoOrcamentoCommand(
    Guid OrcamentoId,
    IReadOnlyList<ItemEstoque> Itens
) : ICommand<DesfechoMovimentacaoDoOrcamento>;
