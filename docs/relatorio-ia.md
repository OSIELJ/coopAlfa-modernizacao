# Relatório de Utilização de Inteligência Artificial
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Ferramenta Utilizada

**Claude (Anthropic) — Claude Sonnet 4.6**
Utilizado via interface web em https://claude.ai em momentos específicos do desenvolvimento, conforme descrito abaixo.

---

## 2. Postura Adotada

O desenvolvimento foi realizado de forma autônoma — estudo dos conceitos, tomada de decisões arquiteturais e implementação do código. A IA foi acionada em momentos pontuais: para aprofundar discussões arquiteturais já iniciadas, depurar erros específicos e validar decisões já tomadas.

---

## 3. Interações Documentadas

---

### Prompt 1 — Discussão de Arquiteturas (após estudo prévio)

**Contexto:**
Antes de acionar a IA, estudei o material do treinamento (Semanas 10 e 11) e pesquisei sobre as formas de integrar .NET e COBOL fora do mainframe. Com base nisso, cheguei a três arquiteturas candidatas e levei para a IA para uma discussão mais aprofundada — não para que ela escolhesse, mas para que me ajudasse a enxergar pontos cegos.

**Prompt utilizado:**
> "Estudei três arquiteturas possíveis para integrar .NET e COBOL neste projeto. Quero uma discussão mais ampla sobre cenários relevantes, pontos fortes e fracos de cada uma:
>
> Arquitetura 1 — COBOL compilado como .dll, chamado pelo .NET via P/Invoke. Comunicação in-process.
>
> Arquitetura 2 — COBOL como executável separado. .NET invoca via Process e troca dados por arquivo ou stdout.
>
> Arquitetura 3 — .NET Web API REST na frente, COBOL via P/Invoke por baixo. Interface HTML consome a API.
>
> Contexto: projeto individual, prazo curto, precisa demonstrar o padrão z/OS Connect do treinamento, e a Aline (cliente) confirmou que o COBOL deve ser quem persiste os dados."

**Resposta obtida:**
A IA estruturou a discussão em torno de quatro dimensões: acoplamento, testabilidade, fidelidade ao padrão mainframe e risco de prazo.

Sobre a **Arquitetura 1**, destacou como ponto forte a chamada in-process sem overhead de processo, e a fidelidade ao padrão de módulos COBOL chamados por outros programas. Como ponto fraco, apontou que o marshalling de dados entre C# e COBOL (campos `PIC X`, endianness, padding) é uma fonte de bugs silenciosos difíceis de diagnosticar — o código compila mas os dados chegam corrompidos se o `[StructLayout]` não estiver correto.

Sobre a **Arquitetura 2**, reconheceu a simplicidade de implementação mas foi direta nos problemas: criar um processo por requisição é inviável em produção, a troca de dados por arquivo é frágil, e não existe paralelismo seguro. Cenário relevante levantado pela IA: se dois atendentes buscarem o mesmo cliente simultaneamente, podem sobrescrever o arquivo de dados um do outro. Ponto que eu não havia considerado.

Sobre a **Arquitetura 3**, apontou como ponto forte decisivo a separação de responsabilidades: o .NET faz validação e exposição REST, o COBOL faz regras de negócio e persistência. Isso reproduz exatamente o padrão do z/OS Connect — onde uma camada moderna expõe o COBOL como API sem que o programa COBOL saiba que está sendo chamado por REST. Como ponto fraco, sinalizou que o P/Invoke ainda existe por baixo, então os riscos de marshalling da Arquitetura 1 persistem, só ficam encapsulados no `ClienteService`.

**Análise crítica da resposta:**
A discussão foi valiosa. O ponto sobre concorrência na Arquitetura 2 era um risco real que eu não havia mapeado. A comparação com o z/OS Connect foi o argumento que me convenceu definitivamente pela Arquitetura 3 — não por ser "mais bonita", mas por ser a que melhor demonstra o padrão estudado no treinamento.

O que a IA não trouxe espontaneamente: não mencionou que o GnuCOBOL tem limitações no formato fixo (colunas 1-72) que impactam diretamente a escrita do código. Esse ponto só apareceu como problema real na compilação, não na discussão arquitetural.

