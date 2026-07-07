# Documento de Arquitetura
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Visão Geral

A Cooperativa Financeira Alfa possui um sistema legado responsável pelo cadastro de clientes. Apesar de confiável e estável, esse sistema possui limitações para integração com novas aplicações e não atende às necessidades atuais da equipe de atendimento.

O objetivo deste projeto **não é substituir o sistema legado**, mas sim expô-lo como um serviço moderno, permitindo que novas aplicações consumam suas funcionalidades enquanto o processamento original é preservado.

---

## 2. Arquitetura Escolhida

### Padrão: API Gateway sobre Núcleo Legado

A solução adota o padrão de modernização **Strangler Fig**, onde o sistema legado é encapsulado por uma camada moderna sem ser substituído. O COBOL permanece como núcleo de processamento e persistência, enquanto o .NET expõe suas funcionalidades como uma API REST.

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
│  │         (P/Invoke → COBOL)                  │    │
│  └──────────────────┬──────────────────────────┘    │
└─────────────────────┼───────────────────────────────┘
                      │ P/Invoke (chamada direta à DLL)
┌─────────────────────▼───────────────────────────────┐
│                  CLICORE.dll                         │
│            (GnuCOBOL - Núcleo Legado)                │
│                                                      │
│  Operação C → Consulta no arquivo indexado           │
│  Operação A → Atualização no arquivo indexado        │
└──────────────────────┬──────────────────────────────┘
                       │ I/O
┌──────────────────────▼──────────────────────────────┐
│                 CLIENTES.DAT                         │
│          (Arquivo Indexado - Persistência)           │
└─────────────────────────────────────────────────────┘
```

---

## 3. Componentes da Solução

### 3.1 Núcleo COBOL — CLICORE.cbl

Responsável por toda a lógica de negócio e persistência dos dados cadastrais. Compilado como módulo (`.dll`) pelo GnuCOBOL, é chamado diretamente pelo .NET via P/Invoke.

**Responsabilidades:**
- Ler registros do arquivo indexado `CLIENTES.DAT`
- Gravar atualizações de contato no arquivo indexado
- Retornar return codes padronizados para o .NET

**Operações suportadas:**

| Código | Operação |
|--------|----------|
| `C` | Consultar cliente pelo código |
| `A` | Atualizar telefone e e-mail |

**Return codes:**

| Código | Significado |
|--------|-------------|
| `00` | Sucesso |
| `01` | Cliente não encontrado |
| `02` | Erro interno |

### 3.2 Copybook — CLIENTE.cpy

Define a estrutura de dados compartilhada entre o COBOL e o .NET. É o **contrato único de dados** da solução — qualquer alteração na estrutura deve ser refletida nos dois lados simultaneamente.

**Total: 192 bytes**

| Campo | COBOL | C# | Bytes |
|-------|-------|----|-------|
| Operação | `PIC X(01)` | `string SizeConst=1` | 1 |
| Código | `PIC 9(04)` | `string SizeConst=4` | 4 |
| Nome | `PIC X(40)` | `string SizeConst=40` | 40 |
| Telefone | `PIC X(15)` | `string SizeConst=15` | 15 |
| E-mail | `PIC X(50)` | `string SizeConst=50` | 50 |
| Return code | `PIC X(02)` | `string SizeConst=2` | 2 |
| Mensagem | `PIC X(80)` | `string SizeConst=80` | 80 |

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
- E-mail com formato válido

### 3.4 Interface do Atendente — index.html

Página HTML/JS servida pela própria API como arquivo estático (`wwwroot`). Consome os endpoints REST da mesma origem, provando na prática que a API é reutilizável.

**Funcionalidades:**
- Busca de cliente por código
- Exibição de dados cadastrais
- Edição de telefone e e-mail com máscara automática
- Feedback visual de sucesso e erro

---

## 4. Decisões Técnicas e Justificativas

### 4.1 Por que P/Invoke em vez de processo separado?

O P/Invoke permite chamada **in-process** e síncrona ao COBOL, sem overhead de criação de processo. É equivalente ao papel do z/OS Connect no mainframe real — que também faz chamadas diretas ao programa COBOL sem criar subprocessos.

**Alternativa considerada:** Invocar o COBOL como executável separado via `Process.Start()`. Descartada por ser mais lenta e frágil (troca de dados por arquivo ou stdout).

### 4.2 Por que arquivo indexado em vez de DB2?

O arquivo indexado (`ORGANIZATION IS INDEXED`) representa fielmente o ambiente legado descrito no cenário — *"dados em arquivos de difícil acesso"*. É o padrão VSAM do mainframe, simulado pelo GnuCOBOL.

**DB2 foi considerado** e está disponível via Docker. A arquitetura foi desenhada para que o módulo de persistência seja substituível por `EXEC SQL` com DB2 sem alterar a API nem a copybook. Documentado como melhoria futura.

### 4.3 Por que a interface é servida pela própria API?

Serve dois propósitos: simplifica o deploy (um único processo) e **demonstra na prática** que a API é reutilizável — o atendente é apenas mais um cliente consumindo os endpoints REST, exatamente como qualquer aplicação futura faria.

### 4.4 Por que IClienteService (interface) em vez de ClienteService diretamente?

Permite **injeção de dependência** e mock nos testes xUnit sem depender do COBOL/P/Invoke. Segue o princípio de inversão de dependência (SOLID), tornando a solução testável e extensível.

### 4.5 CORS permissivo no ambiente de desenvolvimento

O `AllowAnyOrigin()` foi mantido para facilitar o desenvolvimento e demonstração. Em produção, seria substituído por política restrita com origens específicas permitidas. Identificado pelo SonarQube (S5122) e documentado como decisão consciente.

---

## 5. Fluxo de Execução

### Consulta de cliente
```
1. Atendente digita o código e clica "Buscar"
2. HTML/JS envia GET /api/clientes/{codigo}
3. ClientesController valida o código (1-9999)
4. ClienteService monta o ClienteStruct com Operacao='C'
5. P/Invoke chama CLICORE.dll
6. COBOL abre CLIENTES.DAT, busca pelo ARQ-CODIGO
7. COBOL retorna RC='00' (sucesso) ou RC='01' (não encontrado)
8. ClienteService converte o struct em ClienteModel
9. Controller retorna 200 OK ou 404 Not Found
10. Interface exibe os dados ou mensagem de erro
```

### Atualização de contato
```
1. Atendente edita telefone/e-mail e clica "Salvar"
2. HTML/JS valida formato localmente (máscara + regex)
3. HTML/JS envia PUT /api/clientes/{codigo}/contato
4. Controller valida telefone e e-mail (regex com timeout)
5. ClienteService monta o ClienteStruct com Operacao='A'
6. P/Invoke chama CLICORE.dll
7. COBOL lê o registro, atualiza telefone e e-mail, faz REWRITE
8. COBOL retorna RC='00' (sucesso) ou RC='01' (não encontrado)
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
- Duplications: 0%
- Quality Gate: **Passed**

