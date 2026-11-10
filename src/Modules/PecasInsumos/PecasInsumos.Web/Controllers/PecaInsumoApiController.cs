using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PecasInsumos.Adapters.Controllers;
using PecasInsumos.Adapters.Models.Request;

namespace PecasInsumos.Web.Controllers;

[Authorize(Roles = "Oficina")]
[ApiController]
[Route("api/v1/pecas-insumos")]
public class PecaInsumoApiController : ControllerBase
{
    private const string PecaNaoEncontradaErrorCode = "PecaInsumo.NaoEncontrada";

    private readonly PecaInsumoController _caController;

    public PecaInsumoApiController(PecaInsumoController caController) => _caController = caController;

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AdicionarPecaInsumoRequest request)
    {
        var result = await _caController.Criar(request);
        if (result.IsFailure) return UnprocessableEntity(result.Error);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.PecaInsumoId }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var result = await _caController.Listar();
        if (result.IsFailure) return NotFound(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _caController.ObterPorId(id);
        if (result.IsFailure) return NotFound(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id}/disponibilidade")]
    public async Task<IActionResult> VerificarDisponibilidade(Guid id, [FromQuery] int quantidade)
    {
        var result = await _caController.VerificarDisponibilidade(id, quantidade);
        if (result.IsFailure)
            return result.Error.Code == PecaNaoEncontradaErrorCode
                ? NotFound(result.Error)
                : UnprocessableEntity(result.Error);
        return Ok(result.Value);
    }

    [HttpPatch("{id}/descricao")]
    public async Task<IActionResult> AtualizarDescricao(Guid id, [FromBody] AtualizarDescricaoRequest request)
    {
        var result = await _caController.AtualizarDescricao(id, request);
        if (result.IsFailure) return UnprocessableEntity(result.Error);
        return Ok(result.Value);
    }

    [HttpPatch("{id}/preco")]
    public async Task<IActionResult> AtualizarPreco(Guid id, [FromBody] AtualizarPrecoRequest request)
    {
        var result = await _caController.AtualizarPreco(id, request);
        if (result.IsFailure) return UnprocessableEntity(result.Error);
        return Ok(result.Value);
    }

    [HttpPatch("{id}/estoque/entrada")]
    public async Task<IActionResult> IncrementarEstoque(Guid id, [FromBody] EstoqueRequest request)
    {
        var result = await _caController.IncrementarEstoque(id, request);
        if (result.IsFailure) return UnprocessableEntity(result.Error);
        return Ok(result.Value);
    }

    [HttpPatch("{id}/estoque/saida")]
    public async Task<IActionResult> DecrementarEstoque(Guid id, [FromBody] EstoqueRequest request)
    {
        var result = await _caController.DecrementarEstoque(id, request);
        if (result.IsFailure) return UnprocessableEntity(result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var result = await _caController.Desativar(id);
        if (result.IsFailure) return NotFound(result.Error);
        return NoContent();
    }
}
