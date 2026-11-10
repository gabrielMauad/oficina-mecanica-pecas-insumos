using PecasInsumos.Adapters.Models.ViewModels;
using PecasInsumos.Application.Queries.ListarPecasInsumos;
using PecasInsumos.Application.Queries.VerificarDisponibilidade;
using PecasInsumos.Domain;

namespace PecasInsumos.Adapters.Presenters;

public static class PecaInsumoPresenter
{
    public static AdicionarPecaInsumoViewModel PresentAdicionar(PecaInsumo entity) =>
        new(
            entity.Id.Value,
            entity.Nome,
            entity.Descricao,
            entity.PrecoUnitario.Valor,
            entity.QuantidadeEmEstoque,
            entity.UnidadeDeMedida.ToString(),
            entity.Ativo,
            entity.CadastradoEm,
            entity.AtualizadoEm);

    public static AtualizarPecaInsumoViewModel PresentAtualizar(PecaInsumo entity) =>
        new(
            entity.Id.Value,
            entity.PrecoUnitario.Valor,
            entity.Descricao,
            entity.Ativo,
            entity.CadastradoEm,
            entity.AtualizadoEm);

    public static IncrementarEstoqueViewModel PresentIncrementar(PecaInsumo entity) =>
        new(
            entity.Id.Value,
            entity.Nome,
            entity.QuantidadeEmEstoque,
            entity.Ativo,
            entity.CadastradoEm,
            entity.AtualizadoEm);

    public static DecrementarEstoqueViewModel PresentDecrementar(PecaInsumo entity) =>
        new(
            entity.Id.Value,
            entity.Nome,
            entity.QuantidadeEmEstoque,
            entity.Ativo,
            entity.CadastradoEm,
            entity.AtualizadoEm);

    public static DesativarPecaInsumoViewModel PresentDesativar(PecaInsumo entity) =>
        new(
            entity.Id.Value,
            entity.Nome,
            entity.Ativo,
            entity.CadastradoEm,
            entity.AtualizadoEm);

    public static ObterPecaInsumoPorIdViewModel PresentObterPorId(PecaInsumo entity) =>
        new(
            entity.Id.Value,
            entity.Nome,
            entity.Descricao,
            entity.PrecoUnitario.Valor,
            entity.QuantidadeEmEstoque,
            entity.UnidadeDeMedida.ToString(),
            entity.Ativo,
            entity.CadastradoEm,
            entity.AtualizadoEm);

    public static DisponibilidadeViewModel PresentDisponibilidade(DisponibilidadePecaInsumo disponibilidade) =>
        new(disponibilidade.Disponivel, disponibilidade.PrecoUnitario);

    public static List<PecaInsumoListItemViewModel> PresentListar(List<PecaInsumoListItem> items) =>
        items.Select(i => new PecaInsumoListItemViewModel(
            i.Id,
            i.Nome,
            i.Descricao,
            i.PrecoUnitario,
            i.QuantidadeEmEstoque,
            i.UnidadeDeMedida,
            i.Ativo,
            i.CadastradoEm,
            i.AtualizadoEm)).ToList();
}
