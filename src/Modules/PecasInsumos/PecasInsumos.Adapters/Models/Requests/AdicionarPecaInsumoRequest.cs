namespace PecasInsumos.Adapters.Models.Request;

public sealed record AdicionarPecaInsumoRequest(
    string Nome,
    string? Descricao,
    decimal Preco,
    int QuantidadeEmEstoque,
    string UnidadeDeMedida);
