@echo off
setlocal EnableDelayedExpansion

echo: # BUILDING AND RUNNNG THE TESTS IN DEBUG MODE
echo:
echo:

set "FrameworkParam=-f:net9.0"
set "LatestSupportedNetProp=-p:LatestSupportedNet=net9.0"
if [%1] NEQ [] (
    set "FrameworkParam=-f:%1"
    set "LatestSupportedNetProp=-p:LatestSupportedNet=%1"
)
echo:FrameworkParam == '%FrameworkParam%', LatestSupportedNetProp == '%LatestSupportedNetProp%'

echo:
echo:## Starting: RESTORE and BUILD...
echo: 

dotnet build %LatestSupportedNetProp% -c:Debug
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:## Finished: RESTORE and BUILD

echo: 
echo:## Starting: TESTS...
echo:

dotnet run --no-build %LatestSupportedNetProp% %FrameworkParam% -c:Debug --project test/FastExpressionCompiler.TestsRunner
if %ERRORLEVEL% neq 0 goto :error

dotnet run --no-build -c:Debug --project test/FastExpressionCompiler.TestsRunner.Net472
if %ERRORLEVEL% neq 0 goto :error
echo:
echo:## Finished: TESTS

echo: 
echo:## Starting: SOURCE PACKAGING...
echo:
call BuildScripts\NugetPack.bat
if %ERRORLEVEL% neq 0 goto :error
echo:
echo:## Finished: SOURCE PACKAGING
echo: 
echo:# Finished: ALL
echo:
exit /b 0

:error
echo:
echo:## :-( Failed with ERROR: %ERRORLEVEL%
echo:
exit /b %ERRORLEVEL%
