using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands;

/// <summary>
/// Aplica uma movimentação de estoque a várias peças como uma operação "tudo ou nada": carrega todas,
/// aplica a operação de domínio a todas em memória e só então entrega as peças alteradas ao gateway.
/// Se qualquer peça não existir, estiver desativada ou rejeitar a operação, nenhuma é entregue ao
/// gateway e o resultado é uma falha com o motivo de cada peça problemática. A persistência fica a cargo
/// do <c>TransactionBehavior</c>, que salva uma única vez e apenas quando o resultado é sucesso.
/// </summary>
internal static class AjusteDeEstoqueEmLote
{
    public static async Task<Result<IReadOnlyList<PecaInsumo>>> Aplicar(
        IPecaInsumoGateway gateway,
        IEnumerable<ItemEstoque> itens,
        Func<PecaInsumo, int, Result<PecaInsumo>> operacao,
        Error erroBase,
        CancellationToken ct)
    {
        // A mesma peça pode aparecer mais de uma vez no orçamento: soma as quantidades para que cada
        // peça seja carregada, validada e atualizada uma única vez.
        var quantidadePorPeca = itens
            .GroupBy(i => i.PecaInsumoId)
            .Select(g => new ItemEstoque(g.Key, g.Sum(i => i.Quantidade)))
            .ToList();

        var alteradas = new List<PecaInsumo>(quantidadePorPeca.Count);
        var problemas = new List<string>();

        foreach (var item in quantidadePorPeca)
        {
            PecaInsumo? peca = await gateway.ObterPorId(new PecaInsumoId(item.PecaInsumoId), ct);

            if (peca is null)
            {
                problemas.Add($"Peça/insumo {item.PecaInsumoId} não encontrada.");
                continue;
            }

            if (!peca.Ativo)
            {
                problemas.Add($"Peça/insumo '{peca.Nome}' ({item.PecaInsumoId}) está desativada.");
                continue;
            }

            int estoqueAntes = peca.QuantidadeEmEstoque;
            Result<PecaInsumo> resultado = operacao(peca, item.Quantidade);
            if (resultado.IsFailure)
            {
                problemas.Add(
                    $"Peça/insumo '{peca.Nome}' ({item.PecaInsumoId}): {resultado.Error.Message} " +
                    $"(quantidade: {item.Quantidade}, em estoque: {estoqueAntes}).");
                continue;
            }

            alteradas.Add(resultado.Value);
        }

        if (problemas.Count > 0)
            return erroBase with { Message = string.Join(" ", problemas) };

        foreach (var peca in alteradas)
            await gateway.Atualizar(peca, ct);

        return alteradas;
    }
}
