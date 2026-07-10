      *================================================================*
      * CLIENTE.CPY - Estrutura de dados compartilhada                 *
      * Cooperativa Financeira Alfa - Projeto de Modernizacao          *
      *                                                                *
      * Definicao unica do contrato de dados entre a camada .NET e o   *
      * nucleo COBOL. Qualquer alteracao neste arquivo exige alteracao *
      * espelhada em ClienteContrato.cs (camada .NET).                 *
      *                                                                *
      * REQUEST.DAT  - 110 bytes - .NET escreve, COBOL le              *
      * RESPONSE.DAT - 187 bytes - COBOL escreve, .NET le              *
      *================================================================*

      *----------------------------------------------------------------*
      * REQUISICAO - layout de REQUEST.DAT                             *
      *                                                                *
      *   pos 001-001  operacao   PIC X(01)                            *
      *   pos 002-005  codigo     PIC X(04)                            *
      *   pos 006-045  nome       PIC X(40)                            *
      *   pos 046-060  telefone   PIC X(15)                            *
      *   pos 061-110  email      PIC X(50)                            *
      *----------------------------------------------------------------*
       01 WS-REQUEST.
           05 WS-OPERACAO          PIC X(01).
              88 OP-CONSULTAR      VALUE 'C'.
              88 OP-NOVO           VALUE 'N'.
              88 OP-ATUALIZAR      VALUE 'A'.
           05 WS-CODIGO            PIC X(04).
           05 WS-NOME-REQ          PIC X(40).
           05 WS-TELEFONE-REQ      PIC X(15).
           05 WS-EMAIL-REQ         PIC X(50).

      *----------------------------------------------------------------*
      * RESPOSTA - layout de RESPONSE.DAT                              *
      *                                                                *
      *   pos 001-002  return code PIC X(02)                           *
      *   pos 003-042  nome        PIC X(40)                           *
      *   pos 043-057  telefone    PIC X(15)                           *
      *   pos 058-107  email       PIC X(50)                           *
      *   pos 108-187  mensagem    PIC X(80)                           *
      *----------------------------------------------------------------*
       01 WS-RESPONSE.
           05 WS-RETURN-CODE       PIC X(02).
              88 RC-SUCESSO        VALUE '00'.
              88 RC-NAO-ENCONTRADO VALUE '01'.
              88 RC-ERRO           VALUE '02'.
           05 WS-NOME              PIC X(40).
           05 WS-TEL-OUT           PIC X(15).
           05 WS-EMAIL-OUT         PIC X(50).
           05 WS-MENSAGEM          PIC X(80).
