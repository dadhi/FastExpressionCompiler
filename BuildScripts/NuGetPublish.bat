@echo off

set PACKAGES=..\.dist
set SOURCE=https://api.nuget.org/v3/index.json
set /p APIKEY=<"..\ApiKey.txt"

dotnet nuget push "%PACKAGES%\DryIoc.dll.4.1.0-preview-03.nupkg" -k %APIKEY% -s %SOURCE%
dotnet nuget push "%PACKAGES%\DryIoc.4.1.0-preview-03.nupkg" -k %APIKEY% -s %SOURCE%
dotnet nuget push "%PACKAGES%\DryIoc.Internal.4.1.0-preview-03.nupkg" -k %APIKEY% -s %SOURCE%

echo:
echo:Publishing completed.

pause
