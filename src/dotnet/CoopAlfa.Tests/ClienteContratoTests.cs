using Xunit;
using CoopAlfa.Api;

namespace CoopAlfa.Tests;

/// <summary>
/// Testes do contrato de dados compartilhado com o nucleo COBOL.
///
/// Estes testes existem para que uma alteracao nos offsets ou tamanhos
/// definidos em CLIENTE.cpy quebre o build, em vez de gravar dados
/// corrompidos silenciosamente no DB2.
/// </summary>
public class ClienteContratoTests
{
    // ── Integridade do layout ────────────────────────────────────────

    [Fact]
    public void Request_TemExatamente110Bytes()
    {
        var request = ClienteContrato.MontarRequest(
            ClienteContrato.OperacaoConsultar, 1001);

        Assert.Equal(ClienteContrato.RequestLen, request.Length);
        Assert.Equal(110, request.Length);
    }

    [Fact]
    public void Request_SomaDosCamposBateComTamanhoTotal()
    {
        var soma = ClienteContrato.ReqOperacaoLen
                 + ClienteContrato.ReqCodigoLen
                 + ClienteContrato.ReqNomeLen
                 + ClienteContrato.ReqTelefoneLen
                 + ClienteContrato.ReqEmailLen;

        Assert.Equal(ClienteContrato.RequestLen, soma);
    }

    [Fact]
    public void Response_SomaDosCamposBateComTamanhoTotal()
    {
        var soma = ClienteContrato.RespReturnCodeLen
                 + ClienteContrato.RespNomeLen
                 + ClienteContrato.RespTelefoneLen
                 + ClienteContrato.RespEmailLen
                 + ClienteContrato.RespMensagemLen;

        Assert.Equal(ClienteContrato.ResponseLen, soma);
        Assert.Equal(187, soma);
    }

    [Fact]
    public void Request_CamposFicamNasPosicoesDeclaradas()
    {
        var request = ClienteContrato.MontarRequest(
            operacao: "N",
            codigo:   1001,
            nome:     "Maria Silva",
            telefone: "(11) 99999-1234",
            email:    "maria@email.com");

        Assert.Equal("N",
            request.Substring(ClienteContrato.ReqOperacaoPos,
                              ClienteContrato.ReqOperacaoLen));

        Assert.Equal("1001",
            request.Substring(ClienteContrato.ReqCodigoPos,
                              ClienteContrato.ReqCodigoLen));

        Assert.Equal("Maria Silva",
            request.Substring(ClienteContrato.ReqNomePos,
                              ClienteContrato.ReqNomeLen).Trim());

        Assert.Equal("(11) 99999-1234",
            request.Substring(ClienteContrato.ReqTelefonePos,
                              ClienteContrato.ReqTelefoneLen).Trim());

        Assert.Equal("maria@email.com",
            request.Substring(ClienteContrato.ReqEmailPos,
                              ClienteContrato.ReqEmailLen).Trim());
    }

    // ── Serializacao da requisicao ───────────────────────────────────

    [Theory]
    [InlineData(1,    "0001")]
    [InlineData(42,   "0042")]
    [InlineData(1001, "1001")]
    [InlineData(9999, "9999")]
    public void Request_CodigoEPreenchidoComZerosAEsquerda(int codigo, string esperado)
    {
        var request = ClienteContrato.MontarRequest("C", codigo);

        Assert.Equal(esperado,
            request.Substring(ClienteContrato.ReqCodigoPos,
                              ClienteContrato.ReqCodigoLen));
    }

    [Fact]
    public void Request_CampoMaisLongoQueODeclaradoETruncado()
    {
        var nomeGigante = new string('X', 60);

        var request = ClienteContrato.MontarRequest("N", 1, nome: nomeGigante);

        Assert.Equal(ClienteContrato.RequestLen, request.Length);
        Assert.Equal(new string('X', ClienteContrato.ReqNomeLen),
            request.Substring(ClienteContrato.ReqNomePos,
                              ClienteContrato.ReqNomeLen));
    }

    [Fact]
    public void Request_CampoVazioEPreenchidoComEspacos()
    {
        var request = ClienteContrato.MontarRequest("C", 1001);

        var nome = request.Substring(ClienteContrato.ReqNomePos,
                                     ClienteContrato.ReqNomeLen);

        Assert.Equal(new string(' ', ClienteContrato.ReqNomeLen), nome);
    }

    // ── Desserializacao da resposta ──────────────────────────────────

    [Fact]
    public void Response_ExtraiCamposDeUmaLinhaRealDoCobol()
    {
        // Saida real do CLICORE.exe para a consulta do cliente 1001
        var linha = "00"
            + "Maria Silva".PadRight(40)
            + "(11) 99999-1234".PadRight(15)
            + "maria@email.com".PadRight(50)
            + "Consulta realizada com sucesso".PadRight(80);

        var r = ClienteContrato.LerResponse(linha);

        Assert.Equal("00", r.ReturnCode);
        Assert.Equal("Maria Silva", r.Nome);
        Assert.Equal("(11) 99999-1234", r.Telefone);
        Assert.Equal("maria@email.com", r.Email);
        Assert.Equal("Consulta realizada com sucesso", r.Mensagem);
        Assert.True(r.Sucesso);
        Assert.False(r.NaoEncontrado);
    }

    [Fact]
    public void Response_ReconheceClienteNaoEncontrado()
    {
        var linha = "01"
            + new string(' ', 105)
            + "Cliente nao encontrado".PadRight(80);

        var r = ClienteContrato.LerResponse(linha);

        Assert.True(r.NaoEncontrado);
        Assert.False(r.Sucesso);
        Assert.Equal("Cliente nao encontrado", r.Mensagem);
    }

    [Fact]
    public void Response_ToleraQuebraDeLinhaNoFinal()
    {
        var linha = "00"
            + "Joao Santos".PadRight(40)
            + "(21) 98888-5678".PadRight(15)
            + "joao@email.com".PadRight(50)
            + "Consulta realizada com sucesso".PadRight(80)
            + "\r\n";

        var r = ClienteContrato.LerResponse(linha);

        Assert.Equal("00", r.ReturnCode);
        Assert.Equal("Joao Santos", r.Nome);
    }

    [Fact]
    public void Response_ToleraConteudoMaisCurtoQueOEsperado()
    {
        var r = ClienteContrato.LerResponse("02");

        Assert.Equal("02", r.ReturnCode);
        Assert.Equal(string.Empty, r.Nome);
        Assert.Equal(string.Empty, r.Mensagem);
    }

    [Fact]
    public void Response_ToleraConteudoNuloOuVazio()
    {
        var r = ClienteContrato.LerResponse(string.Empty);

        Assert.Equal(string.Empty, r.ReturnCode);
        Assert.False(r.Sucesso);
    }

    // ── Ida e volta ──────────────────────────────────────────────────

    [Fact]
    public void Contrato_RequestEResponseUsamOsMesmosTamanhosDeCampo()
    {
        // Nome, telefone e e-mail existem nos dois layouts.
        // Se um mudar sem o outro, os dados truncam de um lado so.
        Assert.Equal(ClienteContrato.ReqNomeLen,     ClienteContrato.RespNomeLen);
        Assert.Equal(ClienteContrato.ReqTelefoneLen, ClienteContrato.RespTelefoneLen);
        Assert.Equal(ClienteContrato.ReqEmailLen,    ClienteContrato.RespEmailLen);
    }
}
