using PecasInsumos.Domain;
using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.AdicionarPecaInsumo;

public sealed record AdicionarPecaInsumoCommand(
    string Nome,
    string? Descricao,
    decimal Preco,
    int QuantidadeEmEstoque,
    string UnidadeDeMedida
) : ICommand<PecaInsumo>;
