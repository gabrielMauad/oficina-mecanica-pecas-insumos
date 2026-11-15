using PecasInsumos.Adapters.DataSources.Records;

namespace PecasInsumos.Adapters.DataSources;

public interface IPecaInsumoRepository
{
    Task Adicionar(PecaInsumoRecord pecaInsumo, CancellationToken ct = default);
    Task<PecaInsumoRecord?> ObterPorId(Guid id, CancellationToken ct = default);
    Task<bool> ExistePorNome(string nome, CancellationToken ct = default);

    /// <summary>Marca os dados cadastrais como alterados; nunca grava a quantidade em estoque.</summary>
    Task Atualizar(PecaInsumoRecord pecaInsumo, CancellationToken ct = default);

    /// <summary>UPDATE condicional (peça ativa e sem ultrapassar o limite do saldo), executado de imediato.</summary>
    Task<bool> IncrementarEstoque(Guid id, int quantidade, CancellationToken ct = default);

    /// <summary>UPDATE condicional (peça ativa e saldo suficiente), executado de imediato.</summary>
    Task<bool> DecrementarEstoque(Guid id, int quantidade, CancellationToken ct = default);

    /// <summary>
    /// Registra a movimentação do orçamento e aplica todos os itens numa única transação: ou todos são
    /// aplicados, ou nenhum (inclusive o registro). Se o orçamento já tinha a movimentação registrada,
    /// não altera nada.
    /// </summary>
    Task<ResultadoMovimentacaoRecord> MovimentarEmLote(
        Guid orcamentoId,
        string tipo,
        bool incremento,
        IReadOnlyList<ItemMovimentacaoRecord> itens,
        CancellationToken ct = default);
}
