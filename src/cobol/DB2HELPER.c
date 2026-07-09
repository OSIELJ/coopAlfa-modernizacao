/*================================================================*
 * DB2HELPER.C - Wrapper C para acesso ao DB2 via ODBC            *
 * Cooperativa Financeira Alfa - Projeto de Modernizacao          *
 *                                                                *
 * Este modulo e chamado pelo CLICORE.cbl via CALL e faz          *
 * as chamadas ODBC para o DB2. Elimina a necessidade de          *
 * chamar funcoes ODBC diretamente do COBOL.                      *
 *================================================================*/

#include <windows.h>
#include <sql.h>
#include <sqlext.h>
#include <string.h>
#include <stdio.h>

/* Handles globais — mantidos entre chamadas COBOL */
static SQLHENV  hEnv  = SQL_NULL_HENV;
static SQLHDBC  hDbc  = SQL_NULL_HDBC;
static SQLHSTMT hStmt = SQL_NULL_HSTMT;

/* Limpa espacos a direita de uma string COBOL */
static void rtrim(char *s, int len) {
    int i = len - 1;
    while (i >= 0 && (s[i] == ' ' || s[i] == '\0')) {
        s[i] = '\0';
        i--;
    }
}

/* Mostra erro ODBC no console (diagnostico) */
static void diag(SQLSMALLINT tipo, SQLHANDLE h) {
    SQLCHAR estado[6], msg[300];
    SQLINTEGER nativo;
    SQLSMALLINT tam;
    SQLSMALLINT i = 1;
    while (SQLGetDiagRec(tipo, h, i, estado, &nativo, msg,
                         sizeof(msg), &tam) == SQL_SUCCESS) {
        printf("[ODBC %s] (%d) %s\n", estado, (int)nativo, msg);
        i++;
    }
}

/*----------------------------------------------------------------*
 * DBCONECT - Conecta no DB2 via ODBC                             *
 * Parametro: rc (2 bytes) - return code: 00=ok, 02=erro          *
 *----------------------------------------------------------------*/
void DBCONECT(char *rc) {
    SQLRETURN ret;

    strncpy(rc, "00", 2);

    ret = SQLAllocHandle(SQL_HANDLE_ENV, SQL_NULL_HANDLE, &hEnv);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        printf("Falha SQLAllocHandle ENV\n");
        strncpy(rc, "02", 2); return;
    }

    SQLSetEnvAttr(hEnv, SQL_ATTR_ODBC_VERSION,
                  (SQLPOINTER)SQL_OV_ODBC3, 0);

    ret = SQLAllocHandle(SQL_HANDLE_DBC, hEnv, &hDbc);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        printf("Falha SQLAllocHandle DBC\n");
        strncpy(rc, "02", 2); return;
    }

    /* Timeout de login: 10 segundos */
    SQLUINTEGER timeout = 10;
    SQLSetConnectAttr(hDbc, SQL_ATTR_LOGIN_TIMEOUT,
                      (SQLPOINTER)(uintptr_t)timeout, 0);

    /* Connection string completa */
    SQLCHAR connStr[] = 
        "DSN=BANCODSN;UID=db2inst1;PWD=Db2senha2026;";
    SQLCHAR outStr[512];
    SQLSMALLINT outLen;

    printf("Conectando: %s\n", connStr);

    ret = SQLDriverConnectA(hDbc, NULL,
        connStr, SQL_NTS,
        outStr, sizeof(outStr), &outLen,
        SQL_DRIVER_NOPROMPT);

    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        printf("Falha SQLDriverConnect (ret=%d):\n", (int)ret);
        diag(SQL_HANDLE_DBC, hDbc);
        strncpy(rc, "02", 2);
    } else {
        printf("Conectado com sucesso!\n");
    }
}

/*----------------------------------------------------------------*
 * DBDISCON - Desconecta do DB2                                   *
 *----------------------------------------------------------------*/
