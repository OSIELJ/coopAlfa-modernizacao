# CoopAlfa — Modernização do Sistema de Cadastro de Clientes

Solução de modernização do cadastro de clientes da Cooperativa Financeira Alfa, desenvolvida como projeto final do programa Acelera Maker (Montreal).

O sistema expõe um núcleo COBOL legado como API REST em .NET, permitindo que atendentes consultem, cadastrem e atualizem dados de clientes por uma interface web moderna — sem substituir o processamento legado. O COBOL persiste os dados diretamente no **IBM DB2** via ODBC.

---

## Demonstração

### Consultar Cliente

<img width="708" height="380" alt="CoopAlfa — consulta_cliente" src="https://github.com/user-attachments/assets/17a50207-3f76-46b8-bfbf-17b5ae35bd2d" />

### Criar Cliente

<img width="708" height="380" alt="CoopAlfa — criar_cliente" src="https://github.com/user-attachments/assets/4624b005-3d4c-4c8c-b7d6-e649583d593d" />

### Editar Contato

<img width="708" height="380" alt="CoopAlfa — editar_cliente" src="https://github.com/user-attachments/assets/e648dee2-b619-4c24-8055-62e1f8d9111a" />

---

## Evidências de Funcionamento

### API retornando JSON (navegador)
<img width="1605" height="229" alt="Captura de tela 2026-07-08 184057" src="https://github.com/user-attachments/assets/cfe041eb-31c7-453d-8211-b8f872ca06ef" />


### Swagger — GET /api/clientes/1001
<img width="1919" height="1032" alt="Captura de tela 2026-07-08 183844" src="https://github.com/user-attachments/assets/f0c06fea-ecfe-4f56-94f4-8f704412ba0c" />


## Evidências de Funcionamento

O fluxo abaixo demonstra o ciclo completo — cadastro, persistência, consulta e atualização — com verificação direta na tabela do DB2 a cada etapa.

### 1. Cadastro de cliente pela interface

<img width="783" alt="Formulário de cadastro preenchido" src="https://github.com/user-attachments/assets/a3b6357d-c925-4d97-bdce-1d837ec28e1b" />

### 2. Confirmação do cadastro

<img width="715" alt="Cliente cadastrado com sucesso" src="https://github.com/user-attachments/assets/d9feb0e7-cf23-405d-9e90-db6fb92ce677" />

### 3. Registro persistido no DB2

O cliente 7070 aparece na tabela `DB2INST1.CLIENTES_COOPALF` com telefone `(88) 88888-8888`.

<img width="1101" alt="SELECT no DB2 após cadastro" src="https://github.com/user-attachments/assets/ac68b751-7108-474e-a692-d5eef8a75017" />

### 4. Consulta do cliente recém-cadastrado

<img width="660" alt="Consulta do cliente 7070" src="https://github.com/user-attachments/assets/4deb4ae3-9f26-4eb9-823e-b37818b20a46" />

### 5. Atualização do contato

Telefone alterado de `(88) 88888-8888` para `(88) 88888-9999`.

<img width="704" alt="Contato atualizado com sucesso" src="https://github.com/user-attachments/assets/8be38a6b-f1ab-4984-a6ee-d05be9f9bc18" />

### 6. Atualização refletida no DB2

<img width="1103" alt="SELECT no DB2 após atualização" src="https://github.com/user-attachments/assets/1f98d85a-0292-45d6-a5f7-32a0f65c60eb" />

---

## Arquitetura

```
Interface HTML (atendente)
        ↓ HTTP/REST
  API REST — ASP.NET Core (.NET 10)
        ↓ Processo separado (REQUEST.DAT / RESPONSE.DAT)
  Núcleo COBOL — CLICORE.exe (GnuCOBOL 3.2 64 bits)
        ↓ CALL "DBCONECT" / "DBSELECT" / "DBINSERT" / "DBUPDATE"
  Wrapper C — DB2HELPER.o (ODBC)
        ↓ SQLDriverConnect / SQLExecDirect
  IBM DB2 (Docker) — tabela DB2INST1.CLIENTES_COOPALF
```

O .NET grava a requisição em `REQUEST.DAT`, executa o `CLICORE.exe`, que chama funções C do wrapper `DB2HELPER` para acessar o DB2 via ODBC, e devolve a resposta em `RESPONSE.DAT`.

