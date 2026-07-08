# Plano de Testes
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Objetivo

Validar as funcionalidades da solução e garantir que futuras alterações não comprometam funcionalidades já implementadas.

---

## 2. Estratégia de Testes

**Testes automatizados (xUnit + Moq):** validam a camada Controller e as regras de negócio de forma isolada, usando mock do `IClienteService`. Não dependem do COBOL para executar.

**Testes manuais:** validam o fluxo completo end-to-end, desde a interface HTML até o arquivo indexado COBOL.

Em ambiente mainframe real, o **ECCOX APT** seria utilizado para testes isolados do núcleo COBOL, criando ambientes paralelos com dados de teste independentes.

---

## 3. Casos de Teste Automatizados

### 3.1 Consulta de Cliente

**CT-001 — Consulta com código válido e cliente existente**

| Campo | Valor |
|-------|-------|
| **Entrada** | `GET /api/clientes/1001` |
| **Resultado esperado** | HTTP 200 OK com dados do cliente |
| **Status** | ✅ Passou |

**CT-002 — Consulta com cliente não existente**

| Campo | Valor |
|-------|-------|
| **Entrada** | `GET /api/clientes/9999` |
| **Resultado esperado** | HTTP 404 Not Found |
| **Status** | ✅ Passou |

**CT-003 — Código zero**

| Campo | Valor |
|-------|-------|
| **Entrada** | `GET /api/clientes/0` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Status** | ✅ Passou |

**CT-004 — Código negativo**

| Campo | Valor |
|-------|-------|
| **Entrada** | `GET /api/clientes/-1` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Status** | ✅ Passou |

**CT-005 — Código acima do limite**

| Campo | Valor |
|-------|-------|
| **Entrada** | `GET /api/clientes/10000` |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Status** | ✅ Passou |

---

### 3.2 Atualização de Contato

**CT-006 — Atualização com dados válidos**

| Campo | Valor |
|-------|-------|
| **Entrada** | `PUT /api/clientes/1001/contato` com telefone e e-mail válidos |
| **Resultado esperado** | HTTP 200 OK com dados atualizados |
| **Status** | ✅ Passou |

**CT-007 — Cliente não existente**

| Campo | Valor |
|-------|-------|
| **Entrada** | `PUT /api/clientes/9999/contato` |
| **Resultado esperado** | HTTP 404 Not Found |
| **Status** | ✅ Passou |

**CT-008 a CT-011 — Telefone inválido (4 formatos)**

| Formato testado | Status |
|----------------|--------|
| `11999991234` (sem formatação) | ✅ Passou |
| `(11)99999-1234` (sem espaço) | ✅ Passou |
| `99999-1234` (sem DDD) | ✅ Passou |
| `(1) 99999-1234` (DDD incompleto) | ✅ Passou |

**CT-012 a CT-014 — E-mail inválido (3 formatos)**

| Formato testado | Status |
|----------------|--------|
| `semarroba.com` | ✅ Passou |
| `@semdominio` | ✅ Passou |
| `sem@ponto` | ✅ Passou |

**CT-015 a CT-017 — Código inválido na atualização**

| Código testado | Status |
|---------------|--------|
| 0 | ✅ Passou |
| -5 | ✅ Passou |
| 10000 | ✅ Passou |

---

### 3.3 Testes Manuais (Interface)

**CT-018 — Busca pelo código via interface**

| Campo | Valor |
|-------|-------|
| **Passos** | 1. Acessar `http://localhost:5125/index.html` 2. Digitar código 3. Clicar "Buscar" |
| **Resultado esperado** | Dados do cliente exibidos |
| **Evidência** | Sistema exibiu Maria Silva (código 1001) com telefone e e-mail ✅ |

**CT-019 — Edição de contato via interface**

| Campo | Valor |
|-------|-------|
| **Passos** | 1. Clicar "Editar contato" 2. Alterar dados 3. Clicar "Salvar" |
| **Resultado esperado** | Mensagem de sucesso e dados atualizados |

**CT-020 — Máscara automática de telefone**

| Campo | Valor |
|-------|-------|
| **Passos** | Digitar números no campo telefone |
| **Resultado esperado** | Formatação `(XX) XXXXX-XXXX` automática |

---

## 4. Resumo

| Categoria | Total | Passou |
|-----------|-------|--------|
| Automatizados (xUnit) | 18 | 18 ✅ |
| Manuais | 3 | — |

---

## 5. Como Executar

```bash
cd src/dotnet
dotnet test CoopAlfa.slnx
```

**Resultado:**
```
Resumo do teste: total: 18; falhou: 0; bem-sucedido: 18; ignorado: 0
```
