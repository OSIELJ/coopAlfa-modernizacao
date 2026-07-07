# Projeto 5 — Processamento de Transações Bancárias (COBOL + JCL)

Projeto da **Semana 7** do programa Montreal Acelera Maker (trilha COBOL/Mainframe).

Um banco precisa processar diariamente um arquivo de transações de débito e
crédito, atualizando o saldo dos clientes e gerando relatórios. O job foi
desenvolvido e executado em ambiente **MVS 3.8j (TK5 / Hercules)** via TSO.

## O que o job faz

O `PROJETO5.jcl` executa 4 passos em sequência:

| Passo | Programa | Função |
|---|---|---|
| STEP0 | IEFBR14 | Apaga os arquivos de saída de execuções anteriores |
| SORTCLI | SORT | Ordena o arquivo de clientes por ID |
| SORTTRX | SORT | Ordena o arquivo de transações por ID |
| RUN | COBUCLG | Compila, linkedita e executa o programa COBOL |

O programa COBOL (`PROJETO5.cbl`) processa os dois arquivos ordenados em uma
única passada, usando a técnica clássica de **match/merge** (casamento de
arquivos por chave):

- IDs iguais → valida e aplica a transação (crédito soma, débito subtrai);
- ID do cliente menor → cliente terminou: grava na saída com saldo atualizado
  e imprime o relatório de créditos/débitos dele;
- ID da transação menor → transação órfã (cliente inexistente): registra erro.

## Validações implementadas

Toda inconsistência é gravada no arquivo de erros, sem interromper o job:

1. **Cliente inexistente** — `ERRO: CLIENTE NAO ENCONTRADO - ID nnnnn`
2. **Tipo de transação inválido** (≠ C/D) — `ERRO: TIPO DE TRANSACAO INVALIDO - ID nnnnn`
3. **Valor zerado** — `ERRO: VALOR DE TRANSACAO INVALIDO - ID nnnnn`
4. **Saldo insuficiente** (débito > saldo; transação não é aplicada) —
   `ERRO: SALDO INSUFICIENTE - ID nnnnn`

## Estrutura do repositório

```
src/              fonte COBOL e JCL
dados/entrada/    arquivos de entrada (clientes e transações)
dados/saida/      arquivos gerados pela execução (saldos atualizados e erros)
evidencias/       prints da execução real no TK5
```

## Resultado da execução

Job executado via `SUBMIT 'HERC01.JCL(PROJETO5)'` com **MAX COND CODE 0004**
(RC 4 apenas no passo de compilação — warnings do compilador ANS COBOL,
sem impacto; demais passos RC 0000).

Estatísticas produzidas:

```
CLIENTES PROCESSADOS.....: 000003
TRANSACOES PROCESSADAS...: 000007
CREDITOS PROCESSADOS.....: 000001
DEBITOS PROCESSADOS......: 000002
ERROS ENCONTRADOS........: 000004
```

Saldos finais gravados em `dados/saida/SAIDA.txt`:

| Cliente | Saldo inicial | Saldo final |
|---|---|---|
| 00123 JOAO SILVA | 000010000 | 000010300 |
| 00456 MARIA SOUZA | 000025000 | 000024000 |
| 00789 CARLOS PEREIRA | 000005000 | 000005000 |

## Evidências da execução

Prints capturados no TK5 (terminal TN3270) durante a execução real do job.

**Arquivos de entrada no mainframe** — clientes e transações (propositalmente
fora de ordem, com os casos de erro embutidos):

![Arquivo de clientes](evidencias/01-entrada-clientes.png)

![Arquivo de transações](evidencias/02-entrada-transacoes.png)

**Datasets criados pela execução** — entradas transferidas e saídas geradas
pelo job (`SAIDA` e `ERROS` catalogados em disco):

![Datasets do projeto](evidencias/03-datasets-criados.png)

**Job no spool do JES2:**

![Job no spool](evidencias/04-job-spool.png)

**Relatório por cliente e estatísticas** (SYSOUT do passo GO):

![Relatório e estatísticas](evidencias/05-relatorio-estatisticas.png)

**Arquivo de saída** — mesmo layout do cadastro, saldos atualizados
(00123 recebeu +500 e −200; 00456 teve −1000; 00789 ficou intacto porque
o débito foi rejeitado por saldo insuficiente):

![Saída com saldos atualizados](evidencias/06-saida-saldos.png)

**Arquivo de erros** — as 4 validações exigidas pelo enunciado, todas
disparadas pela massa de teste:

![Arquivo de erros](evidencias/07-arquivo-erros.png)

## Como executar (TK5)

1. Subir o TK5 (Hercules) e logar no TSO.
2. Transferir `src/PROJETO5.jcl` para um membro de PDS
   (ex.: `HERC01.JCL(PROJETO5)`) via IND\$FILE ou editor do TSO.
3. No TSO: `SUBMIT 'HERC01.JCL(PROJETO5)'`.
4. Conferir o resultado no spool (RFE, opção 3.8) e os datasets
   `HERC01.PROJ5.SAIDA` e `HERC01.PROJ5.ERROS` (Browse).

Os dados de entrada já estão embutidos no JCL (cartões `DD *`), portanto o
job é autocontido — basta um único membro para reproduzir a execução.

## Detalhes técnicos do ambiente

- Compilador: ANS COBOL (IKFCBL00) via procedure `COBUCLG` — por ser um
  dialeto antigo, o fonte evita recursos do COBOL-74+ (sem `STRING`,
  `EVALUATE` ou terminadores `END-IF`).
- Sort: OS/360 Sort/Merge — exige o DD `SORTLIB` apontando para
  `SYS1.SORTLIB` em cada passo de ordenação.
- Saídas em `SYSOUT=H` (classe held) para consulta no spool via RFE.
