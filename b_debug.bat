@echo off

echo:
echo:## Starting: RESTORE and BUILD...
echo: 

dotnet build -c:Debug -v:m
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:## Finished: RESTORE and BUILD

echo: 
echo:## Starting: TESTS...
echo:### NET8.0:
echo: 

dotnet run --no-build -f net8.0 -c Debug --project test/FastExpressionCompiler.TestsRunner
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:### NET4.7.2:
echo:

dotnet run --no-build -c Debug --project test/FastExpressionCompiler.TestsRunner.Net472
if %ERRORLEVEL% neq 0 goto :error

echo:
echo:## Finished: TESTS

echo:# Finished: ALL
echo:
exit /b 0

:error
echo:
echo:## :-( Failed with ERROR: %ERRORLEVEL%
echo:
exit /b %ERRORLEVEL%
