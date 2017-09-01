@echo off

dotnet restore

dotnet build

dotnet test -f netcoreapp1.1 ".\test\FastExpressionCompiler.UnitTests"
dotnet test -f netcoreapp1.1 ".\test\FastExpressionCompiler.IssueTests"

dotnet test -f netcoreapp2.0 ".\test\FastExpressionCompiler.UnitTests"
dotnet test -f netcoreapp2.0 ".\test\FastExpressionCompiler.IssueTests"

dotnet pack ".\src\FastExpressionCompiler" -c Release -o "./dist"

pause