using System.Runtime.InteropServices;

namespace CoopAlfa.Shared;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct ClienteStruct
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
    public string Operacao;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
    public string Codigo;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string Nome;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
    public string Telefone;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string Email;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2)]
    public string ReturnCode;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string Mensagem;

    public const string OperacaoConsultar  = "C";
    public const string OperacaoAtualizar  = "A";
    public const string ReturnSucesso       = "00";
    public const string ReturnNaoEncontrado = "01";
    public const string ReturnErro          = "02";
}