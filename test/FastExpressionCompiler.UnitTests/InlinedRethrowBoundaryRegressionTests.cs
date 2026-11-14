using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct InlinedRethrowBoundaryRegressionTests : ITestX
{
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
    public void Run(TestRun t)
    {
        Callee_without_arguments_cannot_rethrow_callers_exception(t);
        Callee_with_arguments_cannot_rethrow_callers_exception(t);
        Caller_arguments_and_callee_catches_keep_their_scopes(t);

    }
    private static void Invalid(TestContext t, System.Linq.Expressions.Expression<Action> tree)
    {
        t.Throws<InvalidOperationException>(() => tree.Compile());
        foreach (var flags in Flags)
            t.Throws<InvalidOperationException>(() => tree.FromSysExpression().CompileFast(true, flags));
    }
    public void Callee_without_arguments_cannot_rethrow_callers_exception(TestContext t)
    {
        Invalid(t, S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))),
            S.Catch(typeof(Exception), S.Invoke(S.Lambda<Action>(S.Rethrow()))))));
    }
    public void Callee_with_arguments_cannot_rethrow_callers_exception(TestContext t)
    {
        var parameter = S.Parameter(typeof(int));
        Invalid(t, S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))),
            S.Catch(typeof(Exception), S.Invoke(S.Lambda<Action<int>>(S.Rethrow(), parameter), S.Constant(1))))));
    }
    public void Caller_arguments_and_callee_catches_keep_their_scopes(TestContext t)
    {
        var value = S.Parameter(typeof(int));
        var callee = S.Lambda<Func<int, bool>>(S.Constant(false), value);
        var original = new InvalidOperationException();
        var argumentRethrow = CatchOriginal(S.Invoke(callee, S.Rethrow(typeof(int))), original);
        foreach (var flags in Flags)
        {
            var fast = argumentRethrow.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            t.IsTrue(fast());
        }
        var ownCatch = S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(ArgumentException))),
            S.Catch(typeof(ArgumentException), S.Rethrow())));
        var outer = S.Lambda<Action>(S.Invoke(ownCatch)).FromSysExpression().CompileFast(true);
        if (t.IsNotNull(outer)) t.Throws<ArgumentException>(() => outer());
    }
    public static int ArgumentCalls;
    public static int BodyCalls;
    public static int Argument() { ++ArgumentCalls; return 1; }
    public static bool Body(int value) { ++BodyCalls; return false; }
    private static System.Linq.Expressions.Expression<Func<bool>> CatchOriginal(System.Linq.Expressions.Expression body,
        Exception original)
    {
        var caught = S.Parameter(typeof(Exception));
        return S.Lambda<Func<bool>>(S.TryCatch(
            S.TryCatch(S.Throw(S.Constant(original), typeof(bool)), S.Catch(typeof(Exception), body)),
            S.Catch(caught, S.ReferenceEqual(caught, S.Constant(original)))));
    }

}
