using System.Runtime.InteropServices;
using CoopAlfa.Api.Models;

namespace CoopAlfa.Api.Services;

/// <summary>
/// Serviço que integra a camada .NET ao núcleo COBOL via P/Invoke.
/// Traduz chamadas REST em operações no arquivo indexado legado.
/// </summary>
public class ClienteService : IClienteService
{
    // P/Invoke — importa a função do CLICORE.dll compilada pelo GnuCOBOL
    [DllImport("CLICORE.dll", EntryPoint = "CLICORE")]
    private static extern void CLICORE(ref ClienteStruct cliente);

    /// <summary>
    /// Consulta um cliente pelo código no arquivo indexado legado.
    /// </summary>
    public ClienteModel? Consultar(int codigo)
    {
        var dados = new ClienteStruct
        {
            Operacao   = ClienteStruct.OperacaoConsultar,
            Codigo     = codigo.ToString().PadLeft(4, '0'),
            Nome       = new string(' ', 40),
            Telefone   = new string(' ', 15),
            Email      = new string(' ', 50),
            ReturnCode = new string(' ', 2),
            Mensagem   = new string(' ', 80)
        };

        CLICORE(ref dados);

        if (dados.ReturnCode.Trim() == ClienteStruct.ReturnNaoEncontrado)
            return null;

        return new ClienteModel
        {
            Codigo   = codigo,
            Nome     = dados.Nome.Trim(),
            Telefone = dados.Telefone.Trim(),
            Email    = dados.Email.Trim()
        };
    }

    /// <summary>
    /// Atualiza telefone e e-mail de um cliente no arquivo indexado legado.
    /// </summary>
    public (bool sucesso, string mensagem) Atualizar(int codigo, string telefone, string email)
    {
        var dados = new ClienteStruct
        {
            Operacao   = ClienteStruct.OperacaoAtualizar,
            Codigo     = codigo.ToString().PadLeft(4, '0'),
            Nome       = new string(' ', 40),
            Telefone   = telefone.PadRight(15),
            Email      = email.PadRight(50),
            ReturnCode = new string(' ', 2),
            Mensagem   = new string(' ', 80)
        };

        CLICORE(ref dados);

        var rc  = dados.ReturnCode.Trim();
        var msg = dados.Mensagem.Trim();

        return rc == ClienteStruct.ReturnSucesso
            ? (true, msg)
            : (false, msg);
    }
}
