# Plano de Testes
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Objetivo

Este documento descreve os casos de teste definidos para validar as funcionalidades da solução de modernização do cadastro de clientes da Cooperativa Financeira Alfa. Os testes visam garantir que a solução atende aos requisitos funcionais e que futuras alterações não comprometam funcionalidades já implementadas.

---

## 2. Escopo

### Funcionalidades testadas
- Consulta de cliente pelo código
- Atualização de telefone e e-mail do cliente
- Validações de entrada (código, telefone, e-mail)
- Tratamento de cliente não encontrado
- Retorno de erros com mensagens adequadas

### Fora do escopo
- Cadastro de novo cliente (melhoria futura)
- Exclusão de cliente (melhoria futura)
- Autenticação e autorização

---

## 3. Estratégia de Testes

A solução adota dois níveis de teste:

**Testes automatizados (xUnit + Moq):** validam a camada Controller e as regras de negócio de forma isolada, usando mock do `IClienteService`. Não dependem do COBOL ou do arquivo indexado para executar.

**Testes manuais:** validam o fluxo completo end-to-end, desde a interface HTML até o arquivo indexado COBOL.

Em ambiente mainframe real, o **ECCOX APT** seria utilizado para testes isolados do núcleo COBOL, criando ambientes paralelos com dados de teste independentes.

---

## 4. Casos de Teste

### 4.1 Consulta de Cliente

---

**CT-001 — Consulta com código válido e cliente existente**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Cliente com código 1001 existe no sistema |
| **Entrada** | `GET /api/clientes/1001` |
| **Resultado esperado** | HTTP 200 OK com dados do cliente (código, nome, telefone, e-mail) |
| **Critério de aceitação** | `sucesso: true` e dados corretos no campo `dados` |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-002 — Consulta com cliente não existente**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Código 9999 não existe no sistema |
| **Entrada** | `GET /api/clientes/9999` |
| **Resultado esperado** | HTTP 404 Not Found com mensagem de erro |
| **Critério de aceitação** | `sucesso: false` e mensagem informando que cliente não foi encontrado |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-003 — Consulta com código zero**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `GET /api/clientes/0` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` e mensagem de código inválido. COBOL não deve ser chamado. |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-004 — Consulta com código negativo**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `GET /api/clientes/-1` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` e mensagem de código inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-005 — Consulta com código acima do limite**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `GET /api/clientes/10000` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` e mensagem de código inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

### 4.2 Atualização de Contato

---

**CT-006 — Atualização com dados válidos**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Cliente com código 1001 existe no sistema |
| **Entrada** | `PUT /api/clientes/1001/contato` com `telefone: "(11) 98888-5678"` e `email: "novo@email.com"` |
| **Resultado esperado** | HTTP 200 OK com dados atualizados |
| **Critério de aceitação** | `sucesso: true` e dados atualizados no campo `dados` |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-007 — Atualização com cliente não existente**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Código 9999 não existe no sistema |
| **Entrada** | `PUT /api/clientes/9999/contato` com dados válidos |
| **Resultado esperado** | HTTP 404 Not Found |
| **Critério de aceitação** | `sucesso: false` e mensagem de cliente não encontrado |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-008 — Atualização com telefone sem formatação**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `telefone: "11999991234"` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem sobre formato inválido. COBOL não deve ser chamado. |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-009 — Atualização com telefone sem espaço**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `telefone: "(11)99999-1234"` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem sobre formato inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-010 — Atualização com telefone sem DDD**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `telefone: "99999-1234"` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem sobre formato inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-011 — Atualização com DDD incompleto**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `telefone: "(1) 99999-1234"` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem sobre formato inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-012 — Atualização com e-mail sem arroba**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `email: "semarroba.com"` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem de e-mail inválido. COBOL não deve ser chamado. |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-013 — Atualização com e-mail sem domínio**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `email: "@semdominio"` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem de e-mail inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-014 — Atualização com e-mail sem ponto**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `email: "sem@ponto"` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem de e-mail inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-015 — Atualização com código zero**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `PUT /api/clientes/0/contato` com dados válidos |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem de código inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-016 — Atualização com código negativo**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `PUT /api/clientes/-5/contato` com dados válidos |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem de código inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

**CT-017 — Atualização com código acima do limite**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Nenhuma |
| **Entrada** | `PUT /api/clientes/10000/contato` com dados válidos |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Critério de aceitação** | `sucesso: false` com mensagem de código inválido |
| **Tipo** | Automatizado |
| **Status** | ✅ Passou |

---

### 4.3 Testes Manuais (Interface)

---

**CT-018 — Busca pelo código via interface**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | API rodando, CLICORE.dll disponível |
| **Passos** | 1. Acessar `http://localhost:5000` 2. Digitar código válido 3. Clicar em "Buscar" |
| **Resultado esperado** | Dados do cliente exibidos na tela |
| **Critério de aceitação** | Nome, telefone e e-mail visíveis |
| **Tipo** | Manual |

---

**CT-019 — Edição de contato via interface**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Cliente encontrado (CT-018) |
| **Passos** | 1. Clicar em "Editar contato" 2. Alterar telefone e e-mail 3. Clicar em "Salvar alterações" |
| **Resultado esperado** | Mensagem de sucesso e dados atualizados na tela |
| **Critério de aceitação** | Novos dados refletidos imediatamente |
| **Tipo** | Manual |

---

**CT-020 — Máscara automática de telefone**

| Campo | Valor |
|-------|-------|
| **Pré-condição** | Modo de edição ativo |
| **Passos** | Digitar apenas números no campo telefone |
| **Resultado esperado** | Formatação automática `(XX) XXXXX-XXXX` |
| **Critério de aceitação** | Máscara aplicada em tempo real |
| **Tipo** | Manual |

---

## 5. Resumo dos Resultados

| Categoria | Total | Passou | Falhou |
|-----------|-------|--------|--------|
| Consulta (automatizado) | 5 | 5 | 0 |
| Atualização (automatizado) | 12 | 12 | 0 |
| Interface (manual) | 3 | — | — |
| **Total automatizado** | **17** | **17** | **0** |

> **Nota:** Os 18 testes automatizados do xUnit cobrem os casos CT-001 a CT-017. O CT-018 ao CT-020 são manuais e devem ser executados com a aplicação rodando.

---

## 6. Como Executar os Testes Automatizados

```bash
cd src/dotnet
dotnet test CoopAlfa.slnx
```

**Resultado esperado:**
```
Resumo do teste: total: 18; falhou: 0; bem-sucedido: 18; ignorado: 0
```
