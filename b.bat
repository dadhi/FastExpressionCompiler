@echo off

echo: 
echo:## Starting: TESTS...
echo:

dotnet run -v:m -c:Release -p:GeneratePackageOnBuild=false --project test/FastExpressionCompiler.TestsRunner
if %ERRORLEVEL% neq 0 goto :error

dotnet run -v:m -c:Release -p:GeneratePackageOnBuild=false --project test/FastExpressionCompiler.TestsRunner.Net472
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
