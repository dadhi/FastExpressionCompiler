using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct NestedLambdaValidationTests : ITestX
{
    public void Run(TestRun t) { Separately_compiled_lambdas_validate_own_labels(t); }
    public void Separately_compiled_lambdas_validate_own_labels(TestContext t)
    {
        foreach (var body in new System.Linq.Expressions.Expression[] { S.Rethrow(), S.Goto(S.Label("missing")) })
        {
            var inner = S.Lambda<Action>(body);
            var tree = S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))), S.Catch(typeof(Exception), S.Invoke(inner))));
            t.Throws<InvalidOperationException>(() => tree.Compile());
            t.Throws<InvalidOperationException>(() => tree.FromSysExpression().CompileFast(true, CompilerFlags.NoInvocationLambdaInlining));
        }
        var valid = S.Lambda<Func<Action>>(S.Lambda<Action>(S.TryCatch(S.Throw(S.New(typeof(Exception))), S.Catch(typeof(Exception), S.Empty()))));
        var fast = valid.FromSysExpression().CompileFast(true, CompilerFlags.NoInvocationLambdaInlining);
        if (t.IsNotNull(fast)) fast()();
    }

}
