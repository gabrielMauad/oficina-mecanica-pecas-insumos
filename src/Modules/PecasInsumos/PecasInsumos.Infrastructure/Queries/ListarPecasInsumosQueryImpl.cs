using Microsoft.EntityFrameworkCore;
using PecasInsumos.Application.Queries.ListarPecasInsumos;
using PecasInsumos.Infrastructure.Persistence;

namespace PecasInsumos.Infrastructure.Queries;

internal class ListarPecasInsumosQueryImpl : IListarPecasInsumosQuery
{
    private readonly PecasInsumosDbContext _context;

    public ListarPecasInsumosQueryImpl(PecasInsumosDbContext context) => _context = context;
    public async Task<List<PecaInsumoListItem>> Listar(CancellationToken ct = default)
    {
        return await _context.PecasInsumos
            .AsNoTracking()
            .Select(s => new PecaInsumoListItem(
                s.Id,
                s.Nome,
                s.Descricao,
                s.PrecoUnitario,
                s.QuantidadeEmEstoque,
                s.UnidadeDeMedida,
                s.Ativo,
                s.CadastradoEm,
                s.AtualizadoEm))
            .ToListAsync(ct);
    }
}
