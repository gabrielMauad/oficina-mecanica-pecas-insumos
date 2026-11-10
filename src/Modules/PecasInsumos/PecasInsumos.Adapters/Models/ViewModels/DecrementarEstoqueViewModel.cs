namespace PecasInsumos.Adapters.Models.ViewModels;

public sealed record DecrementarEstoqueViewModel(
    Guid PecaInsumoId,
    string Nome,
    int QuantidadeEmEstoque,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm);
