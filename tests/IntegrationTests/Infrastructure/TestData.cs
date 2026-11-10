namespace IntegrationTests.Infrastructure;

/// <summary>
/// Gerador de fixtures únicas para os testes de integração.
///
/// A coleção "Integration" compartilha um único PostgreSQL por sessão e nunca limpa os dados
/// entre testes. Com fixtures fixas no código, dois testes que usassem o mesmo nome de peça
/// quebravam um ao outro pelo índice único de nome. Cada chamada aqui devolve um valor inédito.
/// </summary>
public static class TestData
{
    /// <summary>
    /// Sufixo curto derivado de <see cref="Guid"/>, usado para tornar nomes e descrições
    /// únicos sem deixá-los ilegíveis no relatório de falha.
    /// </summary>
    public static string Sufixo() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Sufixo curto derivado de <see cref="Guid"/> contendo apenas letras.
    /// </summary>
    public static string SufixoLetras()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        return new string(bytes.Take(8).Select(b => (char)('a' + b % 26)).ToArray());
    }

    /// <summary>
    /// Nome único: o rótulo informado seguido de um sufixo só de letras
    /// (ex.: "Filtro de Óleo kqzmbvxa").
    /// </summary>
    public static string Nome(string rotulo) => $"{rotulo} {SufixoLetras()}";

    /// <summary>
    /// Descrição única: o texto informado seguido de um sufixo curto.
    /// </summary>
    public static string Descricao(string texto) => $"{texto} {Sufixo()}";
}
