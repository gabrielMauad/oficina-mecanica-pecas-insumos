using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OficinaMecanica.Contracts.IntegrationEvents;
using PecasInsumos.Application.Commands;
using PecasInsumos.Application.Commands.BaixarEstoqueDoOrcamento;
using PecasInsumos.Application.Commands.EstornarEstoqueDoOrcamento;
using PecasInsumos.Application.IntegrationEventHandlers;
using SharedKernel.Application;
using SharedKernel.Domain;

namespace PecasInsumos.Application.Tests.IntegrationEventHandlers;

public class OrcamentoEventHandlersTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IIntegrationEventBus> _bus = new();

    private static OrcamentoGeradoIntegrationEvent NovoGerado(params ItemPecaEventDto[] pecas) =>
        new(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), pecas);

    private static OrcamentoRejeitadoIntegrationEvent NovoRejeitado(params ItemPecaEventDto[] pecas) =>
        new(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), pecas);

    private DecrementarEstoqueQuandoOrcamentoGerado NovoHandlerDeBaixa() =>
        new(_sender.Object, _bus.Object, NullLogger<DecrementarEstoqueQuandoOrcamentoGerado>.Instance);

    [Fact(DisplayName = "Baixa: envia um único command com todas as peças do evento")]
    public async Task Baixa_EnviaUmCommandComTodasAsPecas()
    {
        var p1 = new ItemPecaEventDto(Guid.NewGuid(), 2);
        var p2 = new ItemPecaEventDto(Guid.NewGuid(), 5);
        var evento = NovoGerado(p1, p2);
        _sender
            .Setup(s => s.Send(It.IsAny<BaixarEstoqueDoOrcamentoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(DesfechoMovimentacaoDoOrcamento.Aplicada));

        await NovoHandlerDeBaixa().Handle(evento, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<BaixarEstoqueDoOrcamentoCommand>(c =>
                c.OrcamentoId == evento.OrcamentoId &&
                c.Itens.Count == 2 &&
                c.Itens[0] == new ItemEstoque(p1.PecaInsumoId, 2) &&
                c.Itens[1] == new ItemEstoque(p2.PecaInsumoId, 5)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Baixa com sucesso: não publica EstoqueBaixaFalhou")]
    public async Task Baixa_QuandoSucesso_NaoPublica()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<BaixarEstoqueDoOrcamentoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(DesfechoMovimentacaoDoOrcamento.Aplicada));

        await NovoHandlerDeBaixa().Handle(NovoGerado(new ItemPecaEventDto(Guid.NewGuid(), 1)), CancellationToken.None);

        _bus.Verify(
            b => b.Publish(It.IsAny<EstoqueBaixaFalhouIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Baixa com falha: publica EstoqueBaixaFalhou com OS, orçamento e motivo")]
    public async Task Baixa_QuandoFalha_PublicaEstoqueBaixaFalhou()
    {
        var evento = NovoGerado(new ItemPecaEventDto(Guid.NewGuid(), 1));
        var erro = Error.Conflict("PecaInsumo.BaixaDoOrcamentoNaoRealizada", "Peça 'Vela' sem estoque.");
        _sender
            .Setup(s => s.Send(It.IsAny<BaixarEstoqueDoOrcamentoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DesfechoMovimentacaoDoOrcamento>(erro));

        await NovoHandlerDeBaixa().Handle(evento, CancellationToken.None);

        _bus.Verify(b => b.Publish(
            It.Is<EstoqueBaixaFalhouIntegrationEvent>(e =>
                e.OrdemServicoId == evento.OrdemServicoId &&
                e.OrcamentoId == evento.OrcamentoId &&
                e.Motivo == "Peça 'Vela' sem estoque." &&
                e.EventId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Estorno: envia um único command com todas as peças e não publica evento")]
    public async Task Estorno_EnviaUmCommandComTodasAsPecas()
    {
        var p1 = new ItemPecaEventDto(Guid.NewGuid(), 2);
        var p2 = new ItemPecaEventDto(Guid.NewGuid(), 5);
        _sender
            .Setup(s => s.Send(It.IsAny<EstornarEstoqueDoOrcamentoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(DesfechoMovimentacaoDoOrcamento.Aplicada));
        var handler = new IncrementarEstoqueQuandoOrcamentoRejeitado(
            _sender.Object, NullLogger<IncrementarEstoqueQuandoOrcamentoRejeitado>.Instance);

        var evento = NovoRejeitado(p1, p2);

        await handler.Handle(evento, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<EstornarEstoqueDoOrcamentoCommand>(c =>
                c.OrcamentoId == evento.OrcamentoId &&
                c.Itens.Count == 2 &&
                c.Itens[0] == new ItemEstoque(p1.PecaInsumoId, 2) &&
                c.Itens[1] == new ItemEstoque(p2.PecaInsumoId, 5)),
            It.IsAny<CancellationToken>()), Times.Once);
        _bus.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Estorno com falha: apenas registra log, sem lançar exceção")]
    public async Task Estorno_QuandoFalha_NaoLanca()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<EstornarEstoqueDoOrcamentoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DesfechoMovimentacaoDoOrcamento>(
                Error.Conflict("PecaInsumo.EstornoDoOrcamentoNaoRealizado", "falha")));
        var handler = new IncrementarEstoqueQuandoOrcamentoRejeitado(
            _sender.Object, NullLogger<IncrementarEstoqueQuandoOrcamentoRejeitado>.Instance);

        var ex = await Record.ExceptionAsync(() =>
            handler.Handle(NovoRejeitado(new ItemPecaEventDto(Guid.NewGuid(), 1)), CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact(DisplayName = "Baixa repetida (orçamento já baixado): não publica EstoqueBaixaFalhou")]
    public async Task Baixa_QuandoJaProcessada_NaoPublica()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<BaixarEstoqueDoOrcamentoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(DesfechoMovimentacaoDoOrcamento.JaProcessada));

        await NovoHandlerDeBaixa().Handle(NovoGerado(new ItemPecaEventDto(Guid.NewGuid(), 1)), CancellationToken.None);

        _bus.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Estorno repetido (orçamento já estornado): não lança nem publica")]
    public async Task Estorno_QuandoJaProcessado_NaoLancaNemPublica()
    {
        _sender
            .Setup(s => s.Send(It.IsAny<EstornarEstoqueDoOrcamentoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(DesfechoMovimentacaoDoOrcamento.JaProcessada));
        var handler = new IncrementarEstoqueQuandoOrcamentoRejeitado(
            _sender.Object, NullLogger<IncrementarEstoqueQuandoOrcamentoRejeitado>.Instance);

        var ex = await Record.ExceptionAsync(() =>
            handler.Handle(NovoRejeitado(new ItemPecaEventDto(Guid.NewGuid(), 1)), CancellationToken.None));

        Assert.Null(ex);
        _bus.VerifyNoOtherCalls();
    }
}
