# Documento de Arquitetura
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Visão Geral

A Cooperativa Financeira Alfa possui um sistema legado responsável pelo cadastro de clientes. Apesar de confiável e estável, esse sistema possui limitações para integração com novas aplicações e não atende às necessidades atuais da equipe de atendimento.

O objetivo deste projeto **não é substituir o sistema legado**, mas sim expô-lo como um serviço moderno, permitindo que novas aplicações consumam suas funcionalidades enquanto o processamento original é preservado.

---

## 2. Arquitetura Escolhida

### Padrão: Integração por Processo Separado sobre Núcleo Legado

A solução adota o padrão de modernização **Strangler Fig**, onde o sistema legado é encapsulado por uma camada moderna sem ser substituído. O COBOL permanece como núcleo de processamento e persistência, enquanto o .NET expõe suas funcionalidades como uma API REST.

A comunicação entre .NET e COBOL é feita via **processo separado com troca de dados por arquivo** — reproduzindo fielmente o padrão de integração batch do mainframe, onde aplicações consumidoras interagem com o legado COBOL através de datasets.

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
│  │  GET /api/clientes/{codigo}                 │    │
│  │  PUT /api/clientes/{codigo}/contato         │    │
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
│  1. Lê REQUEST.DAT  (operação + código + dados)      │
│  2. Processa no arquivo indexado CLIENTES.DAT        │
│  3. Grava RESPONSE.DAT (return code + dados)         │
└──────────────────────┬──────────────────────────────┘
                       │ I/O
┌──────────────────────▼──────────────────────────────┐
│                 CLIENTES.DAT                         │
│     (Arquivo Indexado Berkeley DB - Persistência)    │
└─────────────────────────────────────────────────────┘
```

---

## 3. Componentes da Solução

### 3.1 Núcleo COBOL — CLICORE.cbl

Responsável por toda a lógica de negócio e persistência dos dados cadastrais. Compilado como executável pelo GnuCOBOL 3.2 64 bits.

**Responsabilidades:**
- Ler a requisição do arquivo `REQUEST.DAT`
- Processar no arquivo indexado `CLIENTES.DAT`
- Gravar a resposta em `RESPONSE.DAT`

**Layout do REQUEST.DAT:**

| Campo | Posição | Tamanho | Descrição |
|-------|---------|---------|-----------|
| Operação | 1 | 1 | C=Consultar, A=Atualizar |
| Código | 2-5 | 4 | Código do cliente (numérico) |
| Telefone | 6-20 | 15 | Telefone para atualização |
| E-mail | 21-70 | 50 | E-mail para atualização |

**Layout do RESPONSE.DAT:**

| Campo | Posição | Tamanho | Descrição |
|-------|---------|---------|-----------|
| Return Code | 1-2 | 2 | 00=Sucesso, 01=Não encontrado, 02=Erro |
| Nome | 3-42 | 40 | Nome do cliente |
| Telefone | 43-57 | 15 | Telefone do cliente |
| E-mail | 58-107 | 50 | E-mail do cliente |
| Mensagem | 108-187 | 80 | Mensagem de retorno |

### 3.2 Copybook — CLIENTE.cpy

Define a estrutura de dados compartilhada internamente pelo COBOL. É o **contrato de dados** que documenta o layout dos campos.

### 3.3 Web API REST — CoopAlfa.Api

Camada moderna em ASP.NET Core (.NET 10) que expõe o núcleo COBOL como serviço REST. Também serve a interface HTML do atendente como arquivo estático.

**Endpoints:**

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/clientes/{codigo}` | Consulta cliente pelo código |
| `PUT` | `/api/clientes/{codigo}/contato` | Atualiza telefone e e-mail |

**Validações implementadas:**
- Código entre 1 e 9999
- Telefone no formato `(XX) XXXXX-XXXX` ou `(XX) XXXX-XXXX`
- E-mail com formato válido (Regex com timeout — S6444)

### 3.4 Interface do Atendente — index.html

Página HTML/JS servida pela própria API como arquivo estático (`wwwroot`). Consome os endpoints REST da mesma origem.

---

## 4. Decisões Técnicas e Justificativas

### 4.1 Por que processo separado em vez de P/Invoke?

A abordagem inicial prevista era P/Invoke (chamada direta à DLL do COBOL). Durante a implementação, foram identificados problemas de incompatibilidade entre o runtime do GnuCOBOL 32 bits (disponível no OpenCobolIDE) e o .NET 10 64 bits, além de problemas de marshalling de memória entre os dois runtimes.

A decisão de usar processo separado com troca de dados por arquivo foi tomada pelos seguintes motivos:

**Fidelidade ao cenário legado:** a Aline (cliente) descreveu o sistema atual como "dados em arquivos de difícil acesso". No mainframe real, a integração com sistemas legados COBOL é feita exatamente assim — submetendo jobs batch que leem e gravam datasets. O processo separado reproduz esse padrão fielmente.

**Isolamento de runtimes:** cada processo tem seu próprio espaço de memória, eliminando os problemas de marshalling entre .NET e COBOL. Isso é mais robusto e previsível.

