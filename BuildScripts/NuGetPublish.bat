@echo off

set PACKAGES=..\.dist
set SOURCE=https://api.nuget.org/v3/index.json
set /p APIKEY=<"..\..\ApiKey.txt"

set PKGVER=3.1.0-preview-01

dotnet nuget push "%PACKAGES%\FastExpressionCompiler.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE%
rem dotnet nuget push "%PACKAGES%\FastExpressionCompiler.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE%
rem dotnet nuget push "%PACKAGES%\FastExpressionCompiler.Internal.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE%
rem dotnet nuget push "%PACKAGES%\FastExpressionCompiler.LightExpression.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE%
rem dotnet nuget push "%PACKAGES%\FastExpressionCompiler.LightExpression.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE%
rem dotnet nuget push "%PACKAGES%\FastExpressionCompiler.LightExpression.Internal.src.%PKGVER%.nupkg" -k %APIKEY% -s %SOURCE%

echo:
echo:Publishing completed.

pause
