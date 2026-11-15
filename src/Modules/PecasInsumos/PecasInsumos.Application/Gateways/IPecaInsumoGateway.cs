using PecasInsumos.Application.Commands;
using PecasInsumos.Domain;

namespace PecasInsumos.Application.Gateways;

public interface IPecaInsumoGateway
{
    Task Adicionar(PecaInsumo pecaInsumo, CancellationToken ct = default);
    Task<PecaInsumo?> ObterPorId(PecaInsumoId id, CancellationToken ct = default);
    Task<bool> ExistePorNome(string nome, CancellationToken ct = default);

    /// <summary>
    /// Persiste os dados cadastrais da peça (nome, descrição, preço, situação). Não altera a quantidade em
    /// estoque: o saldo só muda pelas operações atômicas abaixo, para que uma gravação feita com um saldo
    /// lido antes não desfaça movimentações concorrentes.
    /// </summary>
    Task Atualizar(PecaInsumo pecaInsumo, CancellationToken ct = default);

    /// <summary>
    /// Soma <paramref name="quantidade"/> ao saldo em uma única operação condicional no banco. Retorna
    /// <c>false</c> (sem alterar nada) se a peça não existe, está desativada ou o saldo ultrapassaria o limite.
    /// </summary>
    Task<bool> IncrementarEstoque(PecaInsumoId id, int quantidade, CancellationToken ct = default);

    /// <summary>
    /// Subtrai <paramref name="quantidade"/> do saldo em uma única operação condicional no banco. Retorna
    /// <c>false</c> (sem alterar nada) se a peça não existe, está desativada ou o saldo ficaria negativo.
    /// </summary>
    Task<bool> DecrementarEstoque(PecaInsumoId id, int quantidade, CancellationToken ct = default);

    /// <summary>
    /// Aplica a baixa ou o estorno de todas as peças de um orçamento em uma única transação "tudo ou nada",
    /// registrando o orçamento para que a mesma movimentação não seja aplicada duas vezes.
    /// </summary>
    Task<ResultadoMovimentacaoEmLote> MovimentarEstoqueDoOrcamento(
        Guid orcamentoId,
        TipoMovimentacaoOrcamento tipo,
        IReadOnlyList<ItemEstoque> itens,
        CancellationToken ct = default);
}

public enum TipoMovimentacaoOrcamento
{
    Baixa,
    Estorno
}

public enum MotivoFalhaMovimentacao
{
    NaoEncontrada,
    Desativada,
    EstoqueInsuficiente,
    LimiteDeEstoque
}

/// <summary>Peça que impediu a movimentação em lote, com o saldo observado no momento da falha.</summary>
public sealed record FalhaMovimentacao(
    Guid PecaInsumoId,
    string? Nome,
    MotivoFalhaMovimentacao Motivo,
    int QuantidadeEmEstoque);

/// <summary>
/// Resultado de uma movimentação em lote. <see cref="JaProcessada"/> indica que o orçamento já tinha essa
/// movimentação registrada (reentrega do evento): nada foi alterado. Com <see cref="Falhas"/> não vazio,
/// nenhuma peça foi alterada.
/// </summary>
public sealed record ResultadoMovimentacaoEmLote(bool JaProcessada, IReadOnlyList<FalhaMovimentacao> Falhas);
