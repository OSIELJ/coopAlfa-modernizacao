# Relatório de Utilização de Inteligência Artificial
## Projeto: Modernização do Sistema de Cadastro de Clientes
### Cooperativa Financeira Alfa

---

## 1. Ferramenta Utilizada

**Claude (Anthropic) — Claude Sonnet 4.6**
Utilizado via interface web em https://claude.ai em momentos específicos do desenvolvimento.

---

## 2. Postura Adotada

A IA **não foi utilizada para conduzir o projeto**. O desenvolvimento foi realizado de forma autônoma — estudo dos conceitos, tomada de decisões arquiteturais e implementação do código. A IA foi acionada em momentos pontuais: para aprofundar discussões já iniciadas, depurar erros específicos e validar decisões já tomadas.

---

## 3. Interações Documentadas

---

### Prompt 1 — Discussão de Arquiteturas (após estudo prévio)

**Contexto:**
Após estudar o material do treinamento (Semanas 10 e 11) e pesquisar sobre integração .NET/COBOL, cheguei a três arquiteturas candidatas e levei para a IA para discussão aprofundada.

**Prompt utilizado:**
> "Estudei três arquiteturas possíveis para integrar .NET e COBOL. Quero uma discussão mais ampla sobre cenários relevantes, pontos fortes e fracos de cada uma:
> Arquitetura 1 — COBOL como .dll via P/Invoke.
> Arquitetura 2 — COBOL como executável separado, .NET via Process e arquivo.
> Arquitetura 3 — .NET Web API REST na frente, COBOL via P/Invoke por baixo."

**Resposta obtida:**
A IA estruturou a discussão em torno de acoplamento, testabilidade, fidelidade ao padrão mainframe e risco de prazo. Destacou que a Arquitetura 2 (processo separado) tem risco de concorrência — dois atendentes acessando o mesmo arquivo simultaneamente. Recomendou a Arquitetura 3 com P/Invoke como a que melhor reproduz o padrão z/OS Connect.

**Análise crítica:**
A discussão foi valiosa. O risco de concorrência na Arquitetura 2 era um ponto que eu não havia mapeado — e que resolvemos com um `lock` no `ClienteService`. A comparação com o z/OS Connect foi pertinente. Porém, a IA não antecipou os problemas de compatibilidade 32/64 bits do P/Invoke no Windows com GnuCOBOL, que só apareceram na implementação.

**Impacto no projeto:**
Iniciamos com a Arquitetura 3 (P/Invoke). Os problemas de runtime levaram à revisão dessa decisão no Prompt 6.

---

### Prompt 2 — Geração da Copybook e Struct C#

**Objetivo:** Criar o contrato de dados compartilhado com alinhamento de memória correto.

**Prompt utilizado:**
> "Preciso criar uma copybook COBOL e sua equivalente struct C# para P/Invoke. Os campos são: operação (1), código (4), nome (40), telefone (15), e-mail (50), return code (2), mensagem (80). Como garantir alinhamento correto?"

**Resposta obtida:**
Gerou a copybook com `PIC X` e `PIC 9`, e a struct C# com `[StructLayout(Pack=1, CharSet=Ansi)]`. Explicou por que `Pack=1` remove o padding automático do C# e por que `CharSet.Ansi` garante 1 byte por caractere.

**Análise crítica:**
Tecnicamente correto. O detalhe do `Pack=1` é crítico. A IA não alertou sobre o problema de newline no final da copybook e o limite de 72 colunas do COBOL fixo, que causaram erros de compilação descobertos na prática.

**Impacto no projeto:**
Copybook e struct adotadas. Ajustes manuais necessários para newline e colunas.

---

### Prompt 3 — Depuração: colunas fixas do COBOL

**Objetivo:** Resolver o erro `'WS-MENSAG' is not defined` na compilação.

**Prompt utilizado:**
> "Compilando CLICORE.cbl recebo `'WS-MENSAG' is not defined`. O campo se chama WS-MENSAGEM e está na copybook. O que pode causar esse truncamento?"

**Resposta obtida:**
Identificou o limite de 72 colunas do COBOL formato fixo — a linha `MOVE 'mensagem longa' TO WS-MENSAGEM` ultrapassava o limite e o compilador cortava o nome do campo. Solução: quebrar em duas linhas.

**Análise crítica:**
Diagnóstico certeiro e imediato. A IA inicialmente sugeriu `-free` (formato livre) sem entender o contexto — corrigiu após o feedback. Mostra que para problemas de compilador, o output de erro real é essencial.

**Impacto no projeto:**
Problema resolvido. Todas as linhas revisadas para respeitar 72 colunas.

---

