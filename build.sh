#!/usr/bin/env bash

set -euo pipefail

dotnet restore

dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.IssueTests.csproj -c Release -f netcoreapp2.0;
dotnet test test/FastExpressionCompiler.UnitTests/FastExpressionCompiler.UnitTests.csproj -c Release -f netcoreapp2.0;
