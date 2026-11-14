using SharedKernel.Domain;

namespace SharedKernel.Domain.Tests;

public class EntityAndAggregateRootTests
{
    private sealed class EntidadeA(Guid id) : Entity<Guid>(id);

    private sealed class EntidadeB(Guid id) : Entity<Guid>(id);

    private sealed record EventoDeTeste : IDomainEvent;

    private sealed class Agregado(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Registrar(IDomainEvent evento) => AddDomainEvent(evento);
    }

    [Fact(DisplayName = "Entity — mesma identidade e mesmo tipo são iguais")]
    public void Entity_MesmoId_SaoIguais()
    {
        var id = Guid.NewGuid();
        var a = new EntidadeA(id);
        var b = new EntidadeA(id);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact(DisplayName = "Entity — a mesma referência é igual a si mesma")]
    public void Entity_MesmaReferencia_EhIgual()
    {
        var a = new EntidadeA(Guid.NewGuid());

        Assert.True(a.Equals(a));
    }

    [Fact(DisplayName = "Entity — identidades diferentes são diferentes")]
    public void Entity_IdsDiferentes_SaoDiferentes()
    {
        var a = new EntidadeA(Guid.NewGuid());
        var b = new EntidadeA(Guid.NewGuid());

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact(DisplayName = "Entity — tipos diferentes com o mesmo id não são iguais")]
    public void Entity_TiposDiferentes_NaoSaoIguais()
    {
        var id = Guid.NewGuid();
        Entity<Guid> a = new EntidadeA(id);
        Entity<Guid> b = new EntidadeB(id);

        Assert.False(a.Equals(b));
    }

    [Fact(DisplayName = "Entity — null e objetos que não são Entity não são iguais; operador == trata null")]
    public void Entity_NuloEOutroTipo_NaoSaoIguais()
    {
        var a = new EntidadeA(Guid.NewGuid());
        EntidadeA? nulo1 = null;
        EntidadeA? nulo2 = null;

        Assert.False(a.Equals(null));
        Assert.False(a.Equals("não é entidade"));
        Assert.True(nulo1 == nulo2);
        Assert.False(nulo1 == a);
    }

    [Fact(DisplayName = "AggregateRoot — acumula eventos de domínio e ClearDomainEvents os remove")]
    public void AggregateRoot_AcumulaELimpaEventos()
    {
        var agregado = new Agregado(Guid.NewGuid());

        agregado.Registrar(new EventoDeTeste());
        agregado.Registrar(new EventoDeTeste());
        Assert.Equal(2, agregado.DomainEvents.Count);

        agregado.ClearDomainEvents();
        Assert.Empty(agregado.DomainEvents);
    }
}
