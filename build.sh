#!/usr/bin/env bash

set -euo pipefail

dotnet restore -p:SourceLink=false;DevMode=false

dotnet test test/FastExpressionCompiler.IssueTests/FastExpressionCompiler.IssueTests.csproj -c:Release -f:netcoreapp2.0 -p:Sign=false;SourceLink=false;DevMode=false
dotnet test test/FastExpressionCompiler.UnitTests/FastExpressionCompiler.UnitTests.csproj   -c:Release -f:netcoreapp2.0 -p:Sign=false;SourceLink=false;DevMode=false

dotnet test test/FastExpressionCompiler.LightExpression.IssueTests/FastExpressionCompiler.LightExpression.IssueTests.csproj -c:Release -f:netcoreapp2.0 -p:Sign=false;SourceLink=false;DevMode=false
dotnet test test/FastExpressionCompiler.LightExpression.UnitTests/FastExpressionCompiler.LightExpression.UnitTests.csproj   -c:Release -f:netcoreapp2.0 -p:Sign=false;SourceLink=false;DevMode=false
