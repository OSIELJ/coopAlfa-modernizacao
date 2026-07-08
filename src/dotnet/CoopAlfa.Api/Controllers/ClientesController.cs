using Microsoft.AspNetCore.Mvc;
using CoopAlfa.Api.Models;
using CoopAlfa.Api.Services;
using System.Text.RegularExpressions;

namespace CoopAlfa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;

    private static readonly Regex TelefoneRegex = new(
        @"^\(\d{2}\) \d{4,5}-\d{4}$",
        RegexOptions.None,
        TimeSpan.FromSeconds(1));

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.None,
        TimeSpan.FromSeconds(1));

    public ClientesController(IClienteService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consulta um cliente pelo código.
    /// GET /api/clientes/{codigo}
    /// </summary>
    [HttpGet("{codigo:int}")]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 400)]
    public IActionResult Consultar(int codigo)
    {
        if (codigo <= 0 || codigo > 9999)
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Código inválido. Deve ser um número entre 1 e 9999."));

        var cliente = _service.Consultar(codigo);

        if (cliente is null)
            return NotFound(ApiResponse<ClienteModel>.Erro(
                $"Cliente com código {codigo} não encontrado."));

        return Ok(ApiResponse<ClienteModel>.Ok(cliente));
    }

    /// <summary>
    /// Cadastra um novo cliente.
    /// POST /api/clientes
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 201)]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 400)]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 409)]
    public IActionResult Cadastrar([FromBody] CadastrarClienteRequest request)
    {
        if (request.Codigo <= 0 || request.Codigo > 9999)
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Código inválido. Deve ser um número entre 1 e 9999."));

        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Nome é obrigatório."));

        if (request.Nome.Length > 40)
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Nome deve ter no máximo 40 caracteres."));

        if (!string.IsNullOrEmpty(request.Telefone) &&
            !TelefoneRegex.IsMatch(request.Telefone))
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Telefone inválido. Use o formato (XX) XXXXX-XXXX."));

        if (!string.IsNullOrEmpty(request.Email) &&
            !EmailRegex.IsMatch(request.Email))
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "E-mail inválido."));

        var (sucesso, mensagem) = _service.Cadastrar(
            request.Codigo, request.Nome, request.Telefone, request.Email);

        if (!sucesso)
            return Conflict(ApiResponse<ClienteModel>.Erro(mensagem));

        var cliente = _service.Consultar(request.Codigo);
        return CreatedAtAction(nameof(Consultar),
            new { codigo = request.Codigo },
            ApiResponse<ClienteModel>.Ok(cliente!, mensagem));
    }

    /// <summary>
    /// Atualiza telefone e e-mail de um cliente.
    /// PUT /api/clientes/{codigo}/contato
    /// </summary>
    [HttpPut("{codigo:int}/contato")]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 200)]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 404)]
    [ProducesResponseType(typeof(ApiResponse<ClienteModel>), 400)]
    public IActionResult AtualizarContato(int codigo,
        [FromBody] AtualizarContatoRequest request)
    {
        if (codigo <= 0 || codigo > 9999)
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Código inválido. Deve ser um número entre 1 e 9999."));

        if (!string.IsNullOrEmpty(request.Telefone) &&
            !TelefoneRegex.IsMatch(request.Telefone))
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Telefone inválido. Use o formato (XX) XXXXX-XXXX."));

        if (!string.IsNullOrEmpty(request.Email) &&
            !EmailRegex.IsMatch(request.Email))
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "E-mail inválido."));

        var (sucesso, mensagem) = _service.Atualizar(
            codigo, request.Telefone, request.Email);

        if (!sucesso)
            return NotFound(ApiResponse<ClienteModel>.Erro(mensagem));

        var cliente = _service.Consultar(codigo);
        return Ok(ApiResponse<ClienteModel>.Ok(cliente!, mensagem));
    }
}
