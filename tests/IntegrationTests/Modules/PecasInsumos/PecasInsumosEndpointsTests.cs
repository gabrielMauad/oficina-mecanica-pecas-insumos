using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.PecasInsumos;

[Collection("Integration")]
public class PecasInsumosEndpointsTests
{
    private readonly OficinaMecanicaWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PecasInsumosEndpointsTests(OficinaMecanicaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // POST /api/v1/pecas-insumos
    // =========================================================================

    [Fact(DisplayName = "POST /pecas-insumos — retorna 201 com dados da peça criada")]
    public async Task AdicionarPeca_DeveRetornar201_QuandoDadosValidos()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var nome = TestData.Nome("Filtro de Óleo Bosch");

        var command = new
        {
            Nome = nome,
            Descricao = "Filtro de óleo para motor 1.0",
            Preco = 49.90m,
            QuantidadeEmEstoque = 20,
            UnidadeDeMedida = "Unidade"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.PecaInsumoId);
        Assert.Equal(nome, body.Nome);
        Assert.Equal(49.90m, body.PrecoUnitario);
        Assert.Equal(20, body.QuantidadeEmEstoque);
        Assert.Equal("Unidade", body.UnidadeDeMedida);
        Assert.True(body.Ativo);
    }

    // =========================================================================
    // GET /api/v1/pecas-insumos
    // =========================================================================

    [Fact(DisplayName = "GET /pecas-insumos — retorna 200 com lista contendo a peça criada")]
    public async Task ListarPecas_DeveRetornar200_ComListaDeItens()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Criar uma peça para garantir que a lista não estará vazia
        var nome = TestData.Nome("Correia Dentada Gates");

        var command = new
        {
            Nome = nome,
            Descricao = (string?)null,
            Preco = 120.00m,
            QuantidadeEmEstoque = 8,
            UnidadeDeMedida = "Unidade"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);

        // Act
        var response = await client.GetAsync("/api/v1/pecas-insumos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<PecaListItemResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body);

        var peca = body.FirstOrDefault(p => p.Id == created!.PecaInsumoId);
        Assert.NotNull(peca);
        Assert.Equal(nome, peca.Nome);
        Assert.Equal(120.00m, peca.PrecoUnitario);
        Assert.Equal(8, peca.QuantidadeEmEstoque);
    }

    // =========================================================================
    // GET /api/v1/pecas-insumos/{id}
    // =========================================================================

    [Fact(DisplayName = "GET /pecas-insumos/{id} — retorna 200 com dados corretos")]
    public async Task ObterPeca_DeveRetornar200_QuandoPecaExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var nome = TestData.Nome("Pastilha de Freio Fras-le");
        var descricao = TestData.Descricao("Jogo dianteiro");

        var command = new
        {
            Nome = nome,
            Descricao = descricao,
            Preco = 89.90m,
            QuantidadeEmEstoque = 15,
            UnidadeDeMedida = "Unidade"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        var pecaId = created!.PecaInsumoId;

        // Act
        var response = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ObterPecaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(pecaId, body.Id);
        Assert.Equal(nome, body.Nome);
        Assert.Equal(descricao, body.Descricao);
        Assert.Equal(15, body.QuantidadeEmEstoque);
        Assert.Equal(89.90m, body.PrecoUnitario);
        Assert.Equal("Unidade", body.UnidadeDeMedida);
        Assert.True(body.Ativo);
    }

