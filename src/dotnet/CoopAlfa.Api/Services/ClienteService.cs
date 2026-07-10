using System.Diagnostics;
using System.Text;
using CoopAlfa.Api.Models;

namespace CoopAlfa.Api.Services;

/// <summary>
/// Integra a camada .NET ao nucleo COBOL via processo separado.
///
/// Grava a requisicao em REQUEST.DAT, executa CLICORE.exe, e le a resposta
/// de RESPONSE.DAT. O CLICORE acessa o DB2 atraves do wrapper DB2HELPER (ODBC).
///
/// O layout dos dois arquivos vem de <see cref="ClienteContrato"/>, que espelha
/// a copybook CLIENTE.cpy. Este servico nao conhece posicoes de campo.
/// </summary>
public class ClienteService : IClienteService
{
    private static readonly string CobolDir =
        Path.Combine(AppContext.BaseDirectory, "cobol");

    private const int TimeoutMs = 30_000;

    /// <summary>
    /// REQUEST.DAT e RESPONSE.DAT sao arquivos compartilhados: duas chamadas
    /// simultaneas sobrescreveriam uma a outra. O lock serializa o acesso.
    /// Em producao, o correto seria um par de arquivos temporarios por
    /// requisicao — ou um COBOL residente, como o CICS faz no mainframe.
    /// </summary>
    private static readonly object _lock = new();

    private static RespostaCobol ExecutarCobol(
        string operacao, int codigo,
        string nome = "", string telefone = "", string email = "")
    {
        lock (_lock)
        {
            var requestPath  = Path.Combine(CobolDir, "REQUEST.DAT");
            var responsePath = Path.Combine(CobolDir, "RESPONSE.DAT");

            var request = ClienteContrato.MontarRequest(
                operacao, codigo, nome, telefone, email);

            File.WriteAllText(requestPath, request, Encoding.ASCII);

            var psi = new ProcessStartInfo
            {
                FileName         = Path.Combine(CobolDir, "CLICORE.exe"),
                WorkingDirectory = CobolDir,
                UseShellExecute  = false,
                CreateNoWindow   = true
            };

            using (var proc = Process.Start(psi))
            {
                proc!.WaitForExit(TimeoutMs);
            }

            if (!File.Exists(responsePath))
                return RespostaCobol.Erro("O nucleo COBOL nao gerou resposta.");

            var conteudo = File.ReadAllText(responsePath, Encoding.ASCII);
            return ClienteContrato.LerResponse(conteudo);
        }
    }

    public ClienteModel? Consultar(int codigo)
    {
        var r = ExecutarCobol(ClienteContrato.OperacaoConsultar, codigo);

        if (r.NaoEncontrado) return null;

        return new ClienteModel
        {
            Codigo   = codigo,
            Nome     = r.Nome,
            Telefone = r.Telefone,
            Email    = r.Email
        };
    }

    public (bool sucesso, string mensagem) Cadastrar(
        int codigo, string nome, string telefone, string email)
    {
        var r = ExecutarCobol(
            ClienteContrato.OperacaoNovo, codigo, nome, telefone, email);

        return (r.Sucesso, r.Mensagem);
    }

    public (bool sucesso, string mensagem) Atualizar(
        int codigo, string telefone, string email)
    {
        var r = ExecutarCobol(
            ClienteContrato.OperacaoAtualizar, codigo,
            nome: string.Empty, telefone: telefone, email: email);

        return (r.Sucesso, r.Mensagem);
    }
}
