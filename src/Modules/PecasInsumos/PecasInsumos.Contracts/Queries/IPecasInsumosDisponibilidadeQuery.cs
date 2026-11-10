using PecasInsumos.Contracts.Dtos;

namespace PecasInsumos.Contracts.Queries;

public interface IPecasInsumosDisponibilidadeQuery
{
    Task<DisponibilidadeDto> VerificarDisponibilidade(Guid pecaId, int quantidade, CancellationToken ct = default);
}