### GitHub Actions (CI/CD)
Pipeline configurado em `.github/workflows/build.yml`. Executa automaticamente a cada push nas branches `main` e `dev`.

**Etapas do pipeline:**
1. Checkout do código
2. Setup .NET 10
3. Build da solution
4. Execução dos 18 testes xUnit

---

## 7. Estrutura do Projeto

```
coopAlfa-modernizacao/
├── .github/workflows/
│   └── build.yml              ← CI/CD GitHub Actions
├── src/
│   ├── cobol/
│   │   ├── copybook/
│   │   │   └── CLIENTE.cpy    ← Contrato único de dados
│   │   ├── build/
│   │   │   └── CLICORE.dll    ← Núcleo COBOL compilado
│   │   └── CLICORE.cbl        ← Código-fonte COBOL
│   └── dotnet/
│       ├── CoopAlfa.Api/
│       │   ├── Controllers/   ← Endpoints REST
│       │   ├── Models/        ← DTOs
│       │   ├── Services/      ← P/Invoke para COBOL
│       │   ├── wwwroot/       ← Interface do atendente
│       │   └── ClienteStruct.cs ← Espelho da copybook
│       └── CoopAlfa.Tests/
│           └── ClientesControllerTests.cs ← 18 testes
├── docs/
│   ├── arquitetura.md         ← Este documento
│   ├── plano-de-testes.md
│   └── relatorio-ia.md
├── build.cmd                  ← Script de build do COBOL
└── README.md
```

---

## 8. Melhorias Futuras

- **DB2 como persistência:** substituir o arquivo indexado por EXEC SQL com DB2, mantendo a copybook como contrato. Infraestrutura já disponível via Docker.
- **SonarCloud:** migrar análise para SonarCloud (plano gratuito até 50k LOC) para integração nativa com GitHub Actions.
- **Cadastro e exclusão de clientes:** operações adicionais já previstas na arquitetura do CLICORE (parâmetro de operação extensível).
- **Autenticação:** adicionar JWT na API para controle de acesso dos atendentes.
- **z/OS Connect:** em ambiente mainframe real, o papel do ClienteService seria assumido pelo z/OS Connect, sem alterar a copybook nem a lógica de negócio do COBOL.
