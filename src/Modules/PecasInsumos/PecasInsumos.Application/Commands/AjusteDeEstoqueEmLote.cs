using PecasInsumos.Application.Gateways;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands;

/// <summary>Desfecho de uma baixa/estorno de orçamento bem-sucedido.</summary>
public enum DesfechoMovimentacaoDoOrcamento
{
    /// <summary>A movimentação foi aplicada a todas as peças.</summary>
    Aplicada,

    /// <summary>O orçamento já tinha essa movimentação registrada; nada foi alterado.</summary>
    JaProcessada,

    /// <summary>Estorno de orçamento sem baixa registrada: só o registro do estorno foi gravado, o estoque não foi tocado.</summary>
    SemBaixaRegistrada
}

/// <summary>
/// Aplica uma movimentação de estoque a todas as peças de um orçamento como uma operação "tudo ou nada"
/// e idempotente por orçamento. A atomicidade e a checagem de saldo ficam no gateway (UPDATE condicional
/// numa única transação), porque ler o saldo, alterar em memória e gravar permite que duas movimentações
/// concorrentes leiam o mesmo saldo e uma sobrescreva a outra. Aqui só se agrupam os itens e se traduz o
/// resultado em erro com o motivo de cada peça problemática.
/// </summary>
internal static class AjusteDeEstoqueEmLote
{
    public static async Task<Result<DesfechoMovimentacaoDoOrcamento>> Aplicar(
        IPecaInsumoGateway gateway,
        Guid orcamentoId,
        TipoMovimentacaoOrcamento tipo,
        IEnumerable<ItemEstoque> itens,
        Error erroBase,
        CancellationToken ct)
    {
        // A mesma peça pode aparecer mais de uma vez no orçamento: soma as quantidades (em long, para
        // não estourar) para que cada peça seja movimentada uma única vez.
        var somas = itens
            .GroupBy(i => i.PecaInsumoId)
            .Select(g => (PecaInsumoId: g.Key, Quantidade: g.Sum(i => (long)i.Quantidade)))
            .ToList();

        var excedentes = somas.Where(s => s.Quantidade > int.MaxValue).ToList();
        if (excedentes.Count > 0)
        {
            return erroBase with
            {
                Message = string.Join(" ", excedentes.Select(e =>
                    $"Peça/insumo {e.PecaInsumoId}: quantidade total do orçamento ultrapassa {int.MaxValue}."))
            };
        }

        var quantidadePorPeca = somas
            .Select(s => new ItemEstoque(s.PecaInsumoId, (int)s.Quantidade))
            .ToList();

        if (quantidadePorPeca.Count == 0)
            return DesfechoMovimentacaoDoOrcamento.Aplicada;

        ResultadoMovimentacaoEmLote resultado =
            await gateway.MovimentarEstoqueDoOrcamento(orcamentoId, tipo, quantidadePorPeca, ct);

        if (resultado.SemBaixaRegistrada)
            return DesfechoMovimentacaoDoOrcamento.SemBaixaRegistrada;

        if (resultado.JaProcessada)
            return DesfechoMovimentacaoDoOrcamento.JaProcessada;

        if (resultado.Falhas.Count > 0)
        {
            var quantidades = quantidadePorPeca.ToDictionary(i => i.PecaInsumoId, i => i.Quantidade);
            return erroBase with
            {
                Message = string.Join(" ", resultado.Falhas.Select(f => Descrever(f, quantidades[f.PecaInsumoId])))
            };
        }

        return DesfechoMovimentacaoDoOrcamento.Aplicada;
    }

    private static string Descrever(FalhaMovimentacao falha, int quantidade)
    {
        string peca = $"Peça/insumo '{falha.Nome}' ({falha.PecaInsumoId})";
        string saldo = $"(quantidade: {quantidade}, em estoque: {falha.QuantidadeEmEstoque})";

        return falha.Motivo switch
        {
            MotivoFalhaMovimentacao.NaoEncontrada => $"Peça/insumo {falha.PecaInsumoId} não encontrada.",
            MotivoFalhaMovimentacao.Desativada => $"{peca} está desativada.",
            MotivoFalhaMovimentacao.EstoqueInsuficiente =>
                $"{peca}: Quantidade em estoque não pode ficar negativa. {saldo}.",
            _ => $"{peca}: Quantidade em estoque não pode ultrapassar {int.MaxValue}. {saldo}."
        };
    }
}
