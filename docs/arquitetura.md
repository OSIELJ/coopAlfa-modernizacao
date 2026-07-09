# Documento de Arquitetura
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Visão Geral

A Cooperativa Financeira Alfa possui um sistema legado responsável pelo cadastro de clientes. Apesar de confiável e estável, esse sistema possui limitações para integração com novas aplicações e não atende às necessidades atuais da equipe de atendimento.

O objetivo deste projeto **não é substituir o sistema legado**, mas sim expô-lo como um serviço moderno, permitindo que novas aplicações consumam suas funcionalidades enquanto o processamento original é preservado.

---

## 2. Arquitetura Escolhida

### Padrão: Integração por Processo Separado + COBOL acessando DB2 via ODBC

A solução adota o padrão de modernização **Strangler Fig**, onde o sistema legado é encapsulado por uma camada moderna sem ser substituído. O COBOL permanece como núcleo de processamento e **é ele quem acessa o banco de dados**, enquanto o .NET expõe suas funcionalidades como uma API REST.

```
┌─────────────────────────────────────────────────────┐
│                    Atendente                         │
│              (Navegador Web - HTML/JS)               │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP/REST (JSON)
┌──────────────────────▼──────────────────────────────┐
│                 CoopAlfa.Api                         │
│            (ASP.NET Core - .NET 10)                  │
│  ┌─────────────────────────────────────────────┐    │
│  │           ClientesController                │    │
│  │  GET  /api/clientes/{codigo}                │    │
│  │  POST /api/clientes                         │    │
│  │  PUT  /api/clientes/{codigo}/contato        │    │
│  └──────────────────┬──────────────────────────┘    │
│  ┌──────────────────▼──────────────────────────┐    │
│  │           ClienteService                    │    │
│  │  1. Grava REQUEST.DAT                       │    │
│  │  2. Executa CLICORE.exe                     │    │
│  │  3. Lê RESPONSE.DAT                         │    │
│  └──────────────────┬──────────────────────────┘    │
└─────────────────────┼───────────────────────────────┘
                      │ Process.Start()
┌─────────────────────▼───────────────────────────────┐
│                 CLICORE.exe                          │
│            (GnuCOBOL 3.2 64 bits)                   │
│                                                      │
│  CALL "DBCONECT" / "DBSELECT" / "DBINSERT" /        │
│       "DBUPDATE" / "DBDISCON"                       │
└─────────────────────┬───────────────────────────────┘
                      │ Linkagem estática (.o)
┌─────────────────────▼───────────────────────────────┐
│                 DB2HELPER.o                          │
│         (Wrapper C — camada ODBC)                    │
│                                                      │
│  SQLDriverConnect, SQLExecDirect, SQLFetch,         │
│  SQLGetData, SQLEndTran (commit)                    │
└─────────────────────┬───────────────────────────────┘
                      │ ODBC (IBM DB2 CLI Driver)
┌─────────────────────▼───────────────────────────────┐
│               IBM DB2 (Docker)                       │
│         Tabela: DB2INST1.CLIENTES_COOPALF            │
└─────────────────────────────────────────────────────┘
```

---

## 3. Componentes da Solução

### 3.1 Núcleo COBOL — CLICORE.cbl

Responsável pela lógica de negócio e por orquestrar o acesso ao DB2 através das chamadas ao wrapper C.

**Operações:**

| Código | Operação | Função C chamada |
|--------|----------|------------------|
| `C` | Consultar cliente | `DBSELECT` |
| `N` | Cadastrar novo cliente | `DBINSERT` |
| `A` | Atualizar telefone e e-mail | `DBUPDATE` |

**Return codes:**

| Código | Significado |
|--------|-------------|
| `00` | Sucesso |
| `01` | Não encontrado / Código já existe |
| `02` | Erro interno |

### 3.2 Wrapper C — DB2HELPER.c

Camada intermediária escrita em C que traduz as chamadas COBOL (`CALL "DBSELECT"`) em chamadas ODBC.

