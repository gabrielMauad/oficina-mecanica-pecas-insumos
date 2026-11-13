using Moq;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;
using PecasInsumos.Application.Gateways;
using PecasInsumos.Domain;
using SharedKernel.Application;
using SharedKernel.Application.Behaviors;
using SharedKernel.Domain;
using PecaInsumoEntity = PecasInsumos.Domain.PecaInsumo;

namespace PecasInsumos.Application.Tests.Commands;

public class BaixarEstoqueDoOrcamentoHandlerTests
{
    private readonly Mock<IPecaInsumoGateway> _gatewayMock = new();
    private readonly BaixarEstoqueDoOrcamentoHandler _handler;

    public BaixarEstoqueDoOrcamentoHandlerTests() => _handler = new BaixarEstoqueDoOrcamentoHandler(_gatewayMock.Object);

    private static PecaInsumoEntity NovaPeca(string nome, int estoque) =>
        PecaInsumoEntity.Criar(nome, null, 10, estoque, UnidadeDeMedida.Unidade).Value;

    private void ExpoePeca(PecaInsumoEntity peca) =>
        _gatewayMock
            .Setup(g => g.ObterPorId(It.Is<PecaInsumoId>(id => id.Value == peca.Id.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(peca);

    [Fact(DisplayName = "Cenário feliz: baixa todas as peças e atualiza cada uma")]
    public async Task Handle_DeveBaixarTodasAsPecas()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        var vela = NovaPeca("Vela", 5);
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(vela.Id.Value, 5)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, filtro.QuantidadeEmEstoque);
        Assert.Equal(0, vela.QuantidadeEmEstoque);
        Assert.Equal(2, result.Value.Count);
        _gatewayMock.Verify(g => g.Atualizar(filtro, It.IsAny<CancellationToken>()), Times.Once);
        _gatewayMock.Verify(g => g.Atualizar(vela, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Segunda peça sem estoque suficiente: falha e nada é atualizado")]
    public async Task Handle_QuandoSegundaPecaSemEstoque_NaoAtualizaNenhuma()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        var vela = NovaPeca("Vela", 2);
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(vela.Id.Value, 4)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PecaInsumo.BaixaDoOrcamentoNaoRealizada", result.Error.Code);
        Assert.Contains("'Vela'", result.Error.Message);
        Assert.Contains("quantidade: 4", result.Error.Message);
        Assert.Contains("em estoque: 2", result.Error.Message);
        Assert.DoesNotContain("Filtro de Óleo", result.Error.Message);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Peça inexistente: falha e nada é atualizado")]
    public async Task Handle_QuandoPecaNaoExiste_NaoAtualizaNenhuma()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        ExpoePeca(filtro);
        var inexistente = Guid.NewGuid();
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(inexistente, 1)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(inexistente.ToString(), result.Error.Message);
        Assert.Contains("não encontrada", result.Error.Message);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Peça desativada: falha e nada é atualizado")]
    public async Task Handle_QuandoPecaDesativada_NaoAtualizaNenhuma()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        var vela = NovaPeca("Vela", 10);
        vela.Desativar();
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 1), new ItemEstoque(vela.Id.Value, 1)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("'Vela'", result.Error.Message);
        Assert.Contains("desativada", result.Error.Message);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Várias peças com problema: o motivo lista todas")]
    public async Task Handle_QuandoVariasPecasComProblema_MotivoListaTodas()
    {
        var filtro = NovaPeca("Filtro de Óleo", 1);
        var vela = NovaPeca("Vela", 1);
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 2), new ItemEstoque(vela.Id.Value, 2)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("'Filtro de Óleo'", result.Error.Message);
        Assert.Contains("'Vela'", result.Error.Message);
    }

    [Fact(DisplayName = "Mesma peça repetida: soma as quantidades e atualiza uma única vez")]
    public async Task Handle_QuandoPecaRepetida_SomaQuantidades()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        ExpoePeca(filtro);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(filtro.Id.Value, 4)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, filtro.QuantidadeEmEstoque);
        _gatewayMock.Verify(g => g.Atualizar(filtro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Mesma peça repetida acima do estoque: falha e nada é atualizado")]
    public async Task Handle_QuandoSomaDaPecaRepetidaExcedeEstoque_Falha()
    {
        var filtro = NovaPeca("Filtro de Óleo", 5);
        ExpoePeca(filtro);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(filtro.Id.Value, 3)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(5, filtro.QuantidadeEmEstoque);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Orçamento sem peças: sucesso sem tocar no estoque")]
    public async Task Handle_QuandoSemPecas_Sucesso()
    {
        var result = await _handler.Handle(new BaixarEstoqueDoOrcamentoCommand([]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        _gatewayMock.Verify(
            g => g.Atualizar(It.IsAny<PecaInsumoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Com o TransactionBehavior: falha não salva nenhum contexto")]
    public async Task ComTransactionBehavior_QuandoFalha_NaoSalva()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        var vela = NovaPeca("Vela", 0);
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(vela.Id.Value, 1)]);
        var unitOfWork = new Mock<IUnitOfWork>();
        var behavior = NovoBehavior<BaixarEstoqueDoOrcamentoCommand, Result<IReadOnlyList<PecaInsumoEntity>>>(unitOfWork);

        var result = await behavior.Handle(command, ct => _handler.Handle(command, ct), CancellationToken.None);

        Assert.True(result.IsFailure);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Com o TransactionBehavior: sucesso salva uma única vez")]
    public async Task ComTransactionBehavior_QuandoSucesso_SalvaUmaVez()
    {
        var filtro = NovaPeca("Filtro de Óleo", 10);
        var vela = NovaPeca("Vela", 5);
        ExpoePeca(filtro);
        ExpoePeca(vela);
        var command = new BaixarEstoqueDoOrcamentoCommand(
            [new ItemEstoque(filtro.Id.Value, 3), new ItemEstoque(vela.Id.Value, 1)]);
        var unitOfWork = new Mock<IUnitOfWork>();
        var behavior = NovoBehavior<BaixarEstoqueDoOrcamentoCommand, Result<IReadOnlyList<PecaInsumoEntity>>>(unitOfWork);

        var result = await behavior.Handle(command, ct => _handler.Handle(command, ct), CancellationToken.None);

        Assert.True(result.IsSuccess);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static TransactionBehavior<TRequest, TResponse> NovoBehavior<TRequest, TResponse>(Mock<IUnitOfWork> unitOfWork)
        where TRequest : ICommand
    {
        var pendentes = new Mock<IPendingIntegrationEvents>();
        pendentes.Setup(p => p.GetPending()).Returns([]);
        var coletor = new Mock<IDomainEventCollector>();
        coletor.Setup(c => c.Coletar()).Returns([]);

        return new TransactionBehavior<TRequest, TResponse>(
            [unitOfWork.Object],
            coletor.Object,
            pendentes.Object,
            new Mock<MediatR.IPublisher>().Object);
    }
}
