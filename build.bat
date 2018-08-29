@echo off

dotnet restore

dotnet build

dotnet test  ".\test\FastExpressionCompiler.UnitTests" -c Release
dotnet test  ".\test\FastExpressionCompiler.IssueTests" -c Release

dotnet pack ".\src\FastExpressionCompiler" -c Release -o "./dist"
dotnet pack ".\src\FastExpressionCompiler.LightExpression" -c Release -o "./dist"
