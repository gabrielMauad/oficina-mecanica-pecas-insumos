using MediatR;
using PecasInsumos.Adapters.Models.Request;
using PecasInsumos.Adapters.Models.ViewModels;
using PecasInsumos.Adapters.Presenters;
using PecasInsumos.Application.Commands.AdicionarPecaInsumo;
using PecasInsumos.Application.Commands.AtualizarPecaInsumo;
using PecasInsumos.Application.Commands.DecrementarEstoque;
using PecasInsumos.Application.Commands.DesativarPecaInsumo;
using PecasInsumos.Application.Commands.IncrementarEstoque;
using PecasInsumos.Application.Queries.ListarPecasInsumos;
using PecasInsumos.Application.Queries.ObterPecaInsumoPorId;
using PecasInsumos.Application.Queries.VerificarDisponibilidade;
using SharedKernel.Domain;

namespace PecasInsumos.Adapters.Controllers;

public sealed class PecaInsumoController
{
    private readonly ISender _sender;

    public PecaInsumoController(ISender sender) => _sender = sender;

    public async Task<Result<AdicionarPecaInsumoViewModel>> Criar(AdicionarPecaInsumoRequest request)
    {
        var command = new AdicionarPecaInsumoCommand(
            request.Nome,
            request.Descricao,
            request.Preco,
            request.QuantidadeEmEstoque,
            request.UnidadeDeMedida);
        var result = await _sender.Send(command);
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentAdicionar(result.Value);
    }

    public async Task<Result<List<PecaInsumoListItemViewModel>>> Listar()
    {
        var result = await _sender.Send(new ListarPecasInsumosQuery());
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentListar(result.Value);
    }

    public async Task<Result<ObterPecaInsumoPorIdViewModel>> ObterPorId(Guid id)
    {
        var result = await _sender.Send(new ObterPecaInsumoPorIdQuery(id));
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentObterPorId(result.Value);
    }

    public async Task<Result<DisponibilidadeViewModel>> VerificarDisponibilidade(Guid id, int quantidade)
    {
        var result = await _sender.Send(new VerificarDisponibilidadeQuery(id, quantidade));
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentDisponibilidade(result.Value);
    }

    public async Task<Result<AtualizarPecaInsumoViewModel>> AtualizarDescricao(Guid id, AtualizarDescricaoRequest request)
    {
        var command = new AtualizarPecaInsumoCommand(id, request.Descricao, null);
        var result = await _sender.Send(command);
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentAtualizar(result.Value);
    }

    public async Task<Result<AtualizarPecaInsumoViewModel>> AtualizarPreco(Guid id, AtualizarPrecoRequest request)
    {
        var command = new AtualizarPecaInsumoCommand(id, null, request.Preco);
        var result = await _sender.Send(command);
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentAtualizar(result.Value);
    }

    public async Task<Result<IncrementarEstoqueViewModel>> IncrementarEstoque(Guid id, EstoqueRequest request)
    {
        var command = new IncrementarEstoqueCommand(id, request.Quantidade);
        var result = await _sender.Send(command);
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentIncrementar(result.Value);
    }

    public async Task<Result<DecrementarEstoqueViewModel>> DecrementarEstoque(Guid id, EstoqueRequest request)
    {
        var command = new DecrementarEstoqueCommand(id, request.Quantidade);
        var result = await _sender.Send(command);
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentDecrementar(result.Value);
    }

    public async Task<Result<DesativarPecaInsumoViewModel>> Desativar(Guid id)
    {
        var command = new DesativarPecaInsumoCommand(id);
        var result = await _sender.Send(command);
        if (result.IsFailure) return result.Error;
        return PecaInsumoPresenter.PresentDesativar(result.Value);
    }
}
