namespace CoopAlfa.Api.Models;

/// <summary>
/// Representa os dados de um cliente na camada da API.
/// Usado como entrada e saída dos endpoints REST.
/// </summary>
public class ClienteModel
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Dados permitidos para atualização — apenas telefone e e-mail.
/// Código e nome não podem ser alterados pelo atendente.
/// </summary>
public class AtualizarContatoRequest
{
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Resposta padronizada da API para todas as operações.
/// </summary>
public class ApiResponse<T>
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public T? Dados { get; set; }

    public static ApiResponse<T> Ok(T dados, string mensagem = "Operação realizada com sucesso")
        => new() { Sucesso = true, Mensagem = mensagem, Dados = dados };

    public static ApiResponse<T> Erro(string mensagem)
        => new() { Sucesso = false, Mensagem = mensagem, Dados = default };
}
