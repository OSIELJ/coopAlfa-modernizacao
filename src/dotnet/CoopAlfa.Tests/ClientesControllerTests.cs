using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using CoopAlfa.Api.Controllers;
using CoopAlfa.Api.Models;
using CoopAlfa.Api.Services;

namespace CoopAlfa.Tests;

public class ClientesControllerTests
{
    private readonly Mock<IClienteService> _serviceMock;
    private readonly ClientesController _controller;

    public ClientesControllerTests()
    {
        _serviceMock = new Mock<IClienteService>();
        _controller  = new ClientesController(_serviceMock.Object);
    }

    // ── CONSULTAR ─────────────────────────────────────────────────────────

    [Fact]
    public void Consultar_CodigoValido_ClienteExiste_RetornaOk()
    {
        // Arrange
        var cliente = new ClienteModel
        {
            Codigo   = 1001,
            Nome     = "Maria Silva",
            Telefone = "(11) 99999-1234",
            Email    = "maria@email.com"
        };
        _serviceMock.Setup(s => s.Consultar(1001)).Returns(cliente);

        // Act
        var result = _controller.Consultar(1001) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var response = result.Value as ApiResponse<ClienteModel>;
        Assert.NotNull(response);
        Assert.True(response.Sucesso);
        Assert.Equal("Maria Silva", response.Dados!.Nome);
    }

    [Fact]
    public void Consultar_ClienteNaoExiste_RetornaNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.Consultar(9999)).Returns((ClienteModel?)null);

        // Act
        var result = _controller.Consultar(9999) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
        var response = result.Value as ApiResponse<ClienteModel>;
        Assert.NotNull(response);
        Assert.False(response.Sucesso);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void Consultar_CodigoInvalido_RetornaBadRequest(int codigo)
    {
        // Act
        var result = _controller.Consultar(codigo) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        _serviceMock.Verify(s => s.Consultar(It.IsAny<int>()), Times.Never);
    }

    // ── ATUALIZAR CONTATO ─────────────────────────────────────────────────

    [Fact]
    public void AtualizarContato_DadosValidos_RetornaOk()
    {
        // Arrange
        var clienteAtualizado = new ClienteModel
        {
            Codigo   = 1001,
            Nome     = "Maria Silva",
            Telefone = "(11) 98888-5678",
            Email    = "maria.nova@email.com"
        };
        _serviceMock
            .Setup(s => s.Atualizar(1001, "(11) 98888-5678", "maria.nova@email.com"))
            .Returns((true, "Atualizacao realizada"));
        _serviceMock
            .Setup(s => s.Consultar(1001))
            .Returns(clienteAtualizado);

        var request = new AtualizarContatoRequest
        {
            Telefone = "(11) 98888-5678",
            Email    = "maria.nova@email.com"
        };

        // Act
        var result = _controller.AtualizarContato(1001, request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var response = result.Value as ApiResponse<ClienteModel>;
        Assert.NotNull(response);
        Assert.True(response.Sucesso);
        Assert.Equal("(11) 98888-5678", response.Dados!.Telefone);
    }

    [Fact]
    public void AtualizarContato_ClienteNaoExiste_RetornaNotFound()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.Atualizar(9999, It.IsAny<string>(), It.IsAny<string>()))
            .Returns((false, "Cliente nao encontrado"));

        var request = new AtualizarContatoRequest
        {
            Telefone = "(11) 98888-5678",
            Email    = "teste@email.com"
        };

        // Act
        var result = _controller.AtualizarContato(9999, request) as NotFoundObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData("11999991234")]       // sem formatação
    [InlineData("(11)99999-1234")]    // sem espaço
    [InlineData("99999-1234")]        // sem DDD
    [InlineData("(1) 99999-1234")]    // DDD incompleto
    public void AtualizarContato_TelefoneInvalido_RetornaBadRequest(string telefone)
    {
        // Act
        var result = _controller.AtualizarContato(1001,
            new AtualizarContatoRequest { Telefone = telefone, Email = "ok@email.com" })
            as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        _serviceMock.Verify(s => s.Atualizar(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("semarroba.com")]
    [InlineData("@semdominio")]
    [InlineData("sem@ponto")]
    public void AtualizarContato_EmailInvalido_RetornaBadRequest(string email)
    {
        // Act
        var result = _controller.AtualizarContato(1001,
            new AtualizarContatoRequest { Telefone = "(11) 99999-1234", Email = email })
            as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        _serviceMock.Verify(s => s.Atualizar(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10000)]
    public void AtualizarContato_CodigoInvalido_RetornaBadRequest(int codigo)
    {
        // Act
        var result = _controller.AtualizarContato(codigo,
            new AtualizarContatoRequest
            {
                Telefone = "(11) 99999-1234",
                Email    = "ok@email.com"
            }) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        _serviceMock.Verify(s => s.Atualizar(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
