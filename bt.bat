@echo off

echo: 
echo:## Running TESTS on the Latest .NET version...
echo:
dotnet run -v:m -f:net9.0 -c:Release -p:GeneratePackageOnBuild=false --project test/FastExpressionCompiler.TestsRunner/FastExpressionCompiler.TestsRunner.csproj
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
