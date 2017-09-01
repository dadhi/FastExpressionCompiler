@echo off

dotnet restore

dotnet build

dotnet test ".\test\FastExpressionCompiler.UnitTests"
dotnet test ".\test\FastExpressionCompiler.IssueTests"

dotnet pack ".\src\FastExpressionCompiler" -c Release -o "./dist"

pause