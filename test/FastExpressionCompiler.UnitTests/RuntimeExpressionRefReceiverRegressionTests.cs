using System;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RuntimeExpressionRefReceiverRegressionTests : ITestX
{
    public void Run(TestRun t) { Ref_expression_receiver_is_loaded_as_a_value(t);  }
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
    public void Ref_expression_receiver_is_loaded_as_a_value(TestContext t)
    {
        var receiver = E.Parameter(typeof(System.Linq.Expressions.Expression<Action>).MakeByRefType());
        var expression = E.Lambda<RefExpressionAction>(E.Invoke(receiver), receiver);
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            System.Linq.Expressions.Expression<Action> value = () => Record(); var original = value;
            Calls = 0; fast(ref value); t.AreEqual(1, Calls); t.AreEqual(true, ReferenceEquals(original, value));
            value = () => Record(); fast(ref value); t.AreEqual(2, Calls);
            value = null; t.Throws<NullReferenceException>(() => fast(ref value));
        }
    }

    public delegate void RefExpressionAction(ref System.Linq.Expressions.Expression<Action> value);
    public static int Calls;
    public static void Record() => ++Calls;
}
