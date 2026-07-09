using System.Diagnostics;
using System.Text;
using CoopAlfa.Api.Models;

namespace CoopAlfa.Api.Services;

/// <summary>
/// Servico que integra .NET ao COBOL via processo separado.
/// O CLICORE.exe acessa o DB2 via wrapper C (DB2HELPER.dll) e ODBC.
///
/// Fluxo: API .NET -> REQUEST.DAT -> CLICORE.exe
///        -> CALL "DBCONECT" -> DB2HELPER.dll -> ODBC -> DB2
///        -> RESPONSE.DAT -> API .NET
/// </summary>
public class ClienteService : IClienteService
{
    private static readonly string CobolDir =
        Path.Combine(AppContext.BaseDirectory, "cobol");

    private static readonly object _lock = new();

    private ResponseCobol ExecutarCobol(string operacao, int codigo,
        string nome = "", string telefone = "", string email = "")
    {
        lock (_lock)
        {
            var request = new StringBuilder();
            request.Append(operacao.PadRight(1)[..1]);
            request.Append(codigo.ToString().PadLeft(4, '0'));
            request.Append(nome.PadRight(40)[..40]);
            request.Append(telefone.PadRight(15)[..15]);
            request.Append(email.PadRight(50)[..50]);

            var requestPath  = Path.Combine(CobolDir, "REQUEST.DAT");
            var responsePath = Path.Combine(CobolDir, "RESPONSE.DAT");

            File.WriteAllText(requestPath, request.ToString(),
                Encoding.ASCII);

            var psi = new ProcessStartInfo
            {
                FileName         = Path.Combine(CobolDir, "CLICORE.exe"),
                WorkingDirectory = CobolDir,
                UseShellExecute  = false,
                CreateNoWindow   = true
            };

            // Necessário para o GnuCOBOL encontrar a DB2HELPER.dll
            psi.Environment["COB_PRE_LOAD"]    = "DB2HELPER";
            psi.Environment["COB_LIBRARY_PATH"] = CobolDir;

            using (var proc = Process.Start(psi))
            {
                proc!.WaitForExit(30000);
            }

            if (!File.Exists(responsePath))
                return new ResponseCobol
                {
                    ReturnCode = "02",
                    Mensagem   = "Erro: resposta nao gerada"
                };

            var linha = File.ReadAllText(responsePath, Encoding.ASCII)
                            .Replace("\r", "").Replace("\n", "")
                            .PadRight(187);

            return new ResponseCobol
            {
                ReturnCode = linha.Substring(0, 2).Trim(),
                Nome       = linha.Substring(2, 40).Trim(),
                Telefone   = linha.Substring(42, 15).Trim(),
                Email      = linha.Substring(57, 50).Trim(),
                Mensagem   = linha.Substring(107, 80).Trim()
            };
        }
    }

    public ClienteModel? Consultar(int codigo)
    {
        var r = ExecutarCobol("C", codigo);
        if (r.ReturnCode == "01") return null;

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
        var r = ExecutarCobol("N", codigo, nome, telefone, email);
        return r.ReturnCode == "00"
            ? (true, r.Mensagem)
            : (false, r.Mensagem);
    }

    public (bool sucesso, string mensagem) Atualizar(
        int codigo, string telefone, string email)
    {
        var r = ExecutarCobol("A", codigo, string.Empty, telefone, email);
        return r.ReturnCode == "00"
            ? (true, r.Mensagem)
            : (false, r.Mensagem);
    }

    private class ResponseCobol
    {
        public string ReturnCode { get; set; } = "02";
        public string Nome       { get; set; } = string.Empty;
        public string Telefone   { get; set; } = string.Empty;
        public string Email      { get; set; } = string.Empty;
        public string Mensagem   { get; set; } = string.Empty;
    }
}
