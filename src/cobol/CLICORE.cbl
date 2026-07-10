      *================================================================*
      * CLICORE.CBL - Nucleo legado de cadastro de clientes            *
      * Cooperativa Financeira Alfa - Projeto de Modernizacao          *
      *                                                                *
      * Arquitetura: processo separado + DB2 via wrapper C (ODBC)      *
      * Le a requisicao de REQUEST.DAT, acessa o DB2 atraves das       *
      * funcoes do DB2HELPER, e grava a resposta em RESPONSE.DAT.      *
      *                                                                *
      * O layout dos dois arquivos e definido em CLIENTE.cpy, que e a  *
      * definicao unica do contrato compartilhado com a camada .NET.   *
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

      *----------------------------------------------------------------*
      * Contrato de dados compartilhado com a camada .NET              *
      * Define WS-REQUEST (110 bytes) e WS-RESPONSE (187 bytes)        *
      *----------------------------------------------------------------*
       COPY "CLIENTE.cpy".

      *----------------------------------------------------------------*
      * Variaveis internas (nao fazem parte do contrato)               *
      *----------------------------------------------------------------*
       01 WS-REQ-STATUS            PIC X(02).
       01 WS-RESP-STATUS           PIC X(02).
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
                       SET RC-ERRO TO TRUE
                       MOVE 'Operacao invalida'
                           TO WS-MENSAGEM
               END-EVALUATE
               PERFORM DESCONECTAR-DB2
           ELSE
               SET RC-ERRO TO TRUE
               MOVE 'Erro ao conectar no DB2'
                   TO WS-MENSAGEM
           END-IF
           PERFORM GRAVAR-RESPONSE
           STOP RUN.

      *----------------------------------------------------------------*
      * LER-REQUEST                                                    *
      * Desmonta o registro de entrada nos campos do contrato          *
      *----------------------------------------------------------------*
       LER-REQUEST.
           INITIALIZE WS-REQUEST
           INITIALIZE WS-RESPONSE
           OPEN INPUT ARQUIVO-REQUEST
           READ ARQUIVO-REQUEST
               AT END
                   SET RC-ERRO TO TRUE
                   MOVE 'Erro ao ler requisicao'
                       TO WS-MENSAGEM
           END-READ
           MOVE REGISTRO-REQUEST(1:110) TO WS-REQUEST
           CLOSE ARQUIVO-REQUEST.

      *----------------------------------------------------------------*
      * CONECTAR-DB2 - chama o wrapper C                               *
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
           EVALUATE TRUE
               WHEN RC-SUCESSO
                   MOVE 'Consulta realizada com sucesso'
                       TO WS-MENSAGEM
               WHEN RC-NAO-ENCONTRADO
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
           EVALUATE TRUE
               WHEN RC-SUCESSO
                   MOVE WS-NOME-REQ     TO WS-NOME
                   MOVE WS-TELEFONE-REQ TO WS-TEL-OUT
                   MOVE WS-EMAIL-REQ    TO WS-EMAIL-OUT
                   MOVE 'Cliente cadastrado com sucesso'
                       TO WS-MENSAGEM
               WHEN RC-NAO-ENCONTRADO
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
           EVALUATE TRUE
               WHEN RC-SUCESSO
                   MOVE WS-TELEFONE-REQ TO WS-TEL-OUT
                   MOVE WS-EMAIL-REQ    TO WS-EMAIL-OUT
                   MOVE 'Atualizacao realizada com sucesso'
                       TO WS-MENSAGEM
               WHEN RC-NAO-ENCONTRADO
                   MOVE 'Cliente nao encontrado'
                       TO WS-MENSAGEM
               WHEN OTHER
                   MOVE 'Erro ao atualizar cliente'
                       TO WS-MENSAGEM
           END-EVALUATE.

      *----------------------------------------------------------------*
      * GRAVAR-RESPONSE                                                *
      * Serializa o contrato de saida no registro de 187 bytes         *
      *----------------------------------------------------------------*
       GRAVAR-RESPONSE.
           MOVE SPACES      TO REGISTRO-RESPONSE
           MOVE WS-RESPONSE TO REGISTRO-RESPONSE(1:187)
           OPEN OUTPUT ARQUIVO-RESPONSE
           WRITE REGISTRO-RESPONSE
           CLOSE ARQUIVO-RESPONSE.
