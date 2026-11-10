using Moq;
using PecasInsumos.Application.Queries.ListarPecasInsumos;

namespace PecasInsumos.Application.Tests.Queries;

public class ListarPecasInsumosHandlerTests
{
    private readonly Mock<IListarPecasInsumosQuery> _queryMock;
    private readonly ListarPecasInsumosHandler _handler;

    public ListarPecasInsumosHandlerTests()
    {
        _queryMock = new Mock<IListarPecasInsumosQuery>();
        _handler = new ListarPecasInsumosHandler(_queryMock.Object);
    }

    [Fact(DisplayName = "Cenário feliz")]
    public async Task Handle_ShouldReturnSuccess_WhenQueryIsValid()
    {
        // Arrange
        var query = new ListarPecasInsumosQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _queryMock.Verify(x => x.Listar(It.IsAny<CancellationToken>()), Times.Once);
    }
}
