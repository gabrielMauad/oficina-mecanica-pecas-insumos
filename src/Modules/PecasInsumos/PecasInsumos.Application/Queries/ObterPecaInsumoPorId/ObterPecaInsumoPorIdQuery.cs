using MediatR;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.ObterPecaInsumoPorId;

public sealed record ObterPecaInsumoPorIdQuery(Guid PecaInsumoId) : IRequest<Result<PecaInsumo>>;