**Portabilidade:** o COBOL pode ser substituído por qualquer implementação que leia `REQUEST.DAT` e escreva `RESPONSE.DAT`, sem alterar a API.

**Alternativa considerada e descartada:** P/Invoke direto com CLICORE.dll. Descartado pelos problemas de compatibilidade 32/64 bits e Access Violation (0xC0000005) ao passar structs entre os runtimes.

### 4.2 Por que arquivo indexado em vez de DB2?

O arquivo indexado (`ORGANIZATION IS INDEXED`) representa fielmente o ambiente legado descrito no cenário. É o padrão VSAM do mainframe, simulado pelo GnuCOBOL.

**DB2 foi considerado** e está disponível via Docker. A arquitetura foi desenhada para que o módulo de persistência seja substituível por `EXEC SQL` com DB2 sem alterar a API. Documentado como melhoria futura.

### 4.3 Por que a interface é servida pela própria API?

Serve dois propósitos: simplifica o deploy (um único processo) e **demonstra na prática** que a API é reutilizável — o atendente é apenas mais um cliente consumindo os endpoints REST.

### 4.4 Por que IClienteService (interface)?

Permite injeção de dependência e mock nos testes xUnit sem depender do COBOL. Segue o princípio de inversão de dependência (SOLID).

### 4.5 CORS permissivo no ambiente de desenvolvimento

O `AllowAnyOrigin()` foi mantido para facilitar o desenvolvimento. Em produção, seria substituído por política restrita. Identificado pelo SonarQube (S5122) e documentado como decisão consciente.

---

## 5. Fluxo de Execução

### Consulta de cliente
```
1. Atendente digita o código e clica "Buscar"
2. HTML/JS envia GET /api/clientes/{codigo}
3. ClientesController valida o código (1-9999)
4. ClienteService grava REQUEST.DAT: "C1001..."
5. ClienteService executa CLICORE.exe
6. CLICORE lê REQUEST.DAT, busca no CLIENTES.DAT
7. CLICORE grava RESPONSE.DAT: "00Maria Silva..."
8. ClienteService lê RESPONSE.DAT e monta ClienteModel
9. Controller retorna 200 OK ou 404 Not Found
10. Interface exibe os dados do cliente
```

### Atualização de contato
```
1. Atendente edita telefone/e-mail e clica "Salvar"
2. HTML/JS valida formato localmente
3. HTML/JS envia PUT /api/clientes/{codigo}/contato
4. Controller valida telefone e e-mail (Regex com timeout)
5. ClienteService grava REQUEST.DAT: "A1001(11)98888..."
6. ClienteService executa CLICORE.exe
7. CLICORE lê REQUEST.DAT, atualiza CLIENTES.DAT (REWRITE)
8. CLICORE grava RESPONSE.DAT: "00Maria Silva..."
9. Controller retorna 200 OK com dados atualizados
10. Interface exibe confirmação de sucesso
```

---

## 6. Qualidade e DevOps

### SonarQube
Análise estática do código C# via SonarQube Community 9.9.8 (Docker local).

**Resultado final:**
- Bugs: 0 (Rating A)
- Vulnerabilities: 0 (Rating A)
- Code Smells: 0 (Rating A)
- Quality Gate: **Passed**

### GitHub Actions (CI/CD)
Pipeline configurado em `.github/workflows/build.yml`. Executa automaticamente a cada push nas branches `main` e `dev`:
1. Setup .NET 10
2. Build da solution
3. Execução dos 18 testes xUnit

---

## 7. Estrutura do Projeto

```
coopAlfa-modernizacao/
├── src/cobol/
│   ├── copybook/CLIENTE.cpy       ← Contrato de dados
│   ├── build/CLICORE.exe          ← Núcleo COBOL compilado
│   ├── build/CLIENTES.DAT         ← Arquivo indexado legado
│   ├── CLICORE.cbl                ← Código-fonte COBOL
│   └── CLISEED.cbl                ← Populador de dados
├── src/dotnet/
│   ├── CoopAlfa.Api/
│   │   ├── Controllers/           ← Endpoints REST
│   │   ├── Models/                ← DTOs
│   │   ├── Services/              ← Integração COBOL
│   │   ├── cobol/                 ← Runtime COBOL
│   │   └── wwwroot/index.html     ← Interface do atendente
│   └── CoopAlfa.Tests/            ← 18 testes xUnit
├── docs/                          ← Documentação
└── .github/workflows/build.yml    ← CI/CD
```

---

## 8. Melhorias Futuras

- **DB2 como persistência:** substituir o arquivo indexado por EXEC SQL com DB2
- **Cadastro e exclusão:** operações adicionais já previstas na arquitetura do CLICORE
- **SonarCloud:** migrar análise para SonarCloud para integração nativa com GitHub Actions
- **Autenticação:** adicionar JWT na API para controle de acesso
- **z/OS Connect:** em ambiente mainframe real, o papel do ClienteService seria assumido pelo z/OS Connect
