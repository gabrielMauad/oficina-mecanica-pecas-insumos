using SharedKernel.Domain;

namespace SharedKernel.Domain.Tests;

public class ErrorAndResultTests
{
    [Fact(DisplayName = "Error — fábricas preservam código e mensagem")]
    public void Error_Fabricas_PreservamCodigoEMensagem()
    {
        Error[] erros =
        [
            Error.Validation("V", "validação"),
            Error.NotFound("N", "não encontrado"),
            Error.Conflict("C", "conflito"),
            Error.Forbidden("F", "proibido")
        ];

        Assert.Equal(["V", "N", "C", "F"], erros.Select(e => e.Code));
        Assert.Equal(["validação", "não encontrado", "conflito", "proibido"], erros.Select(e => e.Message));
    }

    [Fact(DisplayName = "Error.None — é vazio")]
    public void ErrorNone_EhVazio()
    {
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Message);
    }

    [Fact(DisplayName = "Result — sucesso expõe o valor e não tem erro")]
    public void Result_Sucesso_ExpoeValor()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact(DisplayName = "Result — falha expõe o erro e acessar Value lança InvalidOperationException")]
    public void Result_Falha_ExpoeErroELancaAoLerValor()
    {
        var erro = Error.NotFound("X.NaoEncontrado", "não encontrado");
        var result = Result<int>.Failure(erro);

        Assert.True(result.IsFailure);
        Assert.Equal(erro, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact(DisplayName = "Result — conversões implícitas de valor e de erro")]
    public void Result_ConversoesImplicitas()
    {
        Result<string> sucesso = "ok";
        Result<string> falha = Error.Conflict("C", "conflito");

        Assert.True(sucesso.IsSuccess);
        Assert.Equal("ok", sucesso.Value);
        Assert.True(falha.IsFailure);
        Assert.Equal("C", falha.Error.Code);
    }
}
