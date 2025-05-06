@echo off

echo:
echo:## Starting BUILD...
echo: 
dotnet build -p:DevMode=true -v:m -c:Release
if %ERRORLEVEL% neq 0 goto :error

echo: 
echo:## Starting TESTS...
echo:

dotnet run --no-build -p:DevMode=true -f:net9.0 -c:Release --project test/FastExpressionCompiler.TestsRunner/FastExpressionCompiler.TestsRunner.csproj
if %ERRORLEVEL% neq 0 goto :error

dotnet run --no-build -p:DevMode=true -c:Release --project test/FastExpressionCompiler.TestsRunner.Net472
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
