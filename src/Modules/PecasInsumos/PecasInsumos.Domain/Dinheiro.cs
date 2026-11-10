using SharedKernel.Domain;

namespace PecasInsumos.Domain;

public sealed class Dinheiro : ValueObject
{
    public decimal Valor { get; }

    private Dinheiro(decimal valor) => Valor = valor;

    public static Result<Dinheiro> Criar(decimal valor)
    {
        if (valor < 0)
            return Result<Dinheiro>.Failure(new Error("Dinheiro.Negativo", "Preço não pode ser negativo."));
        return Result<Dinheiro>.Success(new Dinheiro(valor));
    }

    public static Dinheiro Reconstituir(decimal valor) => new(valor);

    protected override IEnumerable<object> GetComponents()
    {
        yield return Valor;
    }
}
