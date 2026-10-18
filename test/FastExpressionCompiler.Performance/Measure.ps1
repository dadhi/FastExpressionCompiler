param(
    [Parameter(Mandatory=$true)][string]$Label,
    [Parameter(Mandatory=$true)][string]$OutputRoot,
    [string]$Filter = '*'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$run = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) $Label
if (Test-Path -LiteralPath $run) { throw "Run already exists: $run" }
New-Item -ItemType Directory -Path "$run/binaries" -Force | Out-Null
Push-Location $repo
try {
    dotnet build "$PSScriptRoot/FastExpressionCompiler.Performance.csproj" -c Release --disable-build-servers -m:1
    if ($LASTEXITCODE) { throw 'Build failed' }
    Copy-Item "$PSScriptRoot/bin/Release/net10.0/*" "$run/binaries" -Recurse
    $metadata = [ordered]@{
        label = $Label
        sourceCommit = (git rev-parse HEAD)
        sourceStatus = @(git status --porcelain)
        utc = [DateTime]::UtcNow.ToString('o')
        dotnetInfo = @(dotnet --info)
        processor = @(Get-CimInstance Win32_Processor | Select-Object Name, NumberOfCores, NumberOfLogicalProcessors)
        sources = @(Get-FileHash "$repo/src/FastExpressionCompiler/FastExpressionCompiler.cs", "$repo/src/FastExpressionCompiler.LightExpression/Expression.cs", "$PSScriptRoot/Program.cs")
        assemblies = @(Get-FileHash "$run/binaries/FastExpressionCompiler.dll", "$run/binaries/FastExpressionCompiler.LightExpression.dll", "$run/binaries/FastExpressionCompiler.Performance.dll")
    }
    $metadata | ConvertTo-Json -Depth 8 | Set-Content "$run/metadata.json" -Encoding utf8
    $oldOutput = $env:FEC_PERF_OUTPUT
    try {
        $env:FEC_PERF_OUTPUT = "$run/results"
        dotnet "$run/binaries/FastExpressionCompiler.Performance.dll" --filter $Filter *> "$run/run.log"
        if ($LASTEXITCODE) { throw "Benchmark host failed; see $run/run.log" }
    } finally { $env:FEC_PERF_OUTPUT = $oldOutput }
} finally { Pop-Location }
