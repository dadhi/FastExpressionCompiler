@echo off
setlocal EnableDelayedExpansion

set NUGET=.nuget\NuGet.exe
set NUSPECS=nuspecs
set PACKAGEDIR=.dist

echo:
echo:Packing NuGet packages into %PACKAGEDIR% . . .
echo:
if not exist %PACKAGEDIR% md %PACKAGEDIR% 

echo:
echo:Source and internal packages
echo:===================================
%NUGET% pack %NUSPECS%\FastExpressionCompiler.src.nuspec -OutputDirectory %PACKAGEDIR% -NonInteractive
%NUGET% pack %NUSPECS%\FastExpressionCompiler.LightExpression.src.nuspec -OutputDirectory %PACKAGEDIR% -NonInteractive

PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '.\BuildScripts\MakeInternal.ps1'";
%NUGET% pack %NUSPECS%\FastExpressionCompiler.Internal.src.nuspec -OutputDirectory %PACKAGEDIR% -NonInteractive
%NUGET% pack %NUSPECS%\FastExpressionCompiler.LightExpression.Internal.src.nuspec -OutputDirectory %PACKAGEDIR% -NonInteractive


REM if not "%1"=="-nopause" pause 
REM goto:eof

REM set VERFILE=%~1
REM for /f "usebackq tokens=2,3 delims=:() " %%A in ("%VERFILE%") do (
REM 	if "%%A"=="AssemblyInformationalVersion" set VER=%%~B
REM exit /b