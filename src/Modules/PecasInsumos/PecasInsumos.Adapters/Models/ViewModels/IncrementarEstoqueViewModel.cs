namespace PecasInsumos.Adapters.Models.ViewModels;

public sealed record IncrementarEstoqueViewModel(
    Guid PecaInsumoId,
    string Nome,
    int QuantidadeEmEstoque,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm);
