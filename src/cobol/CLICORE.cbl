      *================================================================*
      * CLICORE.CBL - Nucleo legado de cadastro de clientes            *
      * Cooperativa Financeira Alfa - Projeto de Modernizacao          *
      *                                                                *
      * Arquitetura: processo separado                                 *
      * Le a requisicao de REQUEST.DAT, processa, grava RESPONSE.DAT   *
      *                                                                *
      * Operacoes:                                                     *
      *   C = Consultar cliente pelo codigo                            *
      *   N = Cadastrar novo cliente                                   *
      *   A = Atualizar telefone e e-mail                              *
      *                                                                *
      * Return codes:                                                  *
      *   00 = Sucesso                                                 *
      *   01 = Nao encontrado / Codigo ja existe                       *
      *   02 = Erro interno                                            *
      *================================================================*
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLICORE.

       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT ARQUIVO-CLIENTES
               ASSIGN TO "CLIENTES.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS ARQ-CODIGO
               FILE STATUS IS WS-FILE-STATUS.

           SELECT ARQUIVO-REQUEST
               ASSIGN TO "REQUEST.DAT"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-REQ-STATUS.

           SELECT ARQUIVO-RESPONSE
               ASSIGN TO "RESPONSE.DAT"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-RESP-STATUS.

       DATA DIVISION.
       FILE SECTION.
       FD ARQUIVO-CLIENTES.
       01 REGISTRO-CLIENTE.
           05 ARQ-CODIGO           PIC 9(04).
           05 ARQ-NOME             PIC X(40).
           05 ARQ-TELEFONE         PIC X(15).
           05 ARQ-EMAIL            PIC X(50).

       FD ARQUIVO-REQUEST.
       01 REGISTRO-REQUEST         PIC X(200).

       FD ARQUIVO-RESPONSE.
       01 REGISTRO-RESPONSE        PIC X(200).

       WORKING-STORAGE SECTION.
       01 WS-FILE-STATUS           PIC X(02).
       01 WS-REQ-STATUS            PIC X(02).
       01 WS-RESP-STATUS           PIC X(02).

      * Estrutura da requisicao (entrada)
       01 WS-REQUEST.
           05 WS-OPERACAO          PIC X(01).
              88 OP-CONSULTAR      VALUE 'C'.
              88 OP-NOVO           VALUE 'N'.
              88 OP-ATUALIZAR      VALUE 'A'.
           05 WS-CODIGO            PIC 9(04).
           05 WS-NOME-REQ          PIC X(40).
           05 WS-TELEFONE-REQ      PIC X(15).
           05 WS-EMAIL-REQ         PIC X(50).

      * Estrutura da resposta (saida)
       01 WS-RESPONSE.
           05 WS-RETURN-CODE       PIC X(02).
           05 WS-NOME              PIC X(40).
           05 WS-TEL-OUT           PIC X(15).
           05 WS-EMAIL-OUT         PIC X(50).
           05 WS-MENSAGEM          PIC X(80).

       PROCEDURE DIVISION.
       INICIO.
           PERFORM LER-REQUEST
           EVALUATE TRUE
               WHEN OP-CONSULTAR
                   PERFORM CONSULTAR-CLIENTE
               WHEN OP-NOVO
                   PERFORM CADASTRAR-CLIENTE
               WHEN OP-ATUALIZAR
                   PERFORM ATUALIZAR-CLIENTE
               WHEN OTHER
                   MOVE '02' TO WS-RETURN-CODE
                   MOVE 'Operacao invalida'
                       TO WS-MENSAGEM
           END-EVALUATE
           PERFORM GRAVAR-RESPONSE
           STOP RUN.

      *----------------------------------------------------------------*
      * LER-REQUEST                                                    *
      *----------------------------------------------------------------*
       LER-REQUEST.
           INITIALIZE WS-REQUEST
           INITIALIZE WS-RESPONSE
           OPEN INPUT ARQUIVO-REQUEST
           READ ARQUIVO-REQUEST
               AT END
                   MOVE '02' TO WS-RETURN-CODE
           END-READ
           MOVE REGISTRO-REQUEST(1:1)   TO WS-OPERACAO
           MOVE REGISTRO-REQUEST(2:4)   TO WS-CODIGO
           MOVE REGISTRO-REQUEST(6:40)  TO WS-NOME-REQ
           MOVE REGISTRO-REQUEST(46:15) TO WS-TELEFONE-REQ
           MOVE REGISTRO-REQUEST(61:50) TO WS-EMAIL-REQ
           CLOSE ARQUIVO-REQUEST.

      *----------------------------------------------------------------*
      * CONSULTAR-CLIENTE                                              *
      *----------------------------------------------------------------*
       CONSULTAR-CLIENTE.
           OPEN INPUT ARQUIVO-CLIENTES
           MOVE WS-CODIGO TO ARQ-CODIGO
           READ ARQUIVO-CLIENTES
               INVALID KEY
                   MOVE '01' TO WS-RETURN-CODE
                   MOVE 'Cliente nao encontrado'
                       TO WS-MENSAGEM
               NOT INVALID KEY
                   MOVE ARQ-NOME      TO WS-NOME
                   MOVE ARQ-TELEFONE  TO WS-TEL-OUT
                   MOVE ARQ-EMAIL     TO WS-EMAIL-OUT
                   MOVE '00'          TO WS-RETURN-CODE
                   MOVE 'Consulta realizada com sucesso'
                       TO WS-MENSAGEM
           END-READ
           CLOSE ARQUIVO-CLIENTES.

      *----------------------------------------------------------------*
      * CADASTRAR-CLIENTE                                              *
      *----------------------------------------------------------------*
       CADASTRAR-CLIENTE.
           OPEN I-O ARQUIVO-CLIENTES
           MOVE WS-CODIGO       TO ARQ-CODIGO
           MOVE WS-NOME-REQ     TO ARQ-NOME
           MOVE WS-TELEFONE-REQ TO ARQ-TELEFONE
           MOVE WS-EMAIL-REQ    TO ARQ-EMAIL
           WRITE REGISTRO-CLIENTE
               INVALID KEY
                   MOVE '01' TO WS-RETURN-CODE
                   MOVE 'Codigo ja cadastrado'
                       TO WS-MENSAGEM
               NOT INVALID KEY
                   MOVE ARQ-NOME      TO WS-NOME
                   MOVE ARQ-TELEFONE  TO WS-TEL-OUT
                   MOVE ARQ-EMAIL     TO WS-EMAIL-OUT
                   MOVE '00'          TO WS-RETURN-CODE
                   MOVE 'Cliente cadastrado com sucesso'
                       TO WS-MENSAGEM
           END-WRITE
           CLOSE ARQUIVO-CLIENTES.

      *----------------------------------------------------------------*
      * ATUALIZAR-CLIENTE                                              *
      *----------------------------------------------------------------*
       ATUALIZAR-CLIENTE.
           OPEN I-O ARQUIVO-CLIENTES
           MOVE WS-CODIGO TO ARQ-CODIGO
           READ ARQUIVO-CLIENTES
               INVALID KEY
                   MOVE '01' TO WS-RETURN-CODE
                   MOVE 'Cliente nao encontrado'
                       TO WS-MENSAGEM
               NOT INVALID KEY
                   MOVE WS-TELEFONE-REQ TO ARQ-TELEFONE
                   MOVE WS-EMAIL-REQ    TO ARQ-EMAIL
                   REWRITE REGISTRO-CLIENTE
                       INVALID KEY
                           MOVE '02' TO WS-RETURN-CODE
                           MOVE 'Erro ao atualizar'
                               TO WS-MENSAGEM
                       NOT INVALID KEY
                           MOVE ARQ-NOME     TO WS-NOME
                           MOVE ARQ-TELEFONE TO WS-TEL-OUT
                           MOVE ARQ-EMAIL    TO WS-EMAIL-OUT
                           MOVE '00' TO WS-RETURN-CODE
                           MOVE 'Atualizacao realizada'
                               TO WS-MENSAGEM
                   END-REWRITE
           END-READ
           CLOSE ARQUIVO-CLIENTES.

      *----------------------------------------------------------------*
      * GRAVAR-RESPONSE                                                *
      *----------------------------------------------------------------*
       GRAVAR-RESPONSE.
           MOVE SPACES TO REGISTRO-RESPONSE
           STRING
               WS-RETURN-CODE
               WS-NOME
               WS-TEL-OUT
               WS-EMAIL-OUT
               WS-MENSAGEM
               DELIMITED BY SIZE
               INTO REGISTRO-RESPONSE
           END-STRING
           OPEN OUTPUT ARQUIVO-RESPONSE
           WRITE REGISTRO-RESPONSE
           CLOSE ARQUIVO-RESPONSE.
