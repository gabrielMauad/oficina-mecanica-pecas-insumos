using Microsoft.EntityFrameworkCore;
using PecasInsumos.Adapters.DataSources;
using PecasInsumos.Adapters.DataSources.Records;

namespace PecasInsumos.Infrastructure.Persistence;

internal sealed class PecaInsumoRepository : IPecaInsumoRepository
{
    private readonly PecasInsumosDbContext _context;
    public PecaInsumoRepository(PecasInsumosDbContext context) => _context = context;

    public Task Adicionar(PecaInsumoRecord pecaInsumo, CancellationToken ct = default)
    {
        _context.PecasInsumos.Add(pecaInsumo);
        return Task.CompletedTask;
    }

    public Task<PecaInsumoRecord?> ObterPorId(Guid id, CancellationToken ct = default) =>
        _context.PecasInsumos.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistePorNome(string nome, CancellationToken ct = default) =>
        _context.PecasInsumos.AnyAsync(s => s.Nome == nome, ct);

    public Task Atualizar(PecaInsumoRecord pecaInsumo, CancellationToken ct = default)
    {
        var entry = _context.PecasInsumos.Update(pecaInsumo);

        // O saldo foi lido antes e pode estar defasado: só as operações atômicas abaixo o alteram.
        entry.Property(p => p.QuantidadeEmEstoque).IsModified = false;
        return Task.CompletedTask;
    }

    public async Task<bool> IncrementarEstoque(Guid id, int quantidade, CancellationToken ct = default) =>
        await AplicarIncremento(id, quantidade, exigirAtiva: true, ct) > 0;

    public async Task<bool> DecrementarEstoque(Guid id, int quantidade, CancellationToken ct = default) =>
        await AplicarDecremento(id, quantidade, ct) > 0;

    private const string TipoBaixa = "Baixa";
    private const string TipoEstorno = "Estorno";

    public async Task<ResultadoMovimentacaoRecord> MovimentarEmLote(
        Guid orcamentoId,
        string tipo,
        bool incremento,
        IReadOnlyList<ItemMovimentacaoRecord> itens,
        CancellationToken ct = default)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync(ct);

        // Serializa toda movimentação do mesmo orçamento (baixa x estorno x repetições). Baixa e estorno
        // gravam linhas diferentes da chave (orcamento_id, tipo), então a chave única sozinha não os
        // serializa entre si; o lock consultivo, liberado no fim da transação, garante que cada um
        // enxergue o registro confirmado do outro.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({orcamentoId.ToString()}, 0))", ct);

        if (await Registrado(orcamentoId, tipo, ct))
        {
            await transacao.RollbackAsync(ct);
            return new ResultadoMovimentacaoRecord(true, []);
        }

        if (!incremento)
        {
            // Baixa de um orçamento que já foi estornado (o estorno chegou antes, ex.: a baixa falhou e a
            // rejeição foi processada primeiro): não baixa.
            if (await Registrado(orcamentoId, TipoEstorno, ct))
            {
                await transacao.RollbackAsync(ct);
                return new ResultadoMovimentacaoRecord(true, []);
            }
        }
        else if (!await Registrado(orcamentoId, TipoBaixa, ct))
        {
            // Estorno sem baixa registrada: não há o que devolver. Registra só o estorno, para que uma baixa
            // que chegue depois seja ignorada, e não mexe no estoque.
            await Registrar(orcamentoId, tipo, ct);
            await transacao.CommitAsync(ct);
            return new ResultadoMovimentacaoRecord(false, [], SemBaixaRegistrada: true);
        }

        var falhas = new List<FalhaMovimentacaoRecord>();

        // Ordem fixa por peça: duas transações que disputam as mesmas linhas as travam na mesma ordem,
        // o que evita deadlock entre orçamentos que compartilham peças.
        foreach (var item in itens.OrderBy(i => i.PecaInsumoId))
        {
            // O estorno devolve o estoque mesmo que a peça tenha sido desativada depois da baixa.
            int afetadas = incremento
                ? await AplicarIncremento(item.PecaInsumoId, item.Quantidade, exigirAtiva: false, ct)
                : await AplicarDecremento(item.PecaInsumoId, item.Quantidade, ct);

            if (afetadas == 0)
                falhas.Add(await DescreverFalha(item.PecaInsumoId, ct));
        }

        if (falhas.Count > 0)
        {
            // Desfaz as peças já movimentadas neste lote; nada fica registrado.
            await transacao.RollbackAsync(ct);
            return new ResultadoMovimentacaoRecord(false, falhas);
        }

        await Registrar(orcamentoId, tipo, ct);
        await transacao.CommitAsync(ct);
        return new ResultadoMovimentacaoRecord(false, []);
    }

    private Task<bool> Registrado(Guid orcamentoId, string tipo, CancellationToken ct) =>
        _context.MovimentacoesOrcamento
            .AsNoTracking()
            .AnyAsync(m => m.OrcamentoId == orcamentoId && m.Tipo == tipo, ct);

    private Task<int> Registrar(Guid orcamentoId, string tipo, CancellationToken ct) =>
        _context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO pecas_insumos.movimentacao_orcamento (orcamento_id, tipo, registrada_em)
               VALUES ({orcamentoId}, {tipo}, {DateTime.UtcNow})
               ON CONFLICT (orcamento_id, tipo) DO NOTHING",
            ct);

    private Task<int> AplicarIncremento(Guid id, int quantidade, bool exigirAtiva, CancellationToken ct)
    {
        int limite = int.MaxValue - quantidade;
        DateTime agora = DateTime.UtcNow;

        return _context.PecasInsumos
            .Where(p => p.Id == id && (p.Ativo || !exigirAtiva) && p.QuantidadeEmEstoque <= limite)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.QuantidadeEmEstoque, p => p.QuantidadeEmEstoque + quantidade)
                .SetProperty(p => p.AtualizadoEm, agora), ct);
    }

    private Task<int> AplicarDecremento(Guid id, int quantidade, CancellationToken ct)
    {
        DateTime agora = DateTime.UtcNow;

        return _context.PecasInsumos
            .Where(p => p.Id == id && p.Ativo && p.QuantidadeEmEstoque >= quantidade)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.QuantidadeEmEstoque, p => p.QuantidadeEmEstoque - quantidade)
                .SetProperty(p => p.AtualizadoEm, agora), ct);
    }

    private async Task<FalhaMovimentacaoRecord> DescreverFalha(Guid id, CancellationToken ct)
    {
        var peca = await _context.PecasInsumos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Nome, p.Ativo, p.QuantidadeEmEstoque })
            .FirstOrDefaultAsync(ct);

        return peca is null
            ? new FalhaMovimentacaoRecord(id, null, false, false, 0)
            : new FalhaMovimentacaoRecord(id, peca.Nome, true, peca.Ativo, peca.QuantidadeEmEstoque);
    }
}
