using Microsoft.EntityFrameworkCore;
using PecasInsumos.Adapters.DataSources.Records;
using SharedKernel.Application;

namespace PecasInsumos.Infrastructure.Persistence;

public sealed class PecasInsumosDbContext : DbContext, IUnitOfWork
{
    private const string Schema = "pecas_insumos";

    public PecasInsumosDbContext(DbContextOptions<PecasInsumosDbContext> options) : base(options) { }

    public DbSet<PecaInsumoRecord> PecasInsumos => Set<PecaInsumoRecord>();
    public DbSet<MovimentacaoOrcamentoRecord> MovimentacoesOrcamento => Set<MovimentacaoOrcamentoRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PecasInsumosDbContext).Assembly);
    }
}
