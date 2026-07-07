# CoopAlfa — Modernização do Sistema de Cadastro de Clientes

Solução de modernização do cadastro de clientes da Cooperativa Financeira Alfa, desenvolvida como projeto final do programa Acelera Maker (Montreal).

O sistema expõe um núcleo COBOL legado como API REST em .NET, permitindo que atendentes consultem e atualizem dados de clientes por uma interface web moderna — sem substituir o processamento legado.

---

## Arquitetura

```
Interface HTML (atendente)
        ↓ HTTP/REST
  API REST — ASP.NET Core (.NET 10)
        ↓ P/Invoke
  Núcleo COBOL — CLICORE.dll (GnuCOBOL)
        ↓ I/O
  Arquivo indexado — CLIENTES.DAT
```

Detalhes completos em [`docs/arquitetura.md`](docs/arquitetura.md).

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [GnuCOBOL](https://gnucobol.sourceforge.io/) (testado com GnuCOBOL 3.x no Windows)
- Git

---

## Como rodar o projeto

### 1. Clone o repositório

```bash
git clone https://github.com/OSIELJ/coopAlfa-modernizacao.git
cd coopAlfa-modernizacao
```

### 2. Compile o núcleo COBOL

Execute o script de build na raiz do projeto:

```cmd
build.cmd
```

Isso gera o arquivo `src\cobol\build\CLICORE.dll`.

### 3. Copie a DLL para o diretório da API

A API precisa encontrar a `CLICORE.dll` em tempo de execução:

```cmd
copy src\cobol\build\CLICORE.dll src\dotnet\CoopAlfa.Api\
```

### 4. Execute a API

```cmd
cd src\dotnet\CoopAlfa.Api
dotnet run
```

A API sobe em `https://localhost:5001` (HTTPS) ou `http://localhost:5000` (HTTP).

### 5. Acesse a interface do atendente

Abra o navegador em:

```
http://localhost:5000
```

### 6. Acesse o Swagger (documentação da API)

```
http://localhost:5000/swagger
```

---

## Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/clientes/{codigo}` | Consulta cliente pelo código (1-9999) |
| `PUT` | `/api/clientes/{codigo}/contato` | Atualiza telefone e e-mail |

### Exemplo — Consultar cliente

```http
GET /api/clientes/1001
```

```json
{
  "sucesso": true,
  "mensagem": "Operação realizada com sucesso",
  "dados": {
    "codigo": 1001,
    "nome": "Maria Silva",
    "telefone": "(11) 99999-1234",
    "email": "maria@email.com"
  }
}
```

### Exemplo — Atualizar contato

```http
PUT /api/clientes/1001/contato
Content-Type: application/json

{
  "telefone": "(11) 98888-5678",
  "email": "maria.nova@email.com"
}
```

---

## Como rodar os testes

```cmd
cd src\dotnet
dotnet test CoopAlfa.slnx
```

Resultado esperado:
```
Resumo do teste: total: 18; falhou: 0; bem-sucedido: 18; ignorado: 0
```

---

## Qualidade de código

A análise de qualidade é feita com **SonarQube Community** (Docker local).

Para rodar a análise:

```cmd
cd src\dotnet
dotnet sonarscanner begin /k:"coopAlfa-modernizacao" /d:sonar.login="SEU_TOKEN" /d:sonar.host.url="http://localhost:9000"
dotnet build CoopAlfa.slnx
dotnet sonarscanner end /d:sonar.login="SEU_TOKEN"
```

Resultado atual: **Quality Gate Passed** — 0 Bugs, 0 Vulnerabilities, 0 Code Smells.

---

## CI/CD

Pipeline configurado no GitHub Actions (`.github/workflows/build.yml`).

Executa automaticamente a cada push nas branches `main` e `dev`:
- Build da solution
- Execução dos 18 testes xUnit

---

## Estrutura do projeto

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
│       │   ├── Services/      ← Integração COBOL via P/Invoke
│       │   ├── wwwroot/       ← Interface do atendente (HTML)
│       │   └── ClienteStruct.cs ← Espelho da copybook em C#
│       └── CoopAlfa.Tests/
│           └── ClientesControllerTests.cs ← 18 testes xUnit
├── docs/
│   ├── arquitetura.md         ← Documento de Arquitetura
│   ├── plano-de-testes.md     ← Plano de Testes
│   └── relatorio-ia.md        ← Relatório de Utilização de IA
├── build.cmd                  ← Script de build do COBOL
└── README.md
```

---

## Documentação

| Documento | Descrição |
|-----------|-----------|
| [Arquitetura](docs/arquitetura.md) | Decisões técnicas, componentes e fluxo de execução |
| [Plano de Testes](docs/plano-de-testes.md) | 20 casos de teste com critérios de aceitação |
| [Relatório de IA](docs/relatorio-ia.md) | Utilização crítica de IA durante o desenvolvimento |

---

## Tecnologias

| Tecnologia | Uso |
|-----------|-----|
| GnuCOBOL 3.x | Núcleo legado — regras de negócio e persistência |
| ASP.NET Core (.NET 10) | API REST e interface do atendente |
| xUnit + Moq | Testes automatizados |
| SonarQube Community | Análise de qualidade de código |
| GitHub Actions | CI/CD |
| Docker | SonarQube local |

---

## Autor

**Osiel** — Programa Acelera Maker, Montreal (2026)
