@echo off
chcp 65001 >nul
echo ============================================================
echo   Сборка NeuroSpec.exe (портативная версия для Windows)
echo ============================================================
echo.
echo Для сборки требуется интернет-соединение (один раз, чтобы
echo скачать служебный пакет System.Management).
echo.

dotnet publish "%~dp0NeuroSpec.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "%~dp0dist"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ============================================================
    echo   ОШИБКА СБОРКИ
    echo   Убедитесь, что установлен .NET 8 SDK:
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    echo ============================================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ============================================================
echo   ГОТОВО! Файл NeuroSpec.exe находится в папке dist
echo ============================================================
pause
