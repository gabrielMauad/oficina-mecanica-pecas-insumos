using PecasInsumos.Domain;
using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.DecrementarEstoque;

public sealed record DecrementarEstoqueCommand(
    Guid PecaInsumoId,
    int Quantidade
) : ICommand<PecaInsumo>;
