       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLISEED.

       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT ARQUIVO-CLIENTES
               ASSIGN TO "CLIENTES.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS ARQ-CODIGO
               FILE STATUS IS WS-STATUS.

       DATA DIVISION.
       FILE SECTION.
       FD ARQUIVO-CLIENTES.
       01 REGISTRO-CLIENTE.
           05 ARQ-CODIGO           PIC 9(04).
           05 ARQ-NOME             PIC X(40).
           05 ARQ-TELEFONE         PIC X(15).
           05 ARQ-EMAIL            PIC X(50).

       WORKING-STORAGE SECTION.
       01 WS-STATUS                PIC X(02).

       PROCEDURE DIVISION.
       INICIO.
           OPEN OUTPUT ARQUIVO-CLIENTES

           MOVE 1001              TO ARQ-CODIGO
           MOVE 'Maria Silva'     TO ARQ-NOME
           MOVE '(11) 99999-1234' TO ARQ-TELEFONE
           MOVE 'maria@email.com' TO ARQ-EMAIL
           WRITE REGISTRO-CLIENTE

           MOVE 1002              TO ARQ-CODIGO
           MOVE 'Joao Santos'     TO ARQ-NOME
           MOVE '(21) 98888-5678' TO ARQ-TELEFONE
           MOVE 'joao@email.com'  TO ARQ-EMAIL
           WRITE REGISTRO-CLIENTE

           MOVE 1003              TO ARQ-CODIGO
           MOVE 'Ana Oliveira'    TO ARQ-NOME
           MOVE '(31) 97777-9012' TO ARQ-TELEFONE
           MOVE 'ana@email.com'   TO ARQ-EMAIL
           WRITE REGISTRO-CLIENTE

           CLOSE ARQUIVO-CLIENTES
           DISPLAY 'Dados criados com sucesso!'
           STOP RUN.