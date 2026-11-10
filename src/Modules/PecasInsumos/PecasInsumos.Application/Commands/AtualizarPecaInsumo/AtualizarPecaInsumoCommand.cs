using PecasInsumos.Domain;
using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.AtualizarPecaInsumo;

public sealed record AtualizarPecaInsumoCommand(
    Guid PecaInsumoId,
    string? Descricao,
    decimal? PrecoUnitario
) : ICommand<PecaInsumo>;
