using PecasInsumos.Adapters.DataSources.Records;
using PecasInsumos.Domain;

namespace PecasInsumos.Adapters.DataSources.Mappers;

internal static class PecaInsumoMapper
{
    public static PecaInsumoRecord ToRecord(PecaInsumo pecaInsumo) => new()
    {
        Id = pecaInsumo.Id.Value,
        Nome = pecaInsumo.Nome,
        Descricao = pecaInsumo.Descricao,
        PrecoUnitario = pecaInsumo.PrecoUnitario.Valor,
        QuantidadeEmEstoque = pecaInsumo.QuantidadeEmEstoque,
        UnidadeDeMedida = pecaInsumo.UnidadeDeMedida.ToString(),
        Ativo = pecaInsumo.Ativo,
        CadastradoEm = pecaInsumo.CadastradoEm,
        AtualizadoEm = pecaInsumo.AtualizadoEm
    };

    public static PecaInsumo ToDomain(PecaInsumoRecord record) => PecaInsumo.Reconstituir(
        new PecaInsumoId(record.Id),
        record.Nome,
        record.Descricao,
        Dinheiro.Reconstituir(record.PrecoUnitario),
        record.QuantidadeEmEstoque,
        Enum.Parse<UnidadeDeMedida>(record.UnidadeDeMedida),
        record.Ativo,
        record.CadastradoEm,
        record.AtualizadoEm);
}
