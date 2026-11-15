using SharedKernel.Domain;

namespace PecasInsumos.Application;

internal class PecaInsumoErrors
{
    public static readonly Error NomeJaExiste = Error.Conflict(
        "PecaInsumo.NomeJaExiste",
        "Já existe uma peça/insumo cadastrada com este nome.");

    public static readonly Error NaoEncontrada = Error.NotFound(
        "PecaInsumo.NaoEncontrada",
        "Peça/insumo não encontrada.");

    public static readonly Error JaDesativado = Error.Conflict(
        "PecaInsumo.JaDesativada",
        "A peça/insumo já está desativada.");

    public static readonly Error EstoqueAlteradoPorOutraOperacao = Error.Conflict(
        "PecaInsumo.EstoqueAlteradoPorOutraOperacao",
        "O estoque da peça/insumo foi alterado por outra operação e não comporta mais esta movimentação. Consulte o saldo atual e tente novamente.");
}