    [Fact(DisplayName = "GET /pecas-insumos/{id} inexistente — retorna 404")]
    public async Task ObterPeca_DeveRetornar404_QuandoNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.GetAsync($"/api/v1/pecas-insumos/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // PATCH /api/v1/pecas-insumos/{id}/descricao
    // =========================================================================

    [Fact(DisplayName = "PATCH /descricao — atualiza descrição e retorna 200")]
    public async Task AtualizarDescricao_DeveRetornar200_ComNovaDescricao()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = TestData.Nome("Vela de Ignição NGK"),
            Descricao = TestData.Descricao("Descrição original"),
            Preco = 35.00m,
            QuantidadeEmEstoque = 12,
            UnidadeDeMedida = "Unidade"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        var pecaId = created!.PecaInsumoId;

        // Act
        var patchContent = JsonContent.Create(new { Descricao = "Descrição atualizada via PATCH" });
        var response = await client.PatchAsync(
            $"/api/v1/pecas-insumos/{pecaId}/descricao", patchContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AtualizarPecaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(pecaId, body.PecaInsumoId);
        Assert.Equal("Descrição atualizada via PATCH", body.Descricao);
    }

    // =========================================================================
    // PATCH /api/v1/pecas-insumos/{id}/preco
    // =========================================================================

    [Fact(DisplayName = "PATCH /preco — atualiza preço e retorna 200")]
    public async Task AtualizarPreco_DeveRetornar200_ComNovoPreco()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = TestData.Nome("Amortecedor Dianteiro Monroe"),
            Descricao = (string?)null,
            Preco = 250.00m,
            QuantidadeEmEstoque = 4,
            UnidadeDeMedida = "Unidade"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        var pecaId = created!.PecaInsumoId;

        // Act
        var patchContent = JsonContent.Create(new { Preco = 299.90m });
        var response = await client.PatchAsync(
            $"/api/v1/pecas-insumos/{pecaId}/preco", patchContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AtualizarPecaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(pecaId, body.PecaInsumoId);
        Assert.Equal(299.90m, body.PrecoUnitario);
    }

    // =========================================================================
    // PATCH /api/v1/pecas-insumos/{id}/estoque/entrada
    // =========================================================================

    [Fact(DisplayName = "PATCH /estoque/entrada — incrementa estoque corretamente")]
    public async Task IncrementarEstoque_DeveAtualizarQuantidade()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = TestData.Nome("Palheta Limpador"),
            Descricao = (string?)null,
            Preco = 25.00m,
            QuantidadeEmEstoque = 5,
            UnidadeDeMedida = "Unidade"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        var pecaId = created!.PecaInsumoId;

        // Act — incrementar em 10 (5 + 10 = 15)
        var patchContent = JsonContent.Create(new { Quantidade = 10 });
        var estoqueResponse = await client.PatchAsync(
            $"/api/v1/pecas-insumos/{pecaId}/estoque/entrada", patchContent);

        // Assert — response direta confirma o novo total
        Assert.Equal(HttpStatusCode.OK, estoqueResponse.StatusCode);

        var body = await estoqueResponse.Content.ReadFromJsonAsync<EstoqueOperacaoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(15, body.QuantidadeEmEstoque);

        // Double-check via GET
        var getResponse = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        var getPeca = await getResponse.Content.ReadFromJsonAsync<ObterPecaResponse>(JsonOptions);
        Assert.Equal(15, getPeca!.QuantidadeEmEstoque);
    }

    // =========================================================================
    // PATCH /api/v1/pecas-insumos/{id}/estoque/saida
    // =========================================================================

    [Fact(DisplayName = "PATCH /estoque/saida — decrementa estoque corretamente")]
    public async Task DecrementarEstoque_DeveAtualizarQuantidade()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = TestData.Nome("Óleo Motul 5W30"),
            Descricao = (string?)null,
            Preco = 45.00m,
            QuantidadeEmEstoque = 20,
            UnidadeDeMedida = "Litro"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        var pecaId = created!.PecaInsumoId;

        // Act — decrementar 7 (20 - 7 = 13)
        var patchContent = JsonContent.Create(new { Quantidade = 7 });
        var estoqueResponse = await client.PatchAsync(
            $"/api/v1/pecas-insumos/{pecaId}/estoque/saida", patchContent);

        // Assert — response direta confirma o novo total
        Assert.Equal(HttpStatusCode.OK, estoqueResponse.StatusCode);

        var body = await estoqueResponse.Content.ReadFromJsonAsync<EstoqueOperacaoResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(13, body.QuantidadeEmEstoque);

