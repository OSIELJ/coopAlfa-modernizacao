# Plano de Testes
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Objetivo

Validar as funcionalidades da solução e garantir que futuras alterações não comprometam funcionalidades já implementadas.

---

## 2. Estratégia de Testes

**Testes automatizados (xUnit + Moq):** validam a camada Controller e as regras de negócio de forma isolada, usando mock do `IClienteService`. Não dependem do COBOL nem do DB2 para executar.

**Testes de integração manuais:** validam o fluxo completo end-to-end, desde a interface HTML até a tabela no DB2.

Em ambiente mainframe real, o **ECCOX APT** seria utilizado para testes isolados do núcleo COBOL, criando ambientes paralelos com dados de teste independentes.

---

## 3. Pré-condições

O DB2 deve estar iniciado corretamente:

```cmd
docker exec -it db2 bash -c "su - db2inst1 -c 'db2stop force; ipclean -a; db2start'"
```

Dados iniciais na tabela `DB2INST1.CLIENTES_COOPALF`:

| CLI_CODIGO | CLI_NOME | CLI_TELEFONE | CLI_EMAIL |
|---|---|---|---|
| 1001 | Maria Silva | (11) 99999-1234 | maria@email.com |
| 1002 | Joao Santos | (21) 98888-5678 | joao@email.com |
| 1003 | Ana Oliveira | (31) 97777-9012 | ana@email.com |

---

## 4. Casos de Teste Automatizados

### 4.1 Consulta de Cliente

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

**CT-003 a CT-005 — Código inválido**

| Código testado | Resultado esperado | Status |
|---------------|-------------------|--------|
| 0 | HTTP 400 Bad Request | ✅ Passou |
| -1 | HTTP 400 Bad Request | ✅ Passou |
| 10000 | HTTP 400 Bad Request | ✅ Passou |

---

### 4.2 Cadastro de Cliente

**CT-006 — Cadastro com dados válidos**

| Campo | Valor |
|-------|-------|
| **Entrada** | `POST /api/clientes` com código 2001, nome, telefone e e-mail válidos |
| **Resultado esperado** | HTTP 201 Created + registro persistido no DB2 |
| **Status** | ✅ Passou |

**CT-007 — Cadastro com código já existente**

| Campo | Valor |
|-------|-------|
| **Entrada** | `POST /api/clientes` com código 1001 (já existe) |
| **Resultado esperado** | HTTP 409 Conflict |
| **Status** | ✅ Passou |

**CT-008 — Cadastro sem nome**

| Campo | Valor |
|-------|-------|
| **Entrada** | `POST /api/clientes` com nome vazio |
| **Resultado esperado** | HTTP 400 Bad Request |
| **Status** | ✅ Passou |

---

### 4.3 Atualização de Contato

**CT-009 — Atualização com dados válidos**

| Campo | Valor |
|-------|-------|
| **Entrada** | `PUT /api/clientes/1001/contato` com telefone e e-mail válidos |
| **Resultado esperado** | HTTP 200 OK + dados atualizados no DB2 |
| **Status** | ✅ Passou |

**CT-010 — Cliente não existente**

| Campo | Valor |
|-------|-------|
| **Entrada** | `PUT /api/clientes/9999/contato` |
| **Resultado esperado** | HTTP 404 Not Found |
| **Status** | ✅ Passou |

**CT-011 a CT-014 — Telefone inválido**

| Formato testado | Status |
|----------------|--------|
| `11999991234` (sem formatação) | ✅ Passou |
| `(11)99999-1234` (sem espaço) | ✅ Passou |
| `99999-1234` (sem DDD) | ✅ Passou |
| `(1) 99999-1234` (DDD incompleto) | ✅ Passou |

**CT-015 a CT-017 — E-mail inválido**

| Formato testado | Status |
|----------------|--------|
| `semarroba.com` | ✅ Passou |
| `@semdominio` | ✅ Passou |
| `sem@ponto` | ✅ Passou |

---

## 5. Testes de Integração (Manuais)

### CT-018 — Fluxo completo de consulta

**Passos:**
1. Iniciar o DB2 com `ipclean + db2start`
2. Rodar a API (`dotnet run`)
3. Acessar `http://localhost:5125/index.html`
4. Digitar código `1001` e clicar "Buscar"

**Resultado esperado:** dados de Maria Silva exibidos na tela

**Evidência:** ✅ Sistema exibiu nome, telefone e e-mail corretamente

---

### CT-019 — Fluxo completo de cadastro (persistência no DB2)

**Passos:**
1. Acessar a aba "Criar Cliente"
2. Preencher: código `5008`, nome `TESTE`, telefone `(11) 11111-1111`, e-mail `DB2@GMAIL.COM`
3. Clicar "Cadastrar cliente"
4. Verificar no DB2:

```cmd
docker exec -it db2 bash -c "su - db2inst1 -c 'db2 connect to BANCO && db2 \"SELECT * FROM DB2INST1.CLIENTES_COOPALF\"'"
```

**Resultado esperado:** registro 5008 presente na tabela do DB2

**Evidência:** ✅ Confirmado — registro persistido:
```
CLI_CODIGO  CLI_NOME   CLI_TELEFONE     CLI_EMAIL
5008        TESTE      (11) 11111-1111  DB2@GMAIL.COM
```

---

### CT-020 — Fluxo completo de atualização

**Passos:**
1. Consultar cliente `1001`
2. Clicar "Editar contato"
3. Alterar telefone e e-mail
4. Clicar "Salvar alterações"
5. Verificar no DB2

**Resultado esperado:** dados atualizados na tabela do DB2

---

### 4.4 Testes do Contrato de Dados (Serialização)

Além dos casos funcionais acima, `ClienteContratoTests.cs` valida isoladamente o layout posicional definido em `CLIENTE.cpy` — offsets, tamanhos, preenchimento com espaços/zeros à esquerda, truncamento de campos maiores que o declarado, e desserialização de respostas do COBOL (incluindo casos com quebra de linha ou conteúdo mais curto que o esperado).

Essa camada de testes garante que uma alteração acidental em `CLIENTE.cpy` ou em `ClienteContrato.cs` quebre o build em vez de gravar dados corrompidos silenciosamente no DB2. São 16 testes adicionais aos 17 casos funcionais da Controller.

---

### CT-021 — Teste direto do COBOL (sem API)

**Passos:**
1. Criar `REQUEST.DAT` com `C1001` + espaços
2. Executar `CLICORE.exe`
3. Verificar `RESPONSE.DAT`

**Resultado esperado:**
```
00Maria Silva ... (11) 99999-1234 ... maria@email.com ... Consulta realizada com sucesso
```

**Evidência:** ✅ Confirmado — COBOL acessou o DB2 diretamente via wrapper C

---

## 6. Resumo

| Categoria | Total | Passou |
|-----------|-------|--------|
| Automatizados (xUnit) — casos funcionais (Controller) | 17 | 17 ✅ |
| Automatizados (xUnit) — contrato de dados (Serialização) | 16 | 16 ✅ |
| **Automatizados (xUnit) — total** | **33** | **33 ✅** |
| Integração manual | 4 | 4 ✅ |

---

## 7. Como Executar os Testes Automatizados

```bash
cd src/dotnet
dotnet test CoopAlfa.slnx
```