**Impacto no projeto:**
A Arquitetura 3 foi adotada. A justificativa do z/OS Connect foi incorporada ao Documento de Arquitetura. O risco de marshalling levantado pela IA orientou a atenção especial ao `[StructLayout(Pack=1, CharSet=CharSet.Ansi)]` na `ClienteStruct.cs`.

---

### Prompt 2 — Depuração: erro de compilação COBOL (colunas fixas)

**Contexto:**
Ao compilar o `CLICORE.cbl` pela primeira vez, recebi o erro `'WS-MENSAG' is not defined`. O campo estava declarado na copybook como `WS-MENSAGEM` (10 caracteres). O erro não fazia sentido à primeira vista — o campo existia. Tentei resolver sozinho por cerca de 20 minutos sem sucesso antes de acionar a IA.

**Prompt utilizado:**
> "Estou compilando COBOL com GnuCOBOL e recebo o erro `'WS-MENSAG' is not defined`. O campo se chama `WS-MENSAGEM` e está declarado corretamente na copybook. O compilador está encontrando a copybook (confirmei com `-I`). O que pode estar causando esse truncamento do nome?"

**Resposta obtida:**
A IA identificou imediatamente: o COBOL em formato fixo tem um limite rígido de 72 caracteres por linha (colunas 1-72 são código; 73-80 são reservadas para numeração de sequência, ignoradas pelo compilador moderno mas ainda respeitadas em formato fixo). A linha `MOVE 'Erro ao atualizar cliente' TO WS-MENSAGEM` ultrapassava esse limite, e o compilador simplesmente cortava o resto — incluindo parte do nome `WS-MENSAGEM`, que virava `WS-MENSAG`.

Solução: quebrar a linha em duas:
```cobol
           MOVE 'Erro ao atualizar cliente'
               TO WS-MENSAGEM
```

**Análise crítica:**
O diagnóstico foi certeiro e imediato — o que eu não consegui em 20 minutos a IA resolveu em segundos porque reconheceu um padrão clássico de erro COBOL. Porém, é importante registrar que esse era um erro que eu deveria ter antecipado ao escrever o código — o limite de 72 colunas é um conceito básico do COBOL fixo que estava no material do treinamento. A IA foi útil aqui como "segunda opinião técnica", não como substituta do conhecimento.

Um detalhe importante: a IA inicialmente sugeriu usar a flag `-free` (formato livre) como solução alternativa. Testei e não funcionou porque o arquivo já tinha indentação de formato fixo. Precisei informar isso para que a IA ajustasse a sugestão para quebrar a linha — o que mostra que a IA precisa do contexto real para ser precisa em problemas de compilador.

**Impacto no projeto:**
O erro foi corrigido. Todas as linhas do `CLICORE.cbl` foram revisadas para respeitar o limite de 72 colunas. O build passou sem erros na sequência.

---

### Prompt 3 — Depuração: conflito de pacotes NuGet

**Contexto:**
Ao adicionar o `Swashbuckle.AspNetCore` para o Swagger, recebi um erro de conflito com o `Microsoft.OpenApi` que já havia sido adicionado manualmente em versão anterior. O projeto parou de buildar.

**Prompt utilizado:**
> "Ao adicionar Swashbuckle.AspNetCore ao projeto .NET 10, recebi: `NU1605: Downgrade de pacote detectado: Microsoft.OpenApi de 2.7.5 para 2.0.1`. O Swashbuckle precisa da versão 2.7.5 mas eu havia adicionado manualmente a 2.0.1. Como resolver sem quebrar as dependências?"

**Resposta obtida:**
A IA explicou que o erro ocorre porque o NuGet tem regras de resolução de versão e não aceita downgrade silencioso — quando duas dependências pedem versões diferentes, o build falha com `TreatWarningsAsErrors`. A solução é remover a referência manual ao `Microsoft.OpenApi` e deixar o Swashbuckle resolver a versão correta transitivamente, ou então fixar a versão na que o Swashbuckle precisa (`2.7.5`).

```cmd
dotnet remove package Microsoft.OpenApi
dotnet add package Microsoft.OpenApi --version 2.7.5
```

**Análise crítica:**
A resposta foi direta e correta. O ponto interessante é que a IA explicou o mecanismo por trás do erro (resolução transitiva de dependências no NuGet) e não apenas o comando para corrigir. Isso foi útil porque o mesmo tipo de conflito pode aparecer em outros pacotes no futuro — entender o mecanismo previne o problema, não só resolve o sintoma.