**Por que um wrapper?**
O GnuCOBOL não consegue chamar funções ODBC diretamente via `CALL`, pois procura módulos COBOL, não símbolos C. O wrapper é compilado como objeto (`.o`) e linkado estaticamente ao executável COBOL.

**Funções expostas:**

| Função | Descrição |
|--------|-----------|
| `DBCONECT` | Conecta no DB2 via `SQLDriverConnect` |
| `DBSELECT` | Consulta cliente pelo código |
| `DBINSERT` | Insere novo cliente + `SQLEndTran` (commit) |
| `DBUPDATE` | Atualiza contato + `SQLEndTran` (commit) |
| `DBDISCON` | Desconecta e libera handles |

### 3.3 Banco de Dados — IBM DB2

Tabela `DB2INST1.CLIENTES_COOPALF`:

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| `CLI_CODIGO` | `INTEGER NOT NULL` | Chave primária |
| `CLI_NOME` | `VARCHAR(40) NOT NULL` | Nome do cliente |
| `CLI_TELEFONE` | `VARCHAR(15)` | Telefone |
| `CLI_EMAIL` | `VARCHAR(50)` | E-mail |

### 3.4 Web API REST — CoopAlfa.Api

Camada moderna em ASP.NET Core (.NET 10) que expõe o núcleo COBOL como serviço REST.

**Validações implementadas:**
- Código entre 1 e 9999
- Nome obrigatório (máx 40 caracteres)
- Telefone no formato `(XX) XXXXX-XXXX`
- E-mail com formato válido (Regex com timeout — S6444)

### 3.5 Interface do Atendente — index.html

Página HTML/JS servida pela própria API. Duas abas: **Consultar Cliente** e **Criar Cliente**.

---

## 4. Decisões Técnicas e Justificativas

### 4.1 Por que processo separado em vez de P/Invoke?

A abordagem inicial prevista era P/Invoke (chamada direta à DLL do COBOL). Durante a implementação foram identificados problemas de incompatibilidade entre o runtime do GnuCOBOL e o .NET 10 (BadImageFormatException 32/64 bits, Access Violation 0xC0000005 no marshalling de structs).

A decisão de usar processo separado com troca de dados por arquivo foi tomada porque:

**Fidelidade ao cenário legado:** no mainframe real, a integração com sistemas legados COBOL é feita submetendo jobs batch que leem e gravam datasets. O processo separado reproduz esse padrão.

**Isolamento de runtimes:** cada processo tem seu próprio espaço de memória, eliminando problemas de marshalling entre .NET e COBOL.

### 4.2 Por que o COBOL acessa o DB2 e não o .NET?

Esta foi uma decisão explícita da cliente (Aline): *"O Cobol atualiza o arquivo ou banco de dados"*. No mainframe real, o COBOL é quem executa `EXEC SQL` contra o DB2 — não uma camada intermediária.

Manter o COBOL como responsável pela persistência preserva o papel do legado e demonstra fielmente o fluxo `.NET → COBOL → DB2`.

### 4.3 Por que um wrapper C em vez de EXEC SQL?

O `EXEC SQL` do DB2 exige o preprocessador `db2 prep`, que transforma o COBOL em C antes de compilar. O setup desse preprocessador com GnuCOBOL no Windows é complexo e frágil.

O wrapper C oferece o mesmo resultado — o COBOL comanda o acesso ao banco — usando a API ODBC padrão. É uma solução equivalente e mais portável.

**Compilação:** o wrapper é compilado como objeto (`gcc -c`) e linkado ao COBOL (`cobc -x CLICORE.cbl DB2HELPER.o -lodbc32`), gerando um único executável.

### 4.4 Por que SQLDriverConnect em vez de SQLConnect?

O `SQLDriverConnect` aceita uma connection string completa (`DSN=BANCODSN;UID=...;PWD=...`), enquanto o `SQLConnect` passa os parâmetros separadamente. O primeiro se mostrou mais confiável com o driver IBM DB2 CLI.

Também foi configurado `SQL_ATTR_LOGIN_TIMEOUT` de 10 segundos para evitar travamentos indefinidos quando o DB2 não responde.

