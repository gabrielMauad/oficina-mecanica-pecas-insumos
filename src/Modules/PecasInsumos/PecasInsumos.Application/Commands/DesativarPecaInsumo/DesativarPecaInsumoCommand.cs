using PecasInsumos.Domain;
using SharedKernel.Application;

namespace PecasInsumos.Application.Commands.DesativarPecaInsumo;

public sealed record DesativarPecaInsumoCommand(Guid PecaInsumoId) : ICommand<PecaInsumo>;
