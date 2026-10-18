using System;
using System.Linq;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ExcessiveArityRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Unrepresentable_method_signatures_are_rejected_during_compilation(t);
        Large_supported_signatures_keep_argument_indices(t);
    }

    public void Unrepresentable_method_signatures_are_rejected_during_compilation(TestContext t)
    {
        var parameters = Enumerable.Range(0, ushort.MaxValue).Select(_ => S.Parameter(typeof(int))).ToArray();
        var system = S.Lambda(parameters[parameters.Length - 1], parameters);
        var expression = system.FromSysExpression();
        t.Throws<InvalidProgramException>(() => expression.CompileFast(true));
        t.Throws<InvalidProgramException>(() => expression.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
        t.Throws<InvalidProgramException>(() => system.Compile());
    }

    public void Large_supported_signatures_keep_argument_indices(TestContext t)
    {
        foreach (var count in new[] { 255, 256, 300 })
        {
            var parameters = Enumerable.Range(0, count).Select(_ => S.Parameter(typeof(int))).ToArray();
            var system = S.Lambda(S.Add(parameters[0], parameters[count - 1]), parameters);
            var fast = system.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var arguments = Enumerable.Range(0, count).Select(i => (object)i).ToArray();
            t.AreEqual(count - 1, fast.DynamicInvoke(arguments));
            t.AreEqual(system.Compile().DynamicInvoke(arguments), fast.DynamicInvoke(arguments));
        }
    }
}
