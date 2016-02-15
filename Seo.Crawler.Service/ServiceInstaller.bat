@ECHO OFF

net session >nul 2>&1
IF NOT %ERRORLEVEL% EQU 0 (
   ECHO ERROR: Please run Bat as Administrator.
   PAUSE
   EXIT /B 1
)

@SETLOCAL enableextensions
@CD /d "%~dp0"

REM The following directory is for .NET 4
SET DOTNETFX4=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
SET PATH=%PATH%;%DOTNETFX4%

ECHO Installing Seo.Crawler.Service...
ECHO ---------------------------------------------------
REM /i for install /u for uninstall
InstallUtil /i .\Seo.Crawler.Service.exe
ECHO ---------------------------------------------------
ECHO Done.
PAUSE