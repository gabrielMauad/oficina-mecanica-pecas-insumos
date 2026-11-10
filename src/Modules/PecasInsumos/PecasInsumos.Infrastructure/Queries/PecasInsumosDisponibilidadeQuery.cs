using Microsoft.EntityFrameworkCore;
using PecasInsumos.Contracts.Dtos;
using PecasInsumos.Contracts.Queries;
using PecasInsumos.Infrastructure.Persistence;

namespace PecasInsumos.Infrastructure.Queries;

internal sealed class PecasInsumosDisponibilidadeQuery : IPecasInsumosDisponibilidadeQuery
{
    private readonly PecasInsumosDbContext _context;

    public PecasInsumosDisponibilidadeQuery(PecasInsumosDbContext context) => _context = context;

    public async Task<DisponibilidadeDto> VerificarDisponibilidade(Guid pecaId, int quantidade, CancellationToken ct = default)
    {
        var pecaInsumo = await _context.PecasInsumos
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == pecaId, ct);

        if (pecaInsumo is null)
            return new DisponibilidadeDto(false, 0);

        var disponivel = pecaInsumo.QuantidadeEmEstoque >= quantidade;
        return new DisponibilidadeDto(disponivel, pecaInsumo.PrecoUnitario);
    }
}