void DBDISCON(char *rc) {
    strncpy(rc, "00", 2);
    if (hDbc != SQL_NULL_HDBC) {
        SQLDisconnect(hDbc);
        SQLFreeHandle(SQL_HANDLE_DBC, hDbc);
        hDbc = SQL_NULL_HDBC;
    }
    if (hEnv != SQL_NULL_HENV) {
        SQLFreeHandle(SQL_HANDLE_ENV, hEnv);
        hEnv = SQL_NULL_HENV;
    }
}

/*----------------------------------------------------------------*
 * DBSELECT - Consulta cliente pelo codigo                        *
 * Parametros (todos COBOL PIC X):                                *
 *   codigo    (4)  - codigo do cliente                           *
 *   nome      (40) - nome retornado                              *
 *   telefone  (15) - telefone retornado                          *
 *   email     (50) - email retornado                             *
 *   rc        (2)  - 00=achou, 01=nao achou, 02=erro             *
 *----------------------------------------------------------------*/
void DBSELECT(char *codigo, char *nome, char *telefone,
              char *email, char *rc) {
    SQLRETURN ret;
    SQLHSTMT  hS;
    SQLLEN    ind;
    char      sql[300];
    char      cod[5];
    int       icodigo;
    char      tnome[41], ttel[16], temail[51];

    strncpy(rc, "00", 2);
    memset(nome,     ' ', 40);
    memset(telefone, ' ', 15);
    memset(email,    ' ', 50);

    /* Converte codigo COBOL (PIC 9(4)) para inteiro */
    memcpy(cod, codigo, 4); cod[4] = '\0';
    icodigo = atoi(cod);

    SQLAllocHandle(SQL_HANDLE_STMT, hDbc, &hS);

    snprintf(sql, sizeof(sql),
        "SELECT CLI_NOME, CLI_TELEFONE, CLI_EMAIL "
        "FROM DB2INST1.CLIENTES_COOPALF "
        "WHERE CLI_CODIGO = %d", icodigo);

    ret = SQLExecDirectA(hS, (SQLCHAR*)sql, SQL_NTS);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        strncpy(rc, "02", 2);
        SQLFreeHandle(SQL_HANDLE_STMT, hS);
        return;
    }

    ret = SQLFetch(hS);
    if (ret == SQL_SUCCESS || ret == SQL_SUCCESS_WITH_INFO) {
        memset(tnome,   0, sizeof(tnome));
        memset(ttel,    0, sizeof(ttel));
        memset(temail,  0, sizeof(temail));

        SQLGetData(hS, 1, SQL_C_CHAR, tnome,   41, &ind);
        SQLGetData(hS, 2, SQL_C_CHAR, ttel,    16, &ind);
        SQLGetData(hS, 3, SQL_C_CHAR, temail,  51, &ind);

        /* Copia para campos COBOL preenchendo com espacos */
        memset(nome,     ' ', 40);
        memset(telefone, ' ', 15);
        memset(email,    ' ', 50);
        memcpy(nome,     tnome,  strnlen(tnome,  40));
        memcpy(telefone, ttel,   strnlen(ttel,   15));
        memcpy(email,    temail, strnlen(temail, 50));
        strncpy(rc, "00", 2);
    } else {
        strncpy(rc, "01", 2);
    }

    SQLFreeHandle(SQL_HANDLE_STMT, hS);
}

/*----------------------------------------------------------------*
 * DBINSERT - Cadastra novo cliente                               *
 *----------------------------------------------------------------*/
