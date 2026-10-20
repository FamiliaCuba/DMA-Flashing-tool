@echo off
setlocal
rem ===== Compila Familia Cuba (WPF por codigo, sin SDK) con csc de .NET Framework =====

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo No se encontro csc.exe de .NET Framework 4.x
  exit /b 1
)

set WPF=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF
if not exist "%WPF%\PresentationFramework.dll" set WPF=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\WPF
if not exist "%WPF%\PresentationFramework.dll" (
  echo No se encontraron los ensamblados de WPF
  exit /b 1
)

set ICON=
if exist "%~dp0familiacuba.ico" set ICON=/win32icon:"%~dp0familiacuba.ico"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0pack.ps1"
if errorlevel 1 (
  echo FALLO al empaquetar runtime\ en payload.bin
  exit /b 1
)
if not exist "%~dp0payload.bin" (
  echo No se genero payload.bin
  exit /b 1
)

"%CSC%" /nologo /optimize /target:winexe /lib:"%WPF%" ^
  /r:PresentationFramework.dll /r:PresentationCore.dll /r:WindowsBase.dll /r:System.Xaml.dll ^
  /r:System.dll /r:System.Core.dll /r:System.Xml.dll ^
  /win32manifest:"%~dp0app.manifest" %ICON% ^
  /resource:"%~dp0payload.bin",FC.payload ^
  /out:"%~dp0FamiliaCuba.exe" "%~dp0src\*.cs"
if errorlevel 1 (
  echo.
  echo FALLO la compilacion.
  exit /b 1
)

echo.
echo OK: %~dp0FamiliaCuba.exe
endlocal
