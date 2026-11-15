namespace PecasInsumos.Adapters.DataSources.Records;

public sealed record ItemMovimentacaoRecord(Guid PecaInsumoId, int Quantidade);

/// <summary>Peça que impediu a movimentação em lote, com o estado observado no banco.</summary>
public sealed record FalhaMovimentacaoRecord(
    Guid PecaInsumoId,
    string? Nome,
    bool Encontrada,
    bool Ativo,
    int QuantidadeEmEstoque);

public sealed record ResultadoMovimentacaoRecord(
    bool JaProcessada,
    IReadOnlyList<FalhaMovimentacaoRecord> Falhas);
