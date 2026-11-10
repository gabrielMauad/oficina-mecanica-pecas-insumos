namespace PecasInsumos.Adapters.Models.ViewModels;

public sealed record ObterPecaInsumoPorIdViewModel(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal PrecoUnitario,
    int QuantidadeEmEstoque,
    string UnidadeDeMedida,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm);
