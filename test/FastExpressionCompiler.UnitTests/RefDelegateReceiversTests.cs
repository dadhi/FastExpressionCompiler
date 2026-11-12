using System;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RefDelegateReceiversTests : ITestX
{
    public void Run(TestRun t)
    {
        Ref_delegate_receivers_are_loaded_as_values(t);
    }
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };

    public void Ref_delegate_receivers_are_loaded_as_values(TestContext t)
    {
        var receiver = E.Parameter(typeof(Action).MakeByRefType());
        var expression = E.Lambda<RefDelegateAction>(E.Invoke(receiver), receiver);
        var ignoredReceiver = E.Parameter(typeof(Func<int>).MakeByRefType());
        var ignored = E.Lambda<RefIgnoredReceiver>(E.Invoke(ignoredReceiver), ignoredReceiver);
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            Action action = Record; var original = action;
            Calls = 0; fast(ref action); t.AreEqual(1, Calls); t.AreEqual(true, ReferenceEquals(original, action));
            action = null; t.Throws<NullReferenceException>(() => fast(ref action));
            var discard = ignored.CompileFast(true, flags); if (!t.IsNotNull(discard)) return;
            Func<int> function = () => { ++Calls; return 42; };
            discard(ref function); t.AreEqual(2, Calls);
            function = null; t.Throws<NullReferenceException>(() => discard(ref function));
        }
    }
    public delegate void RefExpressionAction(ref System.Linq.Expressions.Expression<Action> value);
    public delegate void RefDelegateAction(ref Action value);
    public delegate void RefIgnoredReceiver(ref Func<int> value);
    public static int Calls;
    public static void Record() => ++Calls;
}
