namespace PecasInsumos.Adapters.DataSources.Records;

/// <summary>
/// Registro de que a baixa (ou o estorno) de estoque de um orçamento já foi aplicada. A chave
/// (orçamento, tipo) é única: é ela que impede que a reentrega de um evento movimente o estoque de novo.
/// </summary>
public sealed class MovimentacaoOrcamentoRecord
{
    public Guid OrcamentoId { get; set; }
    public string Tipo { get; set; } = "";
    public DateTime RegistradaEm { get; set; }
}
