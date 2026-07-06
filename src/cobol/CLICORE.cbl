      *================================================================*
      * CLICORE.CBL - Nucleo legado de cadastro de clientes            *
      * Cooperativa Financeira Alfa - Projeto de Modernizacao          *
      *                                                                *
      * Operacoes suportadas:                                          *
      *   C = Consultar cliente pelo codigo                            *
      *   A = Atualizar telefone e e-mail do cliente                   *
      *                                                                *
      * Return codes:                                                  *
      *   00 = Sucesso                                                 *
      *   01 = Cliente nao encontrado                                  *
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

       DATA DIVISION.
       FILE SECTION.
       FD ARQUIVO-CLIENTES.
       01 REGISTRO-CLIENTE.
           05 ARQ-CODIGO           PIC 9(04).
           05 ARQ-NOME             PIC X(40).
           05 ARQ-TELEFONE         PIC X(15).
           05 ARQ-EMAIL            PIC X(50).

       WORKING-STORAGE SECTION.
       01 WS-FILE-STATUS           PIC X(02).
          88 FS-SUCESSO            VALUE '00'.
          88 FS-NAO-ENCONTRADO     VALUE '23'.
          88 FS-FIM-ARQUIVO        VALUE '10'.

       COPY "CLIENTE.cpy".

       PROCEDURE DIVISION.
       INICIO.
           EVALUATE TRUE
               WHEN OP-CONSULTAR
                   PERFORM CONSULTAR-CLIENTE
               WHEN OP-ATUALIZAR
                   PERFORM ATUALIZAR-CLIENTE
               WHEN OTHER
                   MOVE '02' TO WS-RETURN-CODE
                   MOVE 'Operacao invalida'
                       TO WS-MENSAGEM
           END-EVALUATE
           STOP RUN.

      *----------------------------------------------------------------*
      * CONSULTAR-CLIENTE                                              *
      * Le o registro do arquivo pelo codigo e devolve os dados        *
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
                   MOVE ARQ-TELEFONE  TO WS-TELEFONE
                   MOVE ARQ-EMAIL     TO WS-EMAIL
                   MOVE '00'          TO WS-RETURN-CODE
                   MOVE 'Consulta realizada com sucesso'
                       TO WS-MENSAGEM
           END-READ
           CLOSE ARQUIVO-CLIENTES.

      *----------------------------------------------------------------*
      * ATUALIZAR-CLIENTE                                              *
      * Atualiza telefone e e-mail mantendo os demais dados intactos   *
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
                   MOVE WS-TELEFONE TO ARQ-TELEFONE
                   MOVE WS-EMAIL    TO ARQ-EMAIL
                   REWRITE REGISTRO-CLIENTE
                       INVALID KEY
                           MOVE '02' TO WS-RETURN-CODE
                           MOVE 'Erro ao atualizar cliente'
                               TO WS-MENSAGEM
                       NOT INVALID KEY
                           MOVE '00' TO WS-RETURN-CODE
                           MOVE 'Atualizacao realizada'
                               TO WS-MENSAGEM
                   END-REWRITE
           END-READ
           CLOSE ARQUIVO-CLIENTES.
