using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ReturnPatternScopeTests : ITestX
{
    public void Run(TestRun t)=>Return_pattern_restores_the_enclosing_variable_binding(t);
    public void Return_pattern_restores_the_enclosing_variable_binding(TestContext t)
    {
        var value=S.Parameter(typeof(int),"shadowed");var target=S.Label(typeof(int));
        var local=S.Block(new[]{value},S.Assign(value,S.Constant(7)),S.Return(target,value),S.Label(target,S.Constant(0)));
        var tree=S.Lambda<Func<int,int>>(S.Add(local,value),value);
        foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining})
        {
            var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
            t.AreEqual(10,fast(3));t.AreEqual(tree.Compile()(3),fast(3));
            t.AreEqual(18,fast(11));
        }
    }
}
