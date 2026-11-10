using PecasInsumos.Adapters.DataSources;
using PecasInsumos.Adapters.DataSources.Mappers;
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
}
