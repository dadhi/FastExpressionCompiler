@echo off
setlocal EnableDelayedExpansion

echo: # BUILDING AND RUNNING THE TESTS IN DEBUG MODE
echo:
echo:
echo:## Starting: RESTORE and BUILD...
echo: 

dotnet build -c:Debug
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:## Finished: RESTORE and BUILD

echo: 
echo:## Starting: TESTS...
echo:
echo:running on .NET 9.0 (Latest)
dotnet run --no-build -f:net9.0 -c:Debug --project test/FastExpressionCompiler.TestsRunner
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:running on .NET 8.0 (LTS)
dotnet run --no-build -f:net8.0 -c:Debug --project test/FastExpressionCompiler.TestsRunner
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:running on .NET 6.0 (Previous LTS)
dotnet run --no-build -f:net6.0 -c:Debug --project test/FastExpressionCompiler.TestsRunner
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:running on .NET 4.7.2
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
