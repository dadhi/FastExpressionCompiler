using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct EmptyRuntimeVariablesTests : ITestX
{
    public void Run(TestRun t) { Empty_runtime_variables_preserve_contract(t); }
    public void Empty_runtime_variables_preserve_contract(TestContext t)
    {
        var tree = S.Lambda<Func<System.Runtime.CompilerServices.IRuntimeVariables>>(S.RuntimeVariables()).FromSysExpression();
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        {
            var fast = tree.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            var variables = fast(); t.AreEqual(0, variables.Count);
            t.Throws<IndexOutOfRangeException>(() => { var unused = variables[0]; });
            t.Throws<IndexOutOfRangeException>(() => variables[0] = 1);
        }
        var ignored = S.Lambda<Action>(S.Block(S.RuntimeVariables(), S.Empty())).FromSysExpression().CompileFast(true);
        if (t.IsNotNull(ignored)) ignored();
    }

}
