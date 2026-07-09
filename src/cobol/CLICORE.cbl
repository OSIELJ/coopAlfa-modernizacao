      *================================================================*
      * CLICORE.CBL - Nucleo legado de cadastro de clientes            *
      * Cooperativa Financeira Alfa - Projeto de Modernizacao          *
      *                                                                *
      * Arquitetura: processo separado + DB2 via wrapper C (ODBC)      *
      * Le REQUEST.DAT, chama DB2HELPER.dll, grava RESPONSE.DAT        *
      *                                                                *
      * Operacoes:                                                     *
      *   C = Consultar cliente pelo codigo                            *
      *   N = Cadastrar novo cliente                                   *
      *   A = Atualizar telefone e e-mail                              *
      *================================================================*
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLICORE.

       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
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
       FD ARQUIVO-REQUEST.
       01 REGISTRO-REQUEST         PIC X(200).

       FD ARQUIVO-RESPONSE.
       01 REGISTRO-RESPONSE        PIC X(200).

       WORKING-STORAGE SECTION.
       01 WS-REQ-STATUS            PIC X(02).
       01 WS-RESP-STATUS           PIC X(02).

      * Estrutura da requisicao (entrada)
       01 WS-REQUEST.
           05 WS-OPERACAO          PIC X(01).
              88 OP-CONSULTAR      VALUE 'C'.
              88 OP-NOVO           VALUE 'N'.
              88 OP-ATUALIZAR      VALUE 'A'.
           05 WS-CODIGO            PIC X(04).
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

      * Return code do helper
       01 WS-DB-RC                 PIC X(02).

       PROCEDURE DIVISION.
       INICIO.
           PERFORM LER-REQUEST
           PERFORM CONECTAR-DB2
           IF WS-DB-RC = '00'
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
               PERFORM DESCONECTAR-DB2
           ELSE
               MOVE '02' TO WS-RETURN-CODE
               MOVE 'Erro ao conectar no DB2'
                   TO WS-MENSAGEM
           END-IF
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
                   MOVE 'Erro ao ler requisicao'
                       TO WS-MENSAGEM
           END-READ
           MOVE REGISTRO-REQUEST(1:1)   TO WS-OPERACAO
           MOVE REGISTRO-REQUEST(2:4)   TO WS-CODIGO
           MOVE REGISTRO-REQUEST(6:40)  TO WS-NOME-REQ
           MOVE REGISTRO-REQUEST(46:15) TO WS-TELEFONE-REQ
           MOVE REGISTRO-REQUEST(61:50) TO WS-EMAIL-REQ
           CLOSE ARQUIVO-REQUEST.

      *----------------------------------------------------------------*
      * CONECTAR-DB2 - chama wrapper C                                 *
      *----------------------------------------------------------------*
       CONECTAR-DB2.
           MOVE SPACES TO WS-DB-RC
           CALL "DBCONECT" USING
               WS-DB-RC
           END-CALL.

      *----------------------------------------------------------------*
      * DESCONECTAR-DB2                                                *
      *----------------------------------------------------------------*
       DESCONECTAR-DB2.
           CALL "DBDISCON" USING
               WS-DB-RC
           END-CALL.

      *----------------------------------------------------------------*
      * CONSULTAR-CLIENTE                                              *
      *----------------------------------------------------------------*
       CONSULTAR-CLIENTE.
           MOVE SPACES TO WS-NOME
           MOVE SPACES TO WS-TEL-OUT
           MOVE SPACES TO WS-EMAIL-OUT
           MOVE SPACES TO WS-DB-RC
           CALL "DBSELECT" USING
               WS-CODIGO
               WS-NOME
               WS-TEL-OUT
               WS-EMAIL-OUT
               WS-DB-RC
           END-CALL
           MOVE WS-DB-RC TO WS-RETURN-CODE
           EVALUATE WS-DB-RC
               WHEN '00'
                   MOVE 'Consulta realizada com sucesso'
                       TO WS-MENSAGEM
               WHEN '01'
                   MOVE 'Cliente nao encontrado'
                       TO WS-MENSAGEM
               WHEN OTHER
                   MOVE 'Erro ao consultar cliente'
                       TO WS-MENSAGEM
           END-EVALUATE.

      *----------------------------------------------------------------*
      * CADASTRAR-CLIENTE                                              *
      *----------------------------------------------------------------*
       CADASTRAR-CLIENTE.
           MOVE SPACES TO WS-DB-RC
           CALL "DBINSERT" USING
               WS-CODIGO
               WS-NOME-REQ
               WS-TELEFONE-REQ
               WS-EMAIL-REQ
               WS-DB-RC
           END-CALL
           MOVE WS-DB-RC TO WS-RETURN-CODE
           EVALUATE WS-DB-RC
               WHEN '00'
                   MOVE WS-NOME-REQ     TO WS-NOME
                   MOVE WS-TELEFONE-REQ TO WS-TEL-OUT
                   MOVE WS-EMAIL-REQ    TO WS-EMAIL-OUT
                   MOVE 'Cliente cadastrado com sucesso'
                       TO WS-MENSAGEM
               WHEN '01'
                   MOVE 'Codigo ja cadastrado'
                       TO WS-MENSAGEM
               WHEN OTHER
                   MOVE 'Erro ao cadastrar cliente'
                       TO WS-MENSAGEM
           END-EVALUATE.

      *----------------------------------------------------------------*
      * ATUALIZAR-CLIENTE                                              *
      *----------------------------------------------------------------*
       ATUALIZAR-CLIENTE.
           MOVE SPACES TO WS-NOME
           MOVE SPACES TO WS-DB-RC
           CALL "DBUPDATE" USING
               WS-CODIGO
               WS-TELEFONE-REQ
               WS-EMAIL-REQ
               WS-NOME
               WS-DB-RC
           END-CALL
           MOVE WS-DB-RC TO WS-RETURN-CODE
           EVALUATE WS-DB-RC
               WHEN '00'
                   MOVE WS-TELEFONE-REQ TO WS-TEL-OUT
                   MOVE WS-EMAIL-REQ    TO WS-EMAIL-OUT
                   MOVE 'Atualizacao realizada com sucesso'
                       TO WS-MENSAGEM
               WHEN '01'
                   MOVE 'Cliente nao encontrado'
                       TO WS-MENSAGEM
               WHEN OTHER
                   MOVE 'Erro ao atualizar cliente'
                       TO WS-MENSAGEM
           END-EVALUATE.

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
