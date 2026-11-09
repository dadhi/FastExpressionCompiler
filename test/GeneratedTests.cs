using System;

namespace FastExpressionCompiler.UnitTests;

// Explicit accessibility makes a missing generator a compiler error, never an empty test run.
internal static partial class GeneratedTests
{
    internal static partial void RunStandard(TestRun tests);
    internal static partial void RunLight(LightExpression.TestRun tests);
    internal static partial void RunContracts(TestRun standard, LightExpression.TestRun light);
    internal static partial void RunUnitLegacy(Action<Func<int>, string> run);
    internal static partial void RunIssueLegacy(Action<Func<int>, string> run);
}
