#!/usr/bin/env bash

set -euo pipefail

dotnet restore

if test "$TRAVIS_OS_NAME" != "osx";
then 
dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.IssueTests.csproj -c Release -f netcoreapp1.1;
dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.UnitTests.csproj -c Release -f netcoreapp1.1;
fi

dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.IssueTests.csproj -c Release -f netcoreapp2.0;
dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.UnitTests.csproj -c Release -f netcoreapp2.0;