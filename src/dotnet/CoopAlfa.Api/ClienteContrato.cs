using System.Text;

namespace CoopAlfa.Api;

/// <summary>
/// Espelho em C# da copybook <c>src/cobol/copybook/CLIENTE.cpy</c>.
///
/// A copybook e a fonte de verdade do contrato. Este arquivo existe porque o
/// .NET nao consegue ler uma copybook COBOL — mas toda posicao aqui deriva
/// diretamente dela, e qualquer alteracao na copybook exige alteracao aqui.
///
/// REQUEST.DAT  — 110 bytes — .NET escreve, COBOL le
/// RESPONSE.DAT — 187 bytes — COBOL escreve, .NET le
///
/// O construtor estatico valida que a soma dos campos bate com o tamanho
/// total. Se alguem alterar um tamanho e esquecer de ajustar o resto, a
/// aplicacao falha ao subir em vez de gravar dados corrompidos.
/// </summary>
public static class ClienteContrato
{
    // ── REQUEST.DAT ──────────────────────────────────────────────────
    // 01 WS-REQUEST.
    //     05 WS-OPERACAO      PIC X(01).   pos 001-001
    //     05 WS-CODIGO        PIC X(04).   pos 002-005
    //     05 WS-NOME-REQ      PIC X(40).   pos 006-045
    //     05 WS-TELEFONE-REQ  PIC X(15).   pos 046-060
    //     05 WS-EMAIL-REQ     PIC X(50).   pos 061-110

    public const int ReqOperacaoPos = 0;   public const int ReqOperacaoLen = 1;
    public const int ReqCodigoPos   = 1;   public const int ReqCodigoLen   = 4;
    public const int ReqNomePos     = 5;   public const int ReqNomeLen     = 40;
    public const int ReqTelefonePos = 45;  public const int ReqTelefoneLen = 15;
    public const int ReqEmailPos    = 60;  public const int ReqEmailLen    = 50;

    public const int RequestLen = 110;

    // ── RESPONSE.DAT ─────────────────────────────────────────────────
    // 01 WS-RESPONSE.
    //     05 WS-RETURN-CODE   PIC X(02).   pos 001-002
    //     05 WS-NOME          PIC X(40).   pos 003-042
    //     05 WS-TEL-OUT       PIC X(15).   pos 043-057
    //     05 WS-EMAIL-OUT     PIC X(50).   pos 058-107
    //     05 WS-MENSAGEM      PIC X(80).   pos 108-187

    public const int RespReturnCodePos = 0;   public const int RespReturnCodeLen = 2;
    public const int RespNomePos       = 2;   public const int RespNomeLen       = 40;
    public const int RespTelefonePos   = 42;  public const int RespTelefoneLen   = 15;
    public const int RespEmailPos      = 57;  public const int RespEmailLen      = 50;
    public const int RespMensagemPos   = 107; public const int RespMensagemLen   = 80;

    public const int ResponseLen = 187;

    // ── Operacoes (niveis 88 da copybook) ────────────────────────────
    public const string OperacaoConsultar = "C";
    public const string OperacaoNovo      = "N";
    public const string OperacaoAtualizar = "A";

    // ── Return codes (niveis 88 da copybook) ─────────────────────────
    public const string RcSucesso       = "00";
    public const string RcNaoEncontrado = "01";
    public const string RcErro          = "02";

    /// <summary>
    /// Falha alto se a copybook e este arquivo sairem de sincronia.
    /// </summary>
    static ClienteContrato()
    {
        var somaRequest = ReqOperacaoLen + ReqCodigoLen + ReqNomeLen
                        + ReqTelefoneLen + ReqEmailLen;

        if (somaRequest != RequestLen)
            throw new InvalidOperationException(
                $"Contrato inconsistente: soma dos campos de REQUEST e " +
                $"{somaRequest}, mas RequestLen e {RequestLen}. " +
                $"Confira CLIENTE.cpy.");

        var somaResponse = RespReturnCodeLen + RespNomeLen + RespTelefoneLen
                         + RespEmailLen + RespMensagemLen;

        if (somaResponse != ResponseLen)
            throw new InvalidOperationException(
                $"Contrato inconsistente: soma dos campos de RESPONSE e " +
                $"{somaResponse}, mas ResponseLen e {ResponseLen}. " +
                $"Confira CLIENTE.cpy.");
    }

    /// <summary>
    /// Serializa uma requisicao no layout posicional de REQUEST.DAT.
    /// Campos mais curtos sao preenchidos com espacos a direita;
    /// campos mais longos sao truncados no tamanho declarado.
    /// </summary>
    public static string MontarRequest(
        string operacao, int codigo,
        string nome = "", string telefone = "", string email = "")
    {
        var sb = new StringBuilder(RequestLen);
        sb.Append(Campo(operacao, ReqOperacaoLen));
        sb.Append(codigo.ToString().PadLeft(ReqCodigoLen, '0')[..ReqCodigoLen]);
        sb.Append(Campo(nome,     ReqNomeLen));
        sb.Append(Campo(telefone, ReqTelefoneLen));
        sb.Append(Campo(email,    ReqEmailLen));
        return sb.ToString();
    }

    /// <summary>
    /// Desserializa o conteudo de RESPONSE.DAT nos campos do contrato.
    /// Tolera conteudo mais curto que o esperado (preenche com espacos).
    /// </summary>
    public static RespostaCobol LerResponse(string conteudo)
    {
        var linha = (conteudo ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .PadRight(ResponseLen);

        return new RespostaCobol(
            ReturnCode: Extrair(linha, RespReturnCodePos, RespReturnCodeLen),
            Nome:       Extrair(linha, RespNomePos,       RespNomeLen),
            Telefone:   Extrair(linha, RespTelefonePos,   RespTelefoneLen),
            Email:      Extrair(linha, RespEmailPos,      RespEmailLen),
            Mensagem:   Extrair(linha, RespMensagemPos,   RespMensagemLen));
    }

    private static string Campo(string valor, int tamanho)
        => (valor ?? string.Empty).PadRight(tamanho)[..tamanho];

    private static string Extrair(string linha, int pos, int len)
        => linha.Substring(pos, len).Trim();
}

/// <summary>
/// Resposta devolvida pelo nucleo COBOL, ja desserializada.
/// </summary>
public record RespostaCobol(
    string ReturnCode,
    string Nome,
    string Telefone,
    string Email,
    string Mensagem)
{
    public bool Sucesso       => ReturnCode == ClienteContrato.RcSucesso;
    public bool NaoEncontrado => ReturnCode == ClienteContrato.RcNaoEncontrado;

    /// <summary>Resposta de erro para quando o COBOL nao chega a responder.</summary>
    public static RespostaCobol Erro(string mensagem)
        => new(ClienteContrato.RcErro, string.Empty, string.Empty,
               string.Empty, mensagem);
}
