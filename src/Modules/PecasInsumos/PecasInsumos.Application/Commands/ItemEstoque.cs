namespace PecasInsumos.Application.Commands;

/// <summary>Peça/insumo e quantidade a movimentar no estoque.</summary>
public sealed record ItemEstoque(Guid PecaInsumoId, int Quantidade);
