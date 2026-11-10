namespace PecasInsumos.Adapters.DataSources.Records;

public sealed class PecaInsumoRecord
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEmEstoque { get; set; }
    public string UnidadeDeMedida { get; set; } = "";
    public bool Ativo { get; set; }
    public DateTime CadastradoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
