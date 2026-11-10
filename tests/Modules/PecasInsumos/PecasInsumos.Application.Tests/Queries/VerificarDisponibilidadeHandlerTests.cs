using Moq;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Application.Queries.VerificarDisponibilidade;
using PecasInsumos.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Queries;

public class VerificarDisponibilidadeHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock = new();
    private readonly VerificarDisponibilidadeHandler _handler;

    public VerificarDisponibilidadeHandlerTests()
    {
        _handler = new VerificarDisponibilidadeHandler(_gatewayMock.Object);
    }

    private void SetupPeca(Guid id, int estoque, decimal preco)
    {
        PecaInsumoEntity peca = PecaInsumoEntity.Criar("Filtro de Óleo", null, preco, estoque, UnidadeDeMedida.Unidade).Value;
        _gatewayMock.Setup(g => g.ObterPorId(new PecaInsumoId(id), It.IsAny<CancellationToken>())).ReturnsAsync(peca);
    }

    [Theory(DisplayName = "Disponibilidade conforme estoque e quantidade solicitada")]
    [InlineData(10, 10, true)]
    [InlineData(10, 3, true)]
    [InlineData(10, 11, false)]
    [InlineData(0, 1, false)]
    public async Task Handle_DeveCompararEstoqueComQuantidade(int estoque, int quantidade, bool esperado)
    {
        var id = Guid.NewGuid();
        SetupPeca(id, estoque, 49.90m);

        var result = await _handler.Handle(new VerificarDisponibilidadeQuery(id, quantidade), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(esperado, result.Value.Disponivel);
        Assert.Equal(49.90m, result.Value.PrecoUnitario);
    }

    [Fact(DisplayName = "Erro: peça inexistente retorna PecaInsumo.NaoEncontrada")]
    public async Task Handle_DeveRetornarNaoEncontrada_QuandoPecaNaoExiste()
    {
        var result = await _handler.Handle(new VerificarDisponibilidadeQuery(Guid.NewGuid(), 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.NaoEncontrada", result.Error.Code);
    }

    [Theory(DisplayName = "Validator: quantidade não positiva é inválida")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_DeveRejeitarQuantidadeNaoPositiva(int quantidade)
    {
        var result = new VerificarDisponibilidadeValidator().Validate(new VerificarDisponibilidadeQuery(Guid.NewGuid(), quantidade));

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Validator: id vazio é inválido")]
    public void Validator_DeveRejeitarIdVazio()
    {
        var result = new VerificarDisponibilidadeValidator().Validate(new VerificarDisponibilidadeQuery(Guid.Empty, 1));

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Validator: dados válidos")]
    public void Validator_DeveAceitarDadosValidos()
    {
        var result = new VerificarDisponibilidadeValidator().Validate(new VerificarDisponibilidadeQuery(Guid.NewGuid(), 2));

        Assert.True(result.IsValid);
    }
}
