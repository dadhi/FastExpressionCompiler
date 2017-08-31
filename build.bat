@echo off

dotnet restore

dotnet build

pushd ".\test\FastExpressionCompiler.UnitTests"
dotnet test
popd

dotnet pack ".\src\FastExpressionCompiler" -c Release -o "./dist"