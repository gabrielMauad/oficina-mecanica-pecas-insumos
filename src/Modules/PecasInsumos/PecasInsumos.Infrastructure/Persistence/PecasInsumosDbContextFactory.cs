using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PecasInsumos.Infrastructure.Persistence;

internal sealed class PecasInsumosDbContextFactory : IDesignTimeDbContextFactory<PecasInsumosDbContext>
{
    public PecasInsumosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        var options = new DbContextOptionsBuilder<PecasInsumosDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PecasInsumosDbContext(options);
    }
}