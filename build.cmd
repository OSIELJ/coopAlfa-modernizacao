@echo off
echo ================================================
echo  CoopAlfa - Build do nucleo COBOL
echo ================================================
echo.
echo Compilando CLICORE.cbl...
cobc -m -I "src\cobol\copybook" src\cobol\CLICORE.cbl -o src\cobol\build\CLICORE.dll
if %errorlevel% == 0 (
    echo.
    echo Compilacao concluida com sucesso!
    echo DLL gerada em: src\cobol\build\CLICORE.dll
) else (
    echo.
    echo ERRO na compilacao. Verifique os logs acima.
    exit /b 1
)