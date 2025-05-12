$inputFiles = @(
    ".\src\FastExpressionCompiler\FastExpressionCompiler.cs",
    ".\src\FastExpressionCompiler\ImTools.cs",
    ".\src\FastExpressionCompiler\ILReader.cs",
    ".\src\FastExpressionCompiler\TestTools.cs",
    ".\src\FastExpressionCompiler.LightExpression\Expression.cs",
    ".\src\FastExpressionCompiler.LightExpression\ExpressionVisitor.cs"
)
$outputFolder = ".\src\FastExpressionCompiler.Internal"

New-Item -ItemType Directory -Force -Path $outputFolder | Out-Null
ForEach ($file in $inputFiles)
{
    $content = Get-Content -path $file
    $content = $content -creplace "public(?=\s+(((abstract|sealed|static)\s+)?(partial\s+)?class|delegate|enum|interface|struct))", "internal"
    $outputPath = Join-Path $outputFolder (Split-Path $file -Leaf)
    Out-File $outputPath UTF8 -InputObject $content
}