void DBINSERT(char *codigo, char *nome, char *telefone,
              char *email, char *rc) {
    SQLRETURN ret;
    SQLHSTMT  hS;
    char      sql[400];
    char      cod[5];
    char      tnome[41], ttel[16], temail[51];
    int       icodigo;

    strncpy(rc, "00", 2);

    memcpy(cod, codigo, 4); cod[4] = '\0';
    icodigo = atoi(cod);

    memcpy(tnome,  nome,     40); tnome[40]  = '\0'; rtrim(tnome,  40);
    memcpy(ttel,   telefone, 15); ttel[15]   = '\0'; rtrim(ttel,   15);
    memcpy(temail, email,    50); temail[50] = '\0'; rtrim(temail, 50);

    SQLAllocHandle(SQL_HANDLE_STMT, hDbc, &hS);

    snprintf(sql, sizeof(sql),
        "INSERT INTO DB2INST1.CLIENTES_COOPALF "
        "(CLI_CODIGO, CLI_NOME, CLI_TELEFONE, CLI_EMAIL) "
        "VALUES (%d, '%s', '%s', '%s')",
        icodigo, tnome, ttel, temail);

    ret = SQLExecDirectA(hS, (SQLCHAR*)sql, SQL_NTS);
    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        strncpy(rc, "01", 2);
    } else {
        SQLEndTran(SQL_HANDLE_DBC, hDbc, SQL_COMMIT);
    }

    SQLFreeHandle(SQL_HANDLE_STMT, hS);
}

/*----------------------------------------------------------------*
 * DBUPDATE - Atualiza telefone e email do cliente                *
 *----------------------------------------------------------------*/
void DBUPDATE(char *codigo, char *telefone, char *email,
              char *nome_out, char *rc) {
    SQLRETURN ret;
    SQLHSTMT  hS;
    char      sql[400];
    char      cod[5];
    char      ttel[16], temail[51];
    int       icodigo;
    SQLLEN    rows;

    strncpy(rc, "00", 2);
    memset(nome_out, ' ', 40);

    memcpy(cod, codigo, 4); cod[4] = '\0';
    icodigo = atoi(cod);

    memcpy(ttel,   telefone, 15); ttel[15]   = '\0'; rtrim(ttel,   15);
    memcpy(temail, email,    50); temail[50] = '\0'; rtrim(temail, 50);

    SQLAllocHandle(SQL_HANDLE_STMT, hDbc, &hS);

    snprintf(sql, sizeof(sql),
        "UPDATE DB2INST1.CLIENTES_COOPALF "
        "SET CLI_TELEFONE='%s', CLI_EMAIL='%s' "
        "WHERE CLI_CODIGO=%d",
        ttel, temail, icodigo);

    ret = SQLExecDirectA(hS, (SQLCHAR*)sql, SQL_NTS);
    SQLRowCount(hS, &rows);
    if (ret == SQL_SUCCESS || ret == SQL_SUCCESS_WITH_INFO) {
        SQLEndTran(SQL_HANDLE_DBC, hDbc, SQL_COMMIT);
    }
    SQLFreeHandle(SQL_HANDLE_STMT, hS);

    if (ret != SQL_SUCCESS && ret != SQL_SUCCESS_WITH_INFO) {
        strncpy(rc, "02", 2); return;
    }
    if (rows == 0) {
        strncpy(rc, "01", 2); return;
    }

    /* Busca o nome para retornar */
    char tnome[41]; char ttel2[16]; char temail2[51];
    SQLLEN ind;
    SQLAllocHandle(SQL_HANDLE_STMT, hDbc, &hS);
    snprintf(sql, sizeof(sql),
        "SELECT CLI_NOME, CLI_TELEFONE, CLI_EMAIL "
        "FROM DB2INST1.CLIENTES_COOPALF "
        "WHERE CLI_CODIGO=%d", icodigo);
    SQLExecDirectA(hS, (SQLCHAR*)sql, SQL_NTS);
    if (SQLFetch(hS) == SQL_SUCCESS) {
        memset(tnome,   0, sizeof(tnome));
        memset(ttel2,   0, sizeof(ttel2));
        memset(temail2, 0, sizeof(temail2));
        SQLGetData(hS, 1, SQL_C_CHAR, tnome,   41, &ind);
        SQLGetData(hS, 2, SQL_C_CHAR, ttel2,   16, &ind);
        SQLGetData(hS, 3, SQL_C_CHAR, temail2, 51, &ind);
        memset(nome_out, ' ', 40);
        memcpy(nome_out, tnome, strnlen(tnome, 40));
    }
    SQLFreeHandle(SQL_HANDLE_STMT, hS);
}
