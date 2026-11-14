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


    }
    private static void Invalid(TestContext t, System.Linq.Expressions.Expression<Action> expression, bool frameworkDynamicMethodUnsupported = false)
    {
#if NETFRAMEWORK
        // Framework DynamicMethod rejects fault/filter IL before System's semantic validation.
        if (frameworkDynamicMethodUnsupported)
            t.Throws<NotSupportedException>(() => expression.Compile());
        else
#endif
        t.Throws<InvalidOperationException>(() => expression.Compile());
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.ThrowOnNotSupportedExpression })
            t.Throws<InvalidOperationException>(() => expression.FromSysExpression().CompileFast(true, flags));
    }

    public void Catch_filters_reject_nested_exception_blocks(TestContext t)
    {
        foreach (var filter in new System.Linq.Expressions.Expression[] {
            S.TryFinally(S.Constant(true), S.Empty()),
            S.TryFault(S.Constant(true), S.Empty()), S.TryCatch(S.Constant(true), S.Catch(typeof(Exception), S.Constant(false))) })
            Invalid(t, S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))), S.Catch(typeof(Exception), S.Empty(), filter))), frameworkDynamicMethodUnsupported: true);
    }
    public void Catch_filters_reject_undefined_variables(TestContext t)
    {
        foreach (var variable in new[] { false, true })
        {
            var filter = S.Parameter(typeof(bool), "undefined");
            var handler = variable ? S.Catch(S.Parameter(typeof(Exception), "caught"), S.Empty(), filter) : S.Catch(typeof(Exception), S.Empty(), filter);
            Invalid(t, S.Lambda<Action>(S.TryCatch(S.Empty(), handler)), frameworkDynamicMethodUnsupported: true);
        }
    }
}
