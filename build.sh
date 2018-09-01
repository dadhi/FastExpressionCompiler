#!/usr/bin/env bash

set -euo pipefail

dotnet restore

# if test "$TRAVIS_OS_NAME" != "osx";
# then 
# todo: Until .Net Core 2 / 2.1 support
# dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.IssueTests.csproj -c Release -f netcoreapp1.1;
# dotnet test test/FastExpressionCompiler.UnitTests/FastExpressionCompiler.UnitTests.csproj -c Release -f netcoreapp1.1;
# fi

dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.IssueTests.csproj -c Release -f netcoreapp2.0;
dotnet test test/FastExpressionCompiler.UnitTests/FastExpressionCompiler.UnitTests.csproj -c Release -f netcoreapp2.0;

dotnet test test/FastExpressionCompiler.LightExpression.IssueTests/FastExpressionCompiler.LightExpression.IssueTests.csproj -c Release -f netcoreapp2.0;
dotnet test test/FastExpressionCompiler.LightExpression.UnitTests/FastExpressionCompiler.LightExpression.UnitTests.csproj -c Release -f netcoreapp2.0;
