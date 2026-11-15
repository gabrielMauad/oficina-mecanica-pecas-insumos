using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PecasInsumos.Adapters.DataSources.Records;

namespace PecasInsumos.Infrastructure.Persistence;

internal sealed class MovimentacaoOrcamentoConfiguration : IEntityTypeConfiguration<MovimentacaoOrcamentoRecord>
{

    public void Configure(EntityTypeBuilder<MovimentacaoOrcamentoRecord> builder)
    {
        builder.ToTable("movimentacao_orcamento");

        // Um orçamento tem no máximo uma baixa e um estorno aplicados: a chave composta é a garantia de
        // idempotência, mesmo com consumidores concorrentes.
        builder.HasKey(m => new { m.OrcamentoId, m.Tipo });

        builder.Property(m => m.OrcamentoId)
            .HasColumnName("orcamento_id");

        builder.Property(m => m.Tipo)
            .HasColumnName("tipo")
            .HasMaxLength(20);

        builder.Property(m => m.RegistradaEm)
            .HasColumnName("registrada_em")
            .IsRequired();
    }
}