### Prompt 4 — Correção de Code Smells no SonarQube

**Objetivo:** Entender e corrigir S6444 (Regex sem timeout) e S1066 (if aninhados).

**Prompt utilizado:**
> "O SonarQube reportou S6444 (Pass a timeout to limit the execution time) e S1066 (Merge this if statement with the enclosing one). Quero entender o motivo técnico, não apenas como corrigir."

**Resposta obtida:**
Para S6444: Regex sem timeout é vulnerável a *catastrophic backtracking* (ReDoS) — padrões maliciosos podem travar a thread indefinidamente. A solução com `TimeSpan.FromSeconds(1)` define um limite máximo. Para S1066: if aninhados aumentam a complexidade ciclomática; mesclar com `&&` simplifica sem alterar a semântica.

**Análise crítica:**
A explicação sobre ReDoS foi o ponto mais valioso. Não era um conhecimento prévio e mudou a perspectiva sobre validação com Regex — não é estilo, é segurança. Sem pontos negativos nessa interação.

**Impacto no projeto:**
Ambas as correções aplicadas. Quality Gate passou de 2 Code Smells para 0.

---

### Prompt 5 — Geração dos Testes xUnit

**Objetivo:** Criar testes isolados do COBOL.

**Prompt utilizado:**
> "Preciso de testes xUnit para o ClientesController sem depender do COBOL. Como isolar a lógica de negócio do processo externo?"

**Resposta obtida:**
Sugeriu extrair `IClienteService`, usar `Mock<IClienteService>` do Moq nos testes, e registrar a interface no DI. Gerou 18 casos de teste cobrindo todos os cenários.

**Análise crítica:**
Abordagem correta e padrão da indústria. A IA usou `[Theory]` com `[InlineData]` para múltiplos valores inválidos sem ser solicitada — boa decisão autônoma. Não mencionou que o construtor do Controller precisaria ser atualizado para usar a interface — descoberto na compilação.

**Impacto no projeto:**
18 testes implementados, todos passando.

---

### Prompt 6 — Decisão de Mudar para Processo Separado

**Contexto:**
Após várias tentativas de P/Invoke com GnuCOBOL (BadImageFormatException 32/64 bits, Access Violation 0xC0000005 com struct e IntPtr), a abordagem precisou ser revisada.

**Prompt utilizado:**
> "O P/Invoke com GnuCOBOL no Windows continua falhando com Access Violation mesmo com IntPtr e byte arrays. Quais são as alternativas reais para integrar .NET e COBOL sem P/Invoke? Qual é mais usada no mercado e mais fiel ao padrão mainframe?"

**Resposta obtida:**
A IA apresentou a alternativa de processo separado com troca de dados por arquivo. Explicou que esse padrão é o mais fiel ao mainframe real — no z/OS, sistemas legados são integrados via jobs batch que leem e gravam datasets, não via chamadas in-process. O P/Invoke seria adequado para COBOL compilado em ambiente controlado, mas o processo separado representa melhor o comportamento de um sistema legado real.

**Análise crítica:**
Esta foi a interação mais importante do projeto. A IA não apenas sugeriu uma alternativa técnica — ela reencuadrou a mudança como uma **decisão arquitetural mais adequada ao cenário**, não como uma limitação. O argumento de que "no mainframe real a integração é feita por datasets, não por chamadas in-process" é defensável e alinhado com o que o treinamento cobriu. Transformou uma dificuldade técnica em uma escolha consciente e justificável.

**Impacto no projeto:**
Mudança para processo separado adotada. CLICORE reescrito para ler `REQUEST.DAT` e gravar `RESPONSE.DAT`. Sistema funcionando end-to-end: navegador → API .NET → CLICORE.exe → CLIENTES.DAT → resposta.

---

## 4. Considerações Finais

### O que a IA contribuiu de forma genuína
- A discussão arquitetural do Prompt 1 trouxe o risco de concorrência que não estava mapeado
- A explicação sobre ReDoS (Prompt 4) foi conhecimento novo que mudou a perspectiva sobre segurança
- O reencuadramento da mudança arquitetural (Prompt 6) transformou uma limitação em decisão justificável

### Onde a IA foi limitada
- Não previu os problemas de compatibilidade 32/64 bits do P/Invoke com GnuCOBOL no Windows
- Sugestões para problemas de compilador sem o output real foram imprecisas
- Não alertou sobre o limite de 72 colunas do COBOL fixo

### Síntese
A IA foi mais útil como **interlocutora para discussões técnicas** e **reencuadradora de problemas** do que como geradora de código. O código gerado sempre exigiu revisão e ajuste. A postura adotada foi usar a IA como acelerador e interlocutora — nunca como autora da solução.
