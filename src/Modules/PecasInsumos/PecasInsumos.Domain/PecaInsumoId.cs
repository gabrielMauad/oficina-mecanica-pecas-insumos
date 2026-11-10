namespace PecasInsumos.Domain;

public sealed record PecaInsumoId(Guid Value)
{
    public static PecaInsumoId Novo() => new(Guid.NewGuid());
}

