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
    $content = $content -creplace "public(?=\s+(((abstract|sealed|static|record)\s+)?(partial\s+)?class|delegate|enum|interface|struct|record))", "internal"
    $content = $content -replace "^\s*#pragma warning.*$", "" # remove any #pragma directive that could be reactivating our global disable
    $content = ,"#pragma warning disable" + $content # $content is a list of lines, insert at the top
    $outputPath = Join-Path $outputFolder (Split-Path $file -Leaf)
    Out-File $outputPath UTF8 -InputObject $content
}
