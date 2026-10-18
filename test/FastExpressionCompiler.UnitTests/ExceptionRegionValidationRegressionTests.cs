using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ExceptionRegionValidationRegressionTests : ITestX
{
    public void Run(TestRun t)
    {

        Catch_filters_reject_nested_exception_blocks(t);
        Catch_filters_reject_undefined_variables(t);
        Nested_try_inside_catch_can_rethrow_original(t);

        Separately_compiled_lambda_does_not_inherit_outer_catch(t);
    }
    private static void Invalid(TestContext t, System.Linq.Expressions.Expression<Action> expression)
    {
        t.Throws<InvalidOperationException>(() => expression.Compile());
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.ThrowOnNotSupportedExpression })
            t.Throws<InvalidOperationException>(() => expression.FromSysExpression().CompileFast(true, flags));
    }

    public void Catch_filters_reject_nested_exception_blocks(TestContext t)
    {
        foreach (var filter in new System.Linq.Expressions.Expression[] {
            S.TryFinally(S.Constant(true), S.Empty()),
            S.TryFault(S.Constant(true), S.Empty()), S.TryCatch(S.Constant(true), S.Catch(typeof(Exception), S.Constant(false))) })
            Invalid(t, S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))), S.Catch(typeof(Exception), S.Empty(), filter))));
    }
    public void Catch_filters_reject_undefined_variables(TestContext t)
    {
        foreach (var variable in new[] { false, true })
        {
            var filter = S.Parameter(typeof(bool), "undefined");
            var handler = variable ? S.Catch(S.Parameter(typeof(Exception), "caught"), S.Empty(), filter) : S.Catch(typeof(Exception), S.Empty(), filter);
            Invalid(t, S.Lambda<Action>(S.TryCatch(S.Empty(), handler)));
        }
    }
    public static int Finalized;
    public static void Finally() => ++Finalized;
    public void Nested_try_inside_catch_can_rethrow_original(TestContext t)
    {
        var original = new InvalidOperationException();
        var caught = S.Parameter(typeof(Exception));
        var inner = S.TryCatch(S.Throw(S.Constant(original), typeof(bool)),
            S.Catch(typeof(Exception), S.TryFinally(S.Rethrow(typeof(bool)), S.Call(typeof(ExceptionRegionValidationRegressionTests).GetMethod(nameof(Finally))))));
        var tree = S.Lambda<Func<bool>>(S.TryCatch(inner, S.Catch(caught, S.ReferenceEqual(caught, S.Constant(original)))));
        Finalized = 0; t.IsTrue(tree.Compile()()); t.AreEqual(1, Finalized);
        var fast = tree.FromSysExpression().CompileFast(true); if (!t.IsNotNull(fast)) return;
        Finalized = 0; t.IsTrue(fast()); t.AreEqual(1, Finalized);
    }

    public void Separately_compiled_lambda_does_not_inherit_outer_catch(TestContext t)
    {
        var invalid = S.Lambda<Action>(S.Rethrow());
        var tree = S.Lambda<Action>(S.TryCatch(S.Empty(), S.Catch(typeof(Exception), S.Invoke(invalid))));
        t.Throws<InvalidOperationException>(() => tree.Compile());
        t.Throws<InvalidOperationException>(() => tree.FromSysExpression().CompileFast(true, CompilerFlags.NoInvocationLambdaInlining));
    }
}
