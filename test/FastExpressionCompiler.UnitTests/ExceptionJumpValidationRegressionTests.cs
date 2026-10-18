using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ExceptionJumpValidationRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Jumps_cannot_enter_exception_regions(t);
        Jumps_cannot_enter_sibling_handlers(t);
        Jumps_cannot_leave_finally_or_fault(t);
        Jumps_cannot_leave_filters(t);
        Jump_out_of_try_executes_finally(t);
        Jump_out_of_catch_executes_finally(t);
    }
    private static void Invalid(TestContext t, System.Linq.Expressions.Expression<Action> expression)
    {
        t.Throws<InvalidOperationException>(() => expression.Compile());
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.ThrowOnNotSupportedExpression })
            t.Throws<InvalidOperationException>(() => expression.FromSysExpression().CompileFast(true, flags));
    }
    public void Jumps_cannot_enter_exception_regions(TestContext t)
    {
        var target = S.Label();
        Invalid(t, S.Lambda<Action>(S.Block(S.Goto(target), S.TryFinally(S.Label(target), S.Empty()))));
        Invalid(t, S.Lambda<Action>(S.Block(S.Goto(target), S.TryFinally(S.Empty(), S.Label(target)))));
        Invalid(t, S.Lambda<Action>(S.Block(S.Goto(target), S.TryCatch(S.Empty(), S.Catch(typeof(Exception), S.Label(target))))));
    }
    public void Jumps_cannot_enter_sibling_handlers(TestContext t)
    {
        var target = S.Label();
        Invalid(t, S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))),
            S.Catch(typeof(ArgumentException), S.Goto(target)), S.Catch(typeof(Exception), S.Label(target)))));
    }
    public void Jumps_cannot_leave_finally_or_fault(TestContext t)
    {
        var target = S.Label();
        foreach (var fault in new[] { false, true })
        {
            var body = fault ? S.TryFault(S.Empty(), S.Goto(target)) : S.TryFinally(S.Empty(), S.Goto(target));
            Invalid(t, S.Lambda<Action>(S.Block(body, S.Label(target))));
        }
    }
    public void Jumps_cannot_leave_filters(TestContext t)
    {
        var target = S.Label();
        var filter = S.Block(S.Goto(target), S.Constant(true));
        Invalid(t, S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))),
            S.Catch(typeof(Exception), S.Label(target), filter))));
    }
    public static int Finalized;
    public static void Finally() => ++Finalized;
    private static System.Linq.Expressions.MethodCallExpression FinallyCall() => S.Call(typeof(ExceptionJumpValidationRegressionTests).GetMethod(nameof(Finally)));
    private static void Verify(TestContext t, System.Linq.Expressions.Expression<Action> expression)
    {
        Finalized = 0; expression.Compile()(); t.AreEqual(1, Finalized);
        var fast = expression.FromSysExpression().CompileFast(true); if (!t.IsNotNull(fast)) return;
        Finalized = 0; fast(); t.AreEqual(1, Finalized);
    }
    public void Jump_out_of_try_executes_finally(TestContext t)
    {
        var target = S.Label();
        Verify(t, S.Lambda<Action>(S.Block(S.TryFinally(S.Goto(target), FinallyCall()), S.Label(target))));
    }
    public void Jump_out_of_catch_executes_finally(TestContext t)
    {
        var target = S.Label();
        Verify(t, S.Lambda<Action>(S.Block(S.TryCatchFinally(S.Throw(S.New(typeof(Exception))), FinallyCall(),
            S.Catch(typeof(Exception), S.Goto(target))), S.Label(target))));
    }
}
