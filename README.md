# CoopAlfa — Modernização do Sistema de Cadastro de Clientes

Solução de modernização do cadastro de clientes da Cooperativa Financeira Alfa, desenvolvida como projeto final do programa Acelera Maker (Montreal).

O sistema expõe um núcleo COBOL legado como API REST em .NET, permitindo que atendentes consultem, cadastrem e atualizem dados de clientes por uma interface web moderna — sem substituir o processamento legado.

---

## Demonstração

### Consultar Cliente

<img width="708" height="380" alt="CoopAlfa — consulta_cliente" src="https://github.com/user-attachments/assets/22bc0efb-fea7-427e-a8db-8da711e8c7f6" />

### Criar Cliente

<img width="708" height="380" alt="CoopAlfa — criar_cliente" src="https://github.com/user-attachments/assets/d885dfd9-aec6-45a3-adac-f94902d1f3cf" />

### Editar Contato

<img width="708" height="380" alt="CoopAlfa — editar_cliente" src="https://github.com/user-attachments/assets/5d9be405-d4f6-4646-9471-acc439bd28d5" />


---

## Arquitetura

```
Interface HTML (atendente)
        ↓ HTTP/REST
  API REST — ASP.NET Core (.NET 10)
        ↓ Processo separado (arquivo entrada/saída)
  Núcleo COBOL — CLICORE.exe (GnuCOBOL 3.2 64 bits)
        ↓ I/O
  Arquivo indexado — CLIENTES.DAT
```

O .NET grava a requisição em `REQUEST.DAT`, executa o `CLICORE.exe`, e lê a resposta de `RESPONSE.DAT`. Este padrão reproduz a integração batch com mainframe legado, onde aplicações consumidoras interagem com o COBOL através de datasets.

Detalhes completos em [`docs/arquitetura.md`](docs/arquitetura.md).

---

## Evidências de Funcionamento

### API retornando JSON (navegador)

<img width="1605" height="229" alt="Captura de tela 2026-07-08 184057" src="https://github.com/user-attachments/assets/c36b339c-05b2-4b51-8103-fc4c38bef73b" />

### Swagger — GET /api/clientes/1001

<img width="1919" height="1032" alt="Captura de tela 2026-07-08 183844" src="https://github.com/user-attachments/assets/35c24634-bb91-46a0-8c0a-e43348ef5ba5" />

### Interface — cliente não encontrado

<img width="1919" height="1029" alt="Captura de tela 2026-07-08 185051" src="https://github.com/user-attachments/assets/807227f1-777c-4724-a8c7-3542c37ada7d" />

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [GnuCOBOL 3.2 64 bits](https://github.com/OCamlPro/superbol-artefacts/releases/download/gnucobol-3.2-aio-20240402/gnucobol-3.2-aio-20240402-user.msi) (SuperBOL All-in-One para Windows)
- Git

---

## Como rodar o projeto

### 1. Clone o repositório

```bash
git clone https://github.com/OSIELJ/coopAlfa-modernizacao.git
cd coopAlfa-modernizacao
```

### 2. Compile o núcleo COBOL

```cmd
"C:\Users\%USERNAME%\AppData\Local\GnuCOBOL\bin\cobc.exe" -x -fimplicit-init -I "src\cobol\copybook" src\cobol\CLICORE.cbl -o src\cobol\build\CLICORE.exe
```

### 3. Popula os dados iniciais

```cmd
"C:\Users\%USERNAME%\AppData\Local\GnuCOBOL\bin\cobc.exe" -x -fimplicit-init -I "src\cobol\copybook" src\cobol\CLISEED.cbl -o src\cobol\build\CLISEED.exe
cd src\cobol\build
CLISEED.exe
cd ..\..\..
```

### 4. Copia os arquivos COBOL para a API

```cmd
mkdir src\dotnet\CoopAlfa.Api\cobol
copy src\cobol\build\CLICORE.exe src\dotnet\CoopAlfa.Api\cobol\
copy src\cobol\build\CLIENTES.DAT src\dotnet\CoopAlfa.Api\cobol\
copy "C:\Users\%USERNAME%\AppData\Local\GnuCOBOL\bin\*.dll" src\dotnet\CoopAlfa.Api\cobol\
```

### 5. Execute a API

```cmd
cd src\dotnet\CoopAlfa.Api
dotnet run
```

A API sobe em `http://localhost:5125`.

### 6. Acesse a interface do atendente

```
http://localhost:5125/index.html
```

### 7. Acesse o Swagger

```
http://localhost:5125/swagger
```

---

## Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/clientes/{codigo}` | Consulta cliente pelo código (1-9999) |
| `POST` | `/api/clientes` | Cadastra um novo cliente |
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

### Exemplo — Cadastrar cliente

```http
POST /api/clientes
Content-Type: application/json

{
  "codigo": 2001,
  "nome": "Carlos Ferreira",
  "telefone": "(41) 99999-5678",
  "email": "carlos@email.com"
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
Resumo do teste: total: 17; falhou: 0; bem-sucedido: 17; ignorado: 0
```

---

## Qualidade de código

Análise com **SonarQube Community** (Docker local). Resultado atual: **Quality Gate Passed** — 0 Bugs, 0 Vulnerabilities, 0 Code Smells.

---

## CI/CD

Pipeline no GitHub Actions (`.github/workflows/build.yml`). Executa build e testes a cada push nas branches `main` e `dev`.

---

## Estrutura do projeto

```
coopAlfa-modernizacao/
├── .github/workflows/
│   └── build.yml              ← CI/CD GitHub Actions
├── docs/
│   ├── gifs/                  ← GIFs de demonstração
│   ├── arquitetura.md
│   ├── plano-de-testes.md
│   └── relatorio-ia.md
├── src/
│   ├── cobol/
│   │   ├── copybook/
│   │   │   └── CLIENTE.cpy    ← Contrato de dados
│   │   ├── build/
│   │   │   ├── CLICORE.exe    ← Núcleo COBOL compilado
│   │   │   └── CLIENTES.DAT   ← Arquivo indexado legado
│   │   ├── CLICORE.cbl        ← Código-fonte COBOL
│   │   └── CLISEED.cbl        ← Populador de dados iniciais
│   └── dotnet/
│       ├── CoopAlfa.Api/
│       │   ├── Controllers/   ← Endpoints REST
│       │   ├── Models/        ← DTOs
│       │   ├── Services/      ← Integração COBOL via processo
│       │   ├── cobol/         ← Runtime COBOL (gerado localmente)
│       │   └── wwwroot/       ← Interface do atendente (HTML)
│       └── CoopAlfa.Tests/
│           └── ClientesControllerTests.cs ← 17 testes xUnit
├── build.cmd                  ← Script de build do COBOL
└── README.md
```

---

## Documentação

| Documento | Descrição |
|-----------|-----------|
| [Arquitetura](docs/arquitetura.md) | Decisões técnicas, componentes e fluxo |
| [Plano de Testes](docs/plano-de-testes.md) | Casos de teste com critérios de aceitação |
| [Relatório de IA](docs/relatorio-ia.md) | Utilização crítica de IA no desenvolvimento |

---

## Tecnologias

| Tecnologia | Uso |
|-----------|-----|
| GnuCOBOL 3.2 (64 bits) | Núcleo legado — regras e persistência |
| ASP.NET Core (.NET 10) | API REST e interface do atendente |
| xUnit + Moq | Testes automatizados |
| SonarQube Community | Análise de qualidade de código |
| GitHub Actions | CI/CD |
| Docker | SonarQube local |

---

## Autor

**Osiel** — Programa Acelera Maker, Montreal (2026)
