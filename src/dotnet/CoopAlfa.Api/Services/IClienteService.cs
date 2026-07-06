using CoopAlfa.Api.Models;

namespace CoopAlfa.Api.Services;

/// <summary>
/// Interface do serviço de clientes.
/// Permite mock nos testes sem depender do COBOL/P/Invoke.
/// </summary>
public interface IClienteService
{
    ClienteModel? Consultar(int codigo);
    (bool sucesso, string mensagem) Atualizar(int codigo, string telefone, string email);
}
