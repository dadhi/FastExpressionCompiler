@echo off

dotnet restore

dotnet build

dotnet test ".\test\FastExpressionCompiler.UnitTests"

dotnet pack ".\src\FastExpressionCompiler" -c Release -o ".\dist"

pause