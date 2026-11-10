using Microsoft.EntityFrameworkCore;
using PecasInsumos.Adapters.DataSources.Records;
using SharedKernel.Application;

namespace PecasInsumos.Infrastructure.Persistence;

public sealed class PecasInsumosDbContext : DbContext, IUnitOfWork
{
    public PecasInsumosDbContext(DbContextOptions<PecasInsumosDbContext> options) : base(options) { }

    public DbSet<PecaInsumoRecord> PecasInsumos => Set<PecaInsumoRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pecas_insumos");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PecasInsumosDbContext).Assembly);
    }
}
