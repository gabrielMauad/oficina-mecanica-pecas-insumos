namespace PecasInsumos.Adapters.Models.ViewModels;

public sealed record AdicionarPecaInsumoViewModel(
    Guid PecaInsumoId,
    string Nome,
    string? Descricao,
    decimal PrecoUnitario,
    int QuantidadeEmEstoque,
    string UnidadeDeMedida,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm);
