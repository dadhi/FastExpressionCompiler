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
        Caller_arguments_can_rethrow_original_exception(t);
        Saved_caller_parameter_wrapper_preserves_rethrow_scope(t);
        Callee_catches_and_subsequent_caller_rethrows_keep_their_scopes(t);
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
    public void Caller_arguments_can_rethrow_original_exception(TestContext t)
    {
        var first = S.Parameter(typeof(int));
        var second = S.Parameter(typeof(int));
        var lambda = S.Lambda<Func<int, int, bool>>(S.Call(typeof(InlinedRethrowBoundaryRegressionTests).GetMethod(nameof(Body)),
            S.Add(first, second)), first, second);
        var invoke = S.Invoke(lambda, S.Call(typeof(InlinedRethrowBoundaryRegressionTests).GetMethod(nameof(Argument))),
            S.Rethrow(typeof(int)));
        var tree = CatchOriginal(invoke, new InvalidOperationException("original"));
        ArgumentCalls = BodyCalls = 0; t.IsTrue(tree.Compile()());
        t.AreEqual(1, ArgumentCalls); t.AreEqual(0, BodyCalls);
        foreach (var flags in Flags)
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            ArgumentCalls = BodyCalls = 0; t.IsTrue(fast());
            t.AreEqual(1, ArgumentCalls); t.AreEqual(0, BodyCalls);
        }
    }
    public void Saved_caller_parameter_wrapper_preserves_rethrow_scope(TestContext t)
    {
        var shared = S.Parameter(typeof(int));
        var other = S.Parameter(typeof(int));
        var lambda = S.Lambda<Func<int, int, bool>>(S.Call(typeof(InlinedRethrowBoundaryRegressionTests).GetMethod(nameof(Body)),
            S.Add(shared, other)), shared, other);
        var body = CatchOriginal(S.Invoke(lambda, shared, S.Rethrow(typeof(int))), new InvalidOperationException("original")).Body;
        var tree = S.Lambda<Func<int, bool>>(body, shared);
        BodyCalls = 0; t.IsTrue(tree.Compile()(5)); t.AreEqual(0, BodyCalls);
        foreach (var flags in Flags)
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            BodyCalls = 0; t.IsTrue(fast(5)); t.AreEqual(0, BodyCalls);
        }
    }
    public void Callee_catches_and_subsequent_caller_rethrows_keep_their_scopes(TestContext t)
    {
        var original = new InvalidOperationException("original");
        var ownCatch = S.Lambda<Func<bool>>(S.TryCatch(S.Throw(S.Constant(original), typeof(bool)),
            S.Catch(typeof(Exception), S.Rethrow(typeof(bool)))));
        var caught = S.Parameter(typeof(Exception));
        var tree = S.Lambda<Func<bool>>(S.TryCatch(S.Invoke(ownCatch),
            S.Catch(caught, S.ReferenceEqual(caught, S.Constant(original)))));
        var caller = CatchOriginal(S.Block(S.Invoke(S.Lambda<Action>(S.Empty())), S.Rethrow(typeof(bool))), original);
        t.IsTrue(tree.Compile()()); t.IsTrue(caller.Compile()());
        foreach (var flags in Flags)
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags); if (t.IsNotNull(fast)) t.IsTrue(fast());
            fast = caller.FromSysExpression().CompileFast(true, flags); if (t.IsNotNull(fast)) t.IsTrue(fast());
        }
    }
}
