namespace PecasInsumos.Adapters.Models.ViewModels;

public sealed record DesativarPecaInsumoViewModel(
    Guid PecaInsumoId,
    string Nome,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm);
