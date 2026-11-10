using MediatR;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.ListarPecasInsumos;

public sealed record ListarPecasInsumosQuery : IRequest<Result<List<PecaInsumoListItem>>>;