Não há pontos negativos nessa interação. O problema era claro, a solução foi precisa e funcionou na primeira tentativa.

**Impacto no projeto:**
O conflito foi resolvido. O build voltou a funcionar com `Microsoft.OpenApi 2.7.5` + `Swashbuckle.AspNetCore 10.2.3`.

---

### Prompt 4 — Depuração: Code Smells no SonarQube

**Contexto:**
Após a primeira análise do SonarQube, apareceram Code Smells que não conhecia: S6444 (Regex sem timeout) e S1066 (if aninhados). Quis entender o motivo técnico por trás de cada um antes de corrigir.

**Prompt utilizado:**
> "O SonarQube reportou dois Code Smells no meu C#: S6444 diz para adicionar timeout no Regex.IsMatch, e S1066 diz para mesclar if aninhados. Quero entender o motivo técnico por trás de cada um, não apenas como corrigir."

**Resposta obtida:**
Para o **S6444**: expressões regulares podem sofrer *catastrophic backtracking* — um fenômeno onde certos padrões de regex com entrada maliciosa fazem o motor de regex executar por tempo exponencial, bloqueando a thread indefinidamente. O `TimeSpan.FromSeconds(1)` define um limite máximo de execução, lançando `RegexMatchTimeoutException` se ultrapassado. Além disso, declarar o Regex como campo estático evita recompilação do padrão a cada chamada.

Para o **S1066**: o if aninhado `if (A) { if (B) { } }` e o if mesclado `if (A && B) { }` são semanticamente equivalentes, mas o segundo é mais legível e tem complexidade ciclomática menor — o que afeta métricas de manutenibilidade e testabilidade do código.

**Análise crítica:**
A explicação sobre catastrophic backtracking foi o ponto mais valioso dessa interação. Não era algo que eu conhecia, e a IA trouxe um exemplo concreto de como um padrão aparentemente simples pode virar uma vulnerabilidade de negação de serviço (ReDoS). Isso mudou minha perspectiva sobre validação com Regex — não é apenas uma questão de estilo, é uma questão de segurança.

A explicação sobre S1066 foi mais trivial, mas a menção à complexidade ciclomática foi um detalhe útil para o Documento de Arquitetura.

**Impacto no projeto:**
Ambos os Code Smells foram corrigidos com entendimento do motivo, não apenas mecanicamente. O Quality Gate passou de 2 Code Smells para 0. A explicação sobre ReDoS foi adicionada como comentário no código para documentar a intenção do timeout.

---

## 5. Considerações Finais

### O que a IA contribuiu de forma genuína
- A discussão arquitetural do Prompt 1 trouxe o argumento do z/OS Connect que não estava explícito no meu raciocínio inicial, e o risco de concorrência na Arquitetura 2 que eu não havia mapeado.
- A explicação sobre catastrophic backtracking (ReDoS) no Prompt 4 foi conhecimento novo que mudou a forma como penso sobre validação com Regex.
- A identificação imediata do problema de colunas fixas no COBOL (Prompt 2) economizou tempo real de depuração.

### Onde a IA foi limitada ou precisou de correção
- Não previu o problema das colunas fixas do COBOL na geração inicial do código — erro que só apareceu na compilação.
- Sugestões para problemas de compilador sem o output de erro real foram genéricas e às vezes incorretas (sugeriu `-free` antes de entender o contexto).
- Detalhes específicos de ambiente Windows (paths, ordem de passos) frequentemente precisaram de revisão manual.
- A IA não alertou sobre a necessidade de copiar a `CLICORE.dll` para o diretório de execução da API — um detalhe crítico que causaria falha em tempo de execução.

### Síntese
A IA foi mais útil como **interlocutora para discussões técnicas** do que como geradora de código. As conversas sobre arquitetura e sobre o motivo dos Code Smells trouxeram valor real de aprendizado. O código gerado foi útil como ponto de partida, mas sempre exigiu revisão e ajuste — o que é esperado e saudável. Usar a IA sem entender o código gerado seria um risco técnico e acadêmico: risco técnico porque bugs sutis passam despercebidos, e risco acadêmico porque na defesa do projeto é preciso explicar cada decisão com as próprias palavras.
