using CoopAlfa.Api.Models;

namespace CoopAlfa.Api.Services;

public interface IClienteService
{
    ClienteModel? Consultar(int codigo);
    (bool sucesso, string mensagem) Cadastrar(int codigo, string nome,
        string telefone, string email);
    (bool sucesso, string mensagem) Atualizar(int codigo, string telefone,
        string email);
}
