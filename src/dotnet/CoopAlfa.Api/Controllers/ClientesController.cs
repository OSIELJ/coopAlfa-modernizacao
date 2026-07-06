using Microsoft.AspNetCore.Mvc;
using CoopAlfa.Api.Models;
using CoopAlfa.Api.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CoopAlfa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ClienteService _service;

    public ClientesController(ClienteService service)
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

        // Validação de telefone: (XX) XXXXX-XXXX ou (XX) XXXX-XXXX
        if (!string.IsNullOrEmpty(request.Telefone))
        {
            var telRegex = @"^\(\d{2}\) \d{4,5}-\d{4}$";
            if (!Regex.IsMatch(request.Telefone, telRegex))
                return BadRequest(ApiResponse<ClienteModel>.Erro(
                    "Telefone inválido. Use o formato (XX) XXXXX-XXXX."));
        }

        // Validação de e-mail
        if (!string.IsNullOrEmpty(request.Email))
        {
            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(request.Email, emailRegex))
                return BadRequest(ApiResponse<ClienteModel>.Erro(
                    "E-mail inválido."));
        }

        var (sucesso, mensagem) = _service.Atualizar(
            codigo, request.Telefone, request.Email);

        if (!sucesso)
            return NotFound(ApiResponse<ClienteModel>.Erro(mensagem));

        // Retorna os dados atualizados
        var cliente = _service.Consultar(codigo);
        return Ok(ApiResponse<ClienteModel>.Ok(cliente!, mensagem));
    }
}
