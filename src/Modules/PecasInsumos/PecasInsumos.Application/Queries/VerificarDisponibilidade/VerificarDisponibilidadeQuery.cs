using MediatR;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Queries.VerificarDisponibilidade;

public sealed record VerificarDisponibilidadeQuery(Guid PecaInsumoId, int Quantidade)
    : IRequest<Result<DisponibilidadePecaInsumo>>;
