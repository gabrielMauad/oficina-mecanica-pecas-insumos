using MediatR;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Commands.AdicionarPecaInsumo;

public sealed class AdicionarPecaInsumoHandler : IRequestHandler<AdicionarPecaInsumoCommand, Result<PecaInsumo>>
{
    private readonly IPecaInsumoGateway _gateway;

    public AdicionarPecaInsumoHandler(IPecaInsumoGateway gateway) => _gateway = gateway;

    public async Task<Result<PecaInsumo>> Handle(AdicionarPecaInsumoCommand command, CancellationToken cancellationToken)
    {
        if (await _gateway.ExistePorNome(command.Nome, cancellationToken))
            return PecaInsumoErrors.NomeJaExiste;

        UnidadeDeMedida unidadeDeMedida = Enum.Parse<UnidadeDeMedida>(command.UnidadeDeMedida);
        Result<PecaInsumo> pecaInsumoResult = PecaInsumo.Criar(
            nome: command.Nome,
            descricao: command.Descricao,
            preco: command.Preco,
            quantidadeEmEstoque: command.QuantidadeEmEstoque,
            unidadeDeMedida: unidadeDeMedida
        );
        if (pecaInsumoResult.IsFailure)
            return pecaInsumoResult.Error;

        PecaInsumo pecaInsumo = pecaInsumoResult.Value;

        await _gateway.Adicionar(pecaInsumo, cancellationToken);

        return pecaInsumo;
    }
}
