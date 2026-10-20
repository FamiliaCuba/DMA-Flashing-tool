@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set ROOT=%~dp0..
copy /Y "%ROOT%\icon-src.jpg" "%ROOT%\icon-src.jpg" >nul
"%CSC%" /nologo /optimize /t:exe /r:System.Drawing.dll /out:"%~dp0MakeIco.exe" "%~dp0MakeIco.cs"
if errorlevel 1 exit /b 1
"%~dp0MakeIco.exe" "%ROOT%\icon-src.jpg" "%ROOT%\familiacuba.ico"
if errorlevel 1 exit /b 1
dir "%ROOT%\familiacuba.ico"
endlocal
