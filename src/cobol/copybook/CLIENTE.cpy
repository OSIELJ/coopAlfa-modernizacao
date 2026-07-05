      *================================================================*
      * CLIENTE.CPY - Estrutura compartilhada de dados do cliente      *
      * Cooperativa Financeira Alfa - Projeto de Modernizacao          *
      *================================================================*
       01 WS-CLIENTE.
           05 WS-OPERACAO          PIC X(01).
              88 OP-CONSULTAR      VALUE 'C'.
              88 OP-ATUALIZAR      VALUE 'A'.
           05 WS-CODIGO            PIC 9(04).
           05 WS-NOME              PIC X(40).
           05 WS-TELEFONE          PIC X(15).
           05 WS-EMAIL             PIC X(50).
           05 WS-RETURN-CODE       PIC X(02).
              88 RC-SUCESSO        VALUE '00'.
              88 RC-NAO-ENCONTRADO VALUE '01'.
              88 RC-ERRO           VALUE '02'.
           05 WS-MENSAGEM          PIC X(80).