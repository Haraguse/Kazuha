@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo       Kazuha Project Packager
echo ==========================================

rem Project root (this script's directory)
set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"

rem Temporary staging directory for packaging
set "PACK_DIR=%ROOT%\_package_tmp"

echo [1/4] Cleaning previous temp directory...
if exist "%PACK_DIR%" (
    rmdir /s /q "%PACK_DIR%"
)
mkdir "%PACK_DIR%"

echo [2/4] Copying project files (excluding build artifacts)...
rem /E  - copy subdirectories, including empty ones
rem /XD - exclude directories
rem /XF - exclude files
robocopy "%ROOT%" "%PACK_DIR%" /E ^
    /XD ".git" "build" "dist" "venv" "__pycache__" "translations" ^
    /XF ".gitignore" "*.pyc" "*.pyo" "Kazuha.spec" "*.spec" "_package_tmp" >nul

echo [2.5/4] Copying translations...
if exist "%ROOT%\translations" (
    mkdir "%PACK_DIR%\translations"
    copy "%ROOT%\translations\*.qm" "%PACK_DIR%\translations\" >nul
)

if %errorlevel% GEQ 8 (
    echo [ERROR] File copy failed with errorlevel %errorlevel%.
    goto :end
)

echo [3/4] Creating archive...
set "OUT_NAME=Kazuha_Package.zip"
if exist "%ROOT%\%OUT_NAME%" del /f /q "%ROOT%\%OUT_NAME%"

powershell -NoLogo -NoProfile -Command ^
    "Compress-Archive -Path '%PACK_DIR%\*' -DestinationPath '%ROOT%\%OUT_NAME%' -Force" 1>nul

if errorlevel 1 (
    echo [ERROR] Compress-Archive failed.
    goto :end
)

echo [4/4] Cleaning up temp directory...
if exist "%PACK_DIR%" (
    rmdir /s /q "%PACK_DIR%"
)

echo.
echo ==========================================
echo       Package Created Successfully
echo ==========================================
echo Output file: %OUT_NAME%
echo Location   : %ROOT%
echo.
goto :eof

:end
echo.
echo Packaging aborted due to errors.
echo.
exit /b 1
