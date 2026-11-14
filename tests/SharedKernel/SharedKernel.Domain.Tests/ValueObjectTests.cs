using SharedKernel.Domain;

namespace SharedKernel.Domain.Tests;

public class ValueObjectTests
{
    private sealed class Endereco(string rua, int numero) : ValueObject
    {
        protected override IEnumerable<object?> GetComponents()
        {
            yield return rua;
            yield return numero;
        }
    }

    private sealed class Cep(string valor) : ValueObject
    {
        protected override IEnumerable<object?> GetComponents()
        {
            yield return valor;
        }
    }

    private sealed class Opcional(string? valor) : ValueObject
    {
        protected override IEnumerable<object?> GetComponents()
        {
            yield return valor;
        }
    }

    [Fact(DisplayName = "Equals — objetos de valor com os mesmos componentes são iguais")]
    public void Equals_MesmosComponentes_RetornaTrue()
    {
        var a = new Endereco("Rua A", 10);
        var b = new Endereco("Rua A", 10);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact(DisplayName = "Equals — componentes diferentes tornam os objetos de valor diferentes")]
    public void Equals_ComponentesDiferentes_RetornaFalse()
    {
        var a = new Endereco("Rua A", 10);
        var b = new Endereco("Rua A", 11);

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact(DisplayName = "Equals — tipos diferentes nunca são iguais, mesmo com componentes equivalentes")]
    public void Equals_TiposDiferentes_RetornaFalse()
    {
        ValueObject cep = new Cep("123");
        ValueObject opcional = new Opcional("123");

        Assert.False(cep.Equals(opcional));
    }

    [Fact(DisplayName = "Equals — null e objeto de outro tipo não são iguais")]
    public void Equals_NuloOuOutroTipo_RetornaFalse()
    {
        var a = new Endereco("Rua A", 10);

        Assert.False(a.Equals(null));
        Assert.False(a.Equals("Rua A"));
    }

    [Fact(DisplayName = "Operador == — trata null nos dois lados")]
    public void OperadorIgual_ComNulos_TrataCorretamente()
    {
        Endereco? nulo1 = null;
        Endereco? nulo2 = null;
        var a = new Endereco("Rua A", 10);

        Assert.True(nulo1 == nulo2);
        Assert.False(nulo1 == a);
        Assert.False(a == nulo1);
        Assert.True(a != nulo1);
    }

    [Fact(DisplayName = "GetHashCode — iguais para objetos de valor iguais e diferentes para componentes diferentes")]
    public void GetHashCode_RespeitaIgualdade()
    {
        var a = new Endereco("Rua A", 10);
        var b = new Endereco("Rua A", 10);
        var c = new Endereco("Rua B", 10);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }

    [Fact(DisplayName = "GetHashCode e Equals — componente nulo é suportado")]
    public void ComponenteNulo_EhSuportado()
    {
        var a = new Opcional(null);
        var b = new Opcional(null);
        var c = new Opcional("x");

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }
}
