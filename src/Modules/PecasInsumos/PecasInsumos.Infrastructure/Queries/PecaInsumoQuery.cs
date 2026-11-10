using Microsoft.EntityFrameworkCore;
using PecasInsumos.Contracts.Dtos;
using PecasInsumos.Contracts.Queries;
using PecasInsumos.Infrastructure.Persistence;

namespace PecasInsumos.Infrastructure.Queries;

internal class PecaInsumoQuery : IPecaInsumoQuery
{
    private readonly PecasInsumosDbContext _context;

    public PecaInsumoQuery(PecasInsumosDbContext context) => _context = context;

    public async Task<PecaInsumoResumoDto?> ObterPorId(Guid pecaId, CancellationToken ct = default)
    {
        var pecaInsumo = await _context.PecasInsumos
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == pecaId, ct);

        if (pecaInsumo is null)
            return null;

        return new PecaInsumoResumoDto(
            pecaInsumo.Id,
            pecaInsumo.Nome,
            pecaInsumo.PrecoUnitario,
            pecaInsumo.UnidadeDeMedida,
            pecaInsumo.Ativo
        );
    }
}
