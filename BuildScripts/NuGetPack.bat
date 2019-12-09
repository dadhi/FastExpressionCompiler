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
echo:DryIoc source and internal packages
echo:===================================
%NUGET% pack %NUSPECS%\DryIoc.nuspec -OutputDirectory %PACKAGEDIR% -NonInteractive
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '.\BuildScripts\MakeInternal.ps1'";
%NUGET% pack %NUSPECS%\DryIoc.Internal.nuspec -OutputDirectory %PACKAGEDIR% -NonInteractive


REM if not "%1"=="-nopause" pause 
REM goto:eof

REM set VERFILE=%~1
REM for /f "usebackq tokens=2,3 delims=:() " %%A in ("%VERFILE%") do (
REM 	if "%%A"=="AssemblyInformationalVersion" set VER=%%~B
REM exit /b