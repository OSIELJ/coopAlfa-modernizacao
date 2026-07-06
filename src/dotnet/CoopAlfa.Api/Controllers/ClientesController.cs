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

    // Regex compilados com timeout — boa prática recomendada pelo SonarQube
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

        // Validação de telefone — if mesclado conforme recomendação SonarQube S1066
        if (!string.IsNullOrEmpty(request.Telefone) && !TelefoneRegex.IsMatch(request.Telefone))
            return BadRequest(ApiResponse<ClienteModel>.Erro(
                "Telefone inválido. Use o formato (XX) XXXXX-XXXX."));

        // Validação de e-mail — if mesclado conforme recomendação SonarQube S1066
        if (!string.IsNullOrEmpty(request.Email) && !EmailRegex.IsMatch(request.Email))
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