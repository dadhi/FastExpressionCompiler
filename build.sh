#!/usr/bin/env bash
dotnet restore && dotnet build -f netcoreapp2.0 -c Release && dotnet test ./test/FastExpressionCompiler.UnitTests && dotnet test ./test/FastExpressionCompiler.IssueTests

#dotnet pack ".\src\FastExpressionCompiler" -c Release -o "./dist"