        // Double-check via GET
        var getResponse = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        var getPeca = await getResponse.Content.ReadFromJsonAsync<ObterPecaResponse>(JsonOptions);
        Assert.Equal(13, getPeca!.QuantidadeEmEstoque);
    }

    [Fact(DisplayName = "PATCH /estoque/saida — retorna 422 quando quantidade excede o estoque")]
    public async Task DecrementarEstoque_DeveRetornar422_QuandoEstoqueInsuficiente()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = TestData.Nome("Cabo de Vela Bosch"),
            Descricao = (string?)null,
            Preco = 80.00m,
            QuantidadeEmEstoque = 3, // apenas 3 em estoque
            UnidadeDeMedida = "Unidade"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        var pecaId = created!.PecaInsumoId;

        // Act — tentar decrementar 10 (mais do que o disponível)
        var patchContent = JsonContent.Create(new { Quantidade = 10 });
        var response = await client.PatchAsync(
            $"/api/v1/pecas-insumos/{pecaId}/estoque/saida", patchContent);

        // Assert — regra de domínio: estoque não pode ficar negativo
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // =========================================================================
    // DELETE /api/v1/pecas-insumos/{id}
    // =========================================================================

    [Fact(DisplayName = "DELETE /pecas-insumos/{id} — desativa a peça e retorna 204")]
    public async Task DesativarPeca_DeveRetornar204_ERemoverDoListar()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        var command = new
        {
            Nome = TestData.Nome("Rolamento Roda Dianteira SKF"),
            Descricao = (string?)null,
            Preco = 180.00m,
            QuantidadeEmEstoque = 6,
            UnidadeDeMedida = "Unidade"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/pecas-insumos", command);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AdicionarPecaResponse>(JsonOptions);
        var pecaId = created!.PecaInsumoId;

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/v1/pecas-insumos/{pecaId}");

        // Assert — 204 No Content
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // A peça desativada ainda é encontrada via GET (soft delete)
        var getResponse = await client.GetAsync($"/api/v1/pecas-insumos/{pecaId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var peca = await getResponse.Content.ReadFromJsonAsync<ObterPecaResponse>(JsonOptions);
        Assert.NotNull(peca);
        Assert.False(peca.Ativo); // deve estar inativa
    }

    [Fact(DisplayName = "DELETE /pecas-insumos/{id} inexistente — retorna 404")]
    public async Task DesativarPeca_DeveRetornar404_QuandoNaoExiste()
    {
        // Arrange
        var token = await _factory.GetAuthTokenAsync();
        using var client = _factory.CreateAuthenticatedClient(token);

        // Act
        var response = await client.DeleteAsync($"/api/v1/pecas-insumos/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // DTOs locais para desserialização
    // =========================================================================

    // POST /pecas-insumos → "pecaInsumoId"
    private sealed record AdicionarPecaResponse(
        Guid PecaInsumoId,
        string Nome,
        string? Descricao,
        decimal PrecoUnitario,
        int QuantidadeEmEstoque,
        string UnidadeDeMedida,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // GET /pecas-insumos/{id} → "id"
    private sealed record ObterPecaResponse(
        Guid Id,
        string Nome,
        string? Descricao,
        decimal PrecoUnitario,
        int QuantidadeEmEstoque,
        string UnidadeDeMedida,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // GET /pecas-insumos (lista) → "id"
    private sealed record PecaListItemResponse(
        Guid Id,
        string Nome,
        string? Descricao,
        decimal PrecoUnitario,
        int QuantidadeEmEstoque,
        string UnidadeDeMedida,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // PATCH /descricao e PATCH /preco → "pecaInsumoId"
    private sealed record AtualizarPecaResponse(
        Guid PecaInsumoId,
        decimal PrecoUnitario,
        string? Descricao,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);

    // PATCH /estoque/entrada e PATCH /estoque/saida → "pecaInsumoId"
    private sealed record EstoqueOperacaoResponse(
        Guid PecaInsumoId,
        string Nome,
        int QuantidadeEmEstoque,
        bool Ativo,
        DateTime CadastradoEm,
        DateTime AtualizadoEm);
}
