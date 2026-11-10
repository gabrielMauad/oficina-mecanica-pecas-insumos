namespace PecasInsumos.Contracts.Dtos;

public sealed record PecaInsumoResumoDto(
    Guid Id,
    string Nome,
    decimal PrecoUnitario,
    string UnidadeDeMedida,
    bool Ativo
);
