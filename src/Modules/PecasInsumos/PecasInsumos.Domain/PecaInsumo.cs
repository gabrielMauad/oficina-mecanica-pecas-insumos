using SharedKernel.Domain;

namespace PecasInsumos.Domain;

public sealed class PecaInsumo : AggregateRoot<PecaInsumoId>
{
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public Dinheiro PrecoUnitario { get; private set; }
    public int QuantidadeEmEstoque { get; private set; }
    public UnidadeDeMedida UnidadeDeMedida { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CadastradoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private PecaInsumo(
        PecaInsumoId id,
        string nome,
        string? descricao,
        Dinheiro precoUnitario,
        int quantidadeEmEstoque,
        UnidadeDeMedida unidadeDeMedida
    )
    : base(id)
    {
        Nome = nome;
        Descricao = descricao;
        PrecoUnitario = precoUnitario;
        QuantidadeEmEstoque = quantidadeEmEstoque;
        UnidadeDeMedida = unidadeDeMedida;
        Ativo = true;
        CadastradoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    private PecaInsumo(
        PecaInsumoId id,
        string nome,
        string? descricao,
        Dinheiro precoUnitario,
        int quantidadeEmEstoque,
        UnidadeDeMedida unidadeDeMedida,
        bool ativo,
        DateTime cadastradoEm,
        DateTime atualizadoEm
    )
    : base(id)
    {
        Nome = nome;
        Descricao = descricao;
        PrecoUnitario = precoUnitario;
        QuantidadeEmEstoque = quantidadeEmEstoque;
        UnidadeDeMedida = unidadeDeMedida;
        Ativo = ativo;
        CadastradoEm = cadastradoEm;
        AtualizadoEm = atualizadoEm;
    }

    public static PecaInsumo Reconstituir(
        PecaInsumoId id,
        string nome,
        string? descricao,
        Dinheiro precoUnitario,
        int quantidadeEmEstoque,
        UnidadeDeMedida unidadeDeMedida,
        bool ativo,
        DateTime cadastradoEm,
        DateTime atualizadoEm) =>
        new(id, nome, descricao, precoUnitario, quantidadeEmEstoque, unidadeDeMedida, ativo, cadastradoEm, atualizadoEm);

    public static Result<PecaInsumo> Criar(string nome, string? descricao, decimal preco, int quantidadeEmEstoque, UnidadeDeMedida unidadeDeMedida)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Error.Validation("PecaInsumo.NomeVazio", "Nome é obrigatório.");
        if (quantidadeEmEstoque < 0)
            return Error.Validation("PecaInsumo.QuantidadeInvalida", "Quantidade em estoque não pode ser negativa.");
        Result<Dinheiro> precoUnitarioResult = Dinheiro.Criar(preco);
        if (precoUnitarioResult.IsFailure)
            return precoUnitarioResult.Error;
        Dinheiro precoUnitario = precoUnitarioResult.Value;

        var pecaInsumo = new PecaInsumo(PecaInsumoId.Novo(), nome, descricao, precoUnitario, quantidadeEmEstoque, unidadeDeMedida);

        return pecaInsumo;
    }

    public Result<PecaInsumo> Incrementar(int quantidade)
    {
        if (quantidade <= 0)
            return Error.Validation("PecaInsumo.QuantidadeInvalida", "Quantidade a incrementar deve ser positiva.");
        QuantidadeEmEstoque += quantidade;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<PecaInsumo> Decrementar(int quantidade)
    {
        if (quantidade <= 0)
            return Error.Validation("PecaInsumo.QuantidadeInvalida", "Quantidade a decrementar deve ser positiva.");
        int novaQuantidade = QuantidadeEmEstoque - quantidade;
        if (novaQuantidade < 0)
            return Error.Validation("PecaInsumo.EstoqueInsuficiente", "Quantidade em estoque não pode ficar negativa.");
        QuantidadeEmEstoque = novaQuantidade;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<PecaInsumo> AtualizarPrecoUnitario(decimal novoPreco)
    {
        Result<Dinheiro> precoBaseResult = Dinheiro.Criar(novoPreco);
        if (precoBaseResult.IsFailure)
            return precoBaseResult.Error;
        PrecoUnitario = precoBaseResult.Value;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }
    public Result<PecaInsumo> AtualizarDescricao(string novaDescricao)
    {
        Descricao = novaDescricao;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public void Desativar()
    {
        Ativo = false;
        AtualizadoEm = DateTime.UtcNow;
    }
}
