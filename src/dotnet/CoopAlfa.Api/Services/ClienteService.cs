using System.Diagnostics;
using System.Text;
using CoopAlfa.Api.Models;

namespace CoopAlfa.Api.Services;

/// <summary>
/// Servico que integra .NET ao COBOL via processo separado.
/// Grava a requisicao em REQUEST.DAT, executa CLICORE.exe,
/// e le a resposta de RESPONSE.DAT.
///
/// Este padrao reproduz a integracao batch com mainframe legado,
/// onde aplicacoes consumidoras interagem com o COBOL atraves de
/// datasets (arquivos), sem acoplamento direto de memoria.
/// </summary>
public class ClienteService : IClienteService
{
    private static readonly string CobolDir =
        Path.Combine(AppContext.BaseDirectory, "cobol");

    private static readonly object _lock = new();

    private ResponseCobol ExecutarCobol(string operacao, int codigo,
        string telefone = "", string email = "")
    {
        lock (_lock)
        {
            // Monta a requisicao: operacao(1) + codigo(4) + telefone(15) + email(50)
            var request = new StringBuilder();
            request.Append(operacao.PadRight(1)[..1]);
            request.Append(codigo.ToString().PadLeft(4, '0'));
            request.Append(telefone.PadRight(15)[..15]);
            request.Append(email.PadRight(50)[..50]);

            var requestPath = Path.Combine(CobolDir, "REQUEST.DAT");
            var responsePath = Path.Combine(CobolDir, "RESPONSE.DAT");

            File.WriteAllText(requestPath, request.ToString(), Encoding.ASCII);

            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(CobolDir, "CLICORE.exe"),
                WorkingDirectory = CobolDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                proc!.WaitForExit(15000);
            }

            if (!File.Exists(responsePath))
                return new ResponseCobol
                {
                    ReturnCode = "02",
                    Mensagem = "Erro: resposta nao gerada"
                };

            // Le a resposta e remove quebras de linha
            var linha = File.ReadAllText(responsePath, Encoding.ASCII)
                            .Replace("\r", "").Replace("\n", "");

            // Garante tamanho minimo preenchendo com espacos
            linha = linha.PadRight(187);

            // Layout: returncode(2) + nome(40) + telefone(15) + email(50) + mensagem(80)
            return new ResponseCobol
            {
                ReturnCode = linha.Substring(0, 2).Trim(),
                Nome = linha.Substring(2, 40).Trim(),
                Telefone = linha.Substring(42, 15).Trim(),
                Email = linha.Substring(57, 50).Trim(),
                Mensagem = linha.Substring(107, 80).Trim()
            };
        }
    }

    public ClienteModel? Consultar(int codigo)
    {
        var r = ExecutarCobol("C", codigo);
        if (r.ReturnCode == "01") return null;

        return new ClienteModel
        {
            Codigo = codigo,
            Nome = r.Nome,
            Telefone = r.Telefone,
            Email = r.Email
        };
    }

    public (bool sucesso, string mensagem) Atualizar(
        int codigo, string telefone, string email)
    {
        var r = ExecutarCobol("A", codigo, telefone, email);
        return r.ReturnCode == "00"
            ? (true, r.Mensagem)
            : (false, r.Mensagem);
    }

    private class ResponseCobol
    {
        public string ReturnCode { get; set; } = "02";
        public string Nome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
    }
}