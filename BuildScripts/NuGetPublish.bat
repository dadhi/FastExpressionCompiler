@echo off

set PACKAGES=..\.dist
set SOURCE=https://api.nuget.org/v3/index.json
set /p APIKEY=<"..\..\ApiKey.txt"

set PKGVER=5.1.0

dotnet nuget push "%PACKAGES%\FastExpressionCompiler.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE% --skip-duplicate
dotnet nuget push "%PACKAGES%\FastExpressionCompiler.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE% --skip-duplicate
dotnet nuget push "%PACKAGES%\FastExpressionCompiler.Internal.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE% --skip-duplicate
dotnet nuget push "%PACKAGES%\FastExpressionCompiler.LightExpression.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE% --skip-duplicate
dotnet nuget push "%PACKAGES%\FastExpressionCompiler.LightExpression.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE% --skip-duplicate
dotnet nuget push "%PACKAGES%\FastExpressionCompiler.LightExpression.Internal.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE% --skip-duplicate

echo:
echo:Publishing completed.

pause
