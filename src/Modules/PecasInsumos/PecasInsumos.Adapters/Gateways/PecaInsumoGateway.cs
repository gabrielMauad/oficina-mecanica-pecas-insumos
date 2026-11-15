using PecasInsumos.Adapters.DataSources;
using PecasInsumos.Adapters.DataSources.Mappers;
using PecasInsumos.Adapters.DataSources.Records;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Application;

namespace PecasInsumos.Adapters.Gateways;

public sealed class PecaInsumoGateway : IPecaInsumoGateway
{
    private readonly IPecaInsumoRepository _repository;
    private readonly IDomainEventCollector _collector;

    public PecaInsumoGateway(IPecaInsumoRepository repository, IDomainEventCollector collector)
    {
        _repository = repository;
        _collector = collector;
    }

    public Task Adicionar(PecaInsumo pecaInsumo, CancellationToken ct = default)
    {
        _collector.Registrar(pecaInsumo);
        return _repository.Adicionar(PecaInsumoMapper.ToRecord(pecaInsumo), ct);
    }

    public async Task<PecaInsumo?> ObterPorId(PecaInsumoId id, CancellationToken ct = default)
    {
        var record = await _repository.ObterPorId(id.Value, ct);
        return record is null ? null : PecaInsumoMapper.ToDomain(record);
    }

    public Task<bool> ExistePorNome(string nome, CancellationToken ct = default) =>
        _repository.ExistePorNome(nome, ct);

    public Task Atualizar(PecaInsumo pecaInsumo, CancellationToken ct = default)
    {
        _collector.Registrar(pecaInsumo);
        return _repository.Atualizar(PecaInsumoMapper.ToRecord(pecaInsumo), ct);
    }

    public Task<bool> IncrementarEstoque(PecaInsumoId id, int quantidade, CancellationToken ct = default) =>
        _repository.IncrementarEstoque(id.Value, quantidade, ct);

    public Task<bool> DecrementarEstoque(PecaInsumoId id, int quantidade, CancellationToken ct = default) =>
        _repository.DecrementarEstoque(id.Value, quantidade, ct);

    public async Task<ResultadoMovimentacaoEmLote> MovimentarEstoqueDoOrcamento(
        Guid orcamentoId,
        TipoMovimentacaoOrcamento tipo,
        IReadOnlyList<ItemEstoque> itens,
        CancellationToken ct = default)
    {
        bool incremento = tipo == TipoMovimentacaoOrcamento.Estorno;
        var itensRecord = itens.Select(i => new ItemMovimentacaoRecord(i.PecaInsumoId, i.Quantidade)).ToList();

        ResultadoMovimentacaoRecord resultado =
            await _repository.MovimentarEmLote(orcamentoId, tipo.ToString(), incremento, itensRecord, ct);

        return new ResultadoMovimentacaoEmLote(
            resultado.JaProcessada,
            resultado.Falhas.Select(f => new FalhaMovimentacao(
                f.PecaInsumoId,
                f.Nome,
                MotivoDa(f, incremento),
                f.QuantidadeEmEstoque)).ToList());
    }

    private static MotivoFalhaMovimentacao MotivoDa(FalhaMovimentacaoRecord falha, bool incremento) =>
        !falha.Encontrada ? MotivoFalhaMovimentacao.NaoEncontrada
        : !falha.Ativo ? MotivoFalhaMovimentacao.Desativada
        : incremento ? MotivoFalhaMovimentacao.LimiteDeEstoque
        : MotivoFalhaMovimentacao.EstoqueInsuficiente;
}
