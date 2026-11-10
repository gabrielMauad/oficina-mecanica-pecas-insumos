namespace PecasInsumos.Adapters.Models.ViewModels;

public sealed record AtualizarPecaInsumoViewModel(
    Guid PecaInsumoId,
    decimal PrecoUnitario,
    string? Descricao,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm);