Detalhes completos em [`docs/arquitetura.md`](docs/arquitetura.md).

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [GnuCOBOL 3.2 64 bits](https://github.com/OCamlPro/superbol-artefacts/releases/download/gnucobol-3.2-aio-20240402/gnucobol-3.2-aio-20240402-user.msi) (SuperBOL All-in-One)
- IBM DB2 Community Edition (Docker)
- IBM DB2 ODBC Driver (CLI Driver)
- Docker Desktop
- Git

---

## Configuração do DB2

### 1. Sobe o container DB2

```cmd
docker run -itd --name db2 --privileged=true -p 50000:50000 -e LICENSE=accept -e DB2INST1_PASSWORD=Db2senha2026 -e DBNAME=BANCO icr.io/db2_community/db2
```

### 2. Cria a tabela

```cmd
docker exec -it db2 bash -c "su - db2inst1 -c 'db2start && db2 connect to BANCO && db2 \"CREATE TABLE CLIENTES_COOPALF (CLI_CODIGO INTEGER NOT NULL, CLI_NOME VARCHAR(40) NOT NULL, CLI_TELEFONE VARCHAR(15), CLI_EMAIL VARCHAR(50), PRIMARY KEY (CLI_CODIGO))\"'"
```

### 3. Configura o DSN ODBC

Abre o **Administrador de Fonte de Dados ODBC** (`odbcad32`) e cria um DSN de usuário:

- **Data source name:** `BANCODSN`
- **Driver:** IBM DB2 ODBC DRIVER
- **User ID:** `db2inst1`
- **Password:** `Db2senha2026`
- **Database:** `BANCO`
- **Hostname:** `localhost`
- **Port:** `50000`

### 4. Inicia o DB2 corretamente

O DB2 Community no Docker requer `ipclean` antes do `db2start` para o TCP funcionar:

```cmd
docker exec -it db2 bash -c "su - db2inst1 -c 'db2stop force; ipclean -a; db2start'"
```

---

## Como rodar o projeto

### 1. Clone o repositório

```bash
git clone https://github.com/OSIELJ/coopAlfa-modernizacao.git
cd coopAlfa-modernizacao
```

### 2. Compila o wrapper C

```cmd
"C:\Users\%USERNAME%\AppData\Local\GnuCOBOL\mingw64\bin\gcc.exe" -c src\cobol\DB2HELPER.c -o src\cobol\build\DB2HELPER.o -I"C:\Program Files\IBM\SQLLIB\include"
```

### 3. Compila o COBOL linkando com o wrapper

```cmd
"C:\Users\%USERNAME%\AppData\Local\GnuCOBOL\bin\cobc.exe" -x -fimplicit-init -I "src\cobol\copybook" src\cobol\CLICORE.cbl src\cobol\build\DB2HELPER.o -L "C:\Program Files\IBM\SQLLIB\lib" -lodbc32 -o src\cobol\build\CLICORE.exe
```

### 4. Copia os arquivos COBOL para a API

```cmd
mkdir src\dotnet\CoopAlfa.Api\cobol
copy src\cobol\build\CLICORE.exe src\dotnet\CoopAlfa.Api\cobol\
copy "C:\Users\%USERNAME%\AppData\Local\GnuCOBOL\bin\*.dll" src\dotnet\CoopAlfa.Api\cobol\
```

### 5. Executa a API

```cmd
cd src\dotnet\CoopAlfa.Api
dotnet run
```

A API sobe em `http://localhost:5125`.

### 6. Acessa a interface

```
http://localhost:5125/index.html
```

### 7. Acessa o Swagger

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

---

## Como rodar os testes

```cmd
cd src\dotnet
dotnet test CoopAlfa.slnx
```

---

## Qualidade de código

Análise com **SonarQube Community** (Docker local). Quality Gate: **Passed** — 0 Bugs, 0 Vulnerabilities, 0 Code Smells.

---

## CI/CD

Pipeline no GitHub Actions (`.github/workflows/build.yml`). Executa build e testes a cada push.

---

## Estrutura do projeto

```
coopAlfa-modernizacao/
├── .github/workflows/
│   └── build.yml              ← CI/CD GitHub Actions
├── docs/
│   ├── gifs/                  ← GIFs de demonstração
│   ├── evidencias/            ← Capturas de tela
│   ├── arquitetura.md
│   ├── plano-de-testes.md
│   └── relatorio-ia.md
├── src/
│   ├── cobol/
│   │   ├── copybook/
│   │   │   └── CLIENTE.cpy    ← Contrato de dados
│   │   ├── build/
│   │   │   ├── CLICORE.exe    ← Núcleo COBOL compilado
│   │   │   └── DB2HELPER.o    ← Wrapper C compilado
│   │   ├── CLICORE.cbl        ← Código-fonte COBOL
│   │   └── DB2HELPER.c        ← Wrapper C para ODBC/DB2
│   └── dotnet/
│       ├── CoopAlfa.Api/
│       │   ├── Controllers/   ← Endpoints REST
│       │   ├── Models/        ← DTOs
│       │   ├── Services/      ← Integração COBOL via processo
│       │   ├── cobol/         ← Runtime COBOL (gerado localmente)
│       │   └── wwwroot/       ← Interface do atendente (HTML)
│       └── CoopAlfa.Tests/    ← Testes xUnit
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
| GnuCOBOL 3.2 (64 bits) | Núcleo legado — regras de negócio |
| Wrapper C + ODBC | Ponte entre COBOL e DB2 |
| IBM DB2 Community | Persistência de dados |
| ASP.NET Core (.NET 10) | API REST e interface do atendente |
| xUnit + Moq | Testes automatizados |
| SonarQube Community | Análise de qualidade de código |
| GitHub Actions | CI/CD |
| Docker | DB2 e SonarQube |

---

## Autor

**Osiel** — Programa Acelera Maker, Montreal (2026)
