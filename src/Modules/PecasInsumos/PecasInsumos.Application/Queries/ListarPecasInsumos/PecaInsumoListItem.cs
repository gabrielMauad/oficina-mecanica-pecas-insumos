namespace PecasInsumos.Application.Queries.ListarPecasInsumos;

public sealed record PecaInsumoListItem(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal PrecoUnitario,
    int QuantidadeEmEstoque,
    string UnidadeDeMedida,
    bool Ativo,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
);
