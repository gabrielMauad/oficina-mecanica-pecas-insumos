using Api.Middlewares;
using Microsoft.AspNetCore.Http;

namespace IntegrationTests.Api;

public class CustomExceptionHandlerTests
{
    private sealed class ProblemDetailsServiceFake(bool resultado = true) : IProblemDetailsService
    {
        public ProblemDetailsContext? Recebido { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Recebido = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Recebido = context;
            return ValueTask.FromResult(resultado);
        }
    }

    [Fact(DisplayName = "ArgumentException — responde 400 com o detalhe da exceção")]
    public async Task TryHandle_ArgumentException_Retorna400()
    {
        var problemDetails = new ProblemDetailsServiceFake();
        var handler = new CustomExceptionHandler(problemDetails);
        var httpContext = new DefaultHttpContext();
        var excecao = new ArgumentException("valor inválido");

        var tratado = await handler.TryHandleAsync(httpContext, excecao, CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.NotNull(problemDetails.Recebido);
        Assert.Same(excecao, problemDetails.Recebido.Exception);
        Assert.Same(httpContext, problemDetails.Recebido.HttpContext);
        Assert.Equal("An error occurred", problemDetails.Recebido.ProblemDetails.Title);
        Assert.Equal(nameof(ArgumentException), problemDetails.Recebido.ProblemDetails.Type);
        Assert.Equal("valor inválido", problemDetails.Recebido.ProblemDetails.Detail);
    }

    [Fact(DisplayName = "Exceção inesperada — responde 500 identificando o tipo da exceção")]
    public async Task TryHandle_ExcecaoInesperada_Retorna500()
    {
        var problemDetails = new ProblemDetailsServiceFake();
        var handler = new CustomExceptionHandler(problemDetails);
        var httpContext = new DefaultHttpContext();

        var tratado = await handler.TryHandleAsync(
            httpContext, new InvalidOperationException("falha interna"), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.Equal(nameof(InvalidOperationException), problemDetails.Recebido!.ProblemDetails.Type);
        Assert.Equal("falha interna", problemDetails.Recebido.ProblemDetails.Detail);
    }

    [Fact(DisplayName = "Quando o ProblemDetailsService não escreve a resposta — devolve false")]
    public async Task TryHandle_ServicoNaoEscreve_RetornaFalse()
    {
        var handler = new CustomExceptionHandler(new ProblemDetailsServiceFake(resultado: false));

        var tratado = await handler.TryHandleAsync(
            new DefaultHttpContext(), new Exception("x"), CancellationToken.None);

        Assert.False(tratado);
    }
}