### 4.5 Por que commit explícito com SQLEndTran?

O ODBC opera em modo *autocommit* por padrão, mas o driver DB2 CLI pode não persistir imediatamente. O `SQLEndTran(SQL_HANDLE_DBC, hDbc, SQL_COMMIT)` após INSERT e UPDATE garante que os dados sejam gravados.

### 4.6 Por que IClienteService (interface)?

Permite injeção de dependência e mock nos testes xUnit sem depender do COBOL ou do DB2. Segue o princípio de inversão de dependência (SOLID).

---

## 5. Fluxo de Execução

### Consulta de cliente
```
1. Atendente digita o código e clica "Buscar"
2. HTML/JS envia GET /api/clientes/{codigo}
3. ClientesController valida o código (1-9999)
4. ClienteService grava REQUEST.DAT: "C1001..."
5. ClienteService executa CLICORE.exe
6. CLICORE lê REQUEST.DAT
7. CLICORE chama DBCONECT (conecta no DB2)
8. CLICORE chama DBSELECT (SELECT no DB2)
9. CLICORE chama DBDISCON (desconecta)
10. CLICORE grava RESPONSE.DAT: "00Maria Silva..."
11. ClienteService lê RESPONSE.DAT
12. Controller retorna 200 OK ou 404 Not Found
```

### Cadastro de cliente
```
1. Atendente preenche o formulário e clica "Cadastrar"
2. HTML/JS envia POST /api/clientes
3. Controller valida código, nome, telefone e e-mail
4. ClienteService grava REQUEST.DAT: "N2001Carlos..."
5. CLICORE chama DBINSERT (INSERT + COMMIT no DB2)
6. Controller retorna 201 Created ou 409 Conflict
```

### Atualização de contato
```
1. Atendente edita telefone/e-mail e clica "Salvar"
2. HTML/JS envia PUT /api/clientes/{codigo}/contato
3. ClienteService grava REQUEST.DAT: "A1001(11)98888..."
4. CLICORE chama DBUPDATE (UPDATE + COMMIT no DB2)
5. Controller retorna 200 OK com dados atualizados
```

---

## 6. Desafios Encontrados

### 6.1 GnuCOBOL 32 bits vs .NET 64 bits
O GnuCOBOL do OpenCobolIDE é 32 bits. Foi necessário instalar o GnuCOBOL 3.2 64 bits (SuperBOL All-in-One) para compatibilidade.

### 6.2 CALL para funções ODBC não funciona no COBOL
O `CALL "SQLAllocHandle"` falha porque o GnuCOBOL procura módulos COBOL, não símbolos C. Solução: wrapper C linkado estaticamente.

### 6.3 DB2 Community no Docker e TCP
O `db2start` frequentemente retorna `SQL5043N` (falha ao iniciar protocolos de comunicação), impedindo conexões ODBC externas. Solução: executar `ipclean -a` antes do `db2start`.

```cmd
docker exec -it db2 bash -c "su - db2inst1 -c 'db2stop force; ipclean -a; db2start'"
```

---

## 7. Qualidade e DevOps

### SonarQube
Análise estática do código C# via SonarQube Community (Docker local).

**Resultado:** Bugs 0 (A) | Vulnerabilities 0 (A) | Code Smells 0 (A) | Quality Gate **Passed**

### GitHub Actions (CI/CD)
Pipeline em `.github/workflows/build.yml`. Executa build e testes xUnit a cada push nas branches `main` e `dev`.

---

## 8. Melhorias Futuras

- **EXEC SQL nativo:** substituir o wrapper C por `EXEC SQL` com o preprocessador `db2 prep`, mais próximo do Enterprise COBOL do mainframe.
- **Exclusão de clientes:** operação `DBDELETE` já prevista na arquitetura.
- **Connection pooling:** manter a conexão DB2 aberta entre chamadas para reduzir latência.
- **Autenticação:** adicionar JWT na API para controle de acesso.
- **z/OS Connect:** em ambiente mainframe real, o papel do ClienteService seria assumido pelo z/OS Connect.
