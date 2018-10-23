@echo off

dotnet build -c:Release -v:m -p:DevMode=false

dotnet test  ".\test\FastExpressionCompiler.UnitTests" -c Release -p:DevMode=false
dotnet test  ".\test\FastExpressionCompiler.IssueTests" -c Release -p:DevMode=false

dotnet test  ".\test\FastExpressionCompiler.LightExpression.UnitTests" -c Release -p:DevMode=false
dotnet test  ".\test\FastExpressionCompiler.LightExpression.IssueTests" -c Release -p:DevMode=false

dotnet pack ".\src\FastExpressionCompiler" -c Release -p:DevMode=false
dotnet pack ".\src\FastExpressionCompiler.LightExpression" -c Release -p:DevMode=false
