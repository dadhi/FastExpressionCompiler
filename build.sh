#!/usr/bin/env bash
dotnet restore && dotnet build && dotnet test

#dotnet pack ".\src\FastExpressionCompiler" -c Release -o "./dist"