using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct SwitchCaseLabelTests : ITestX
{
    public void Run(TestRun t) { Cross_case_jumps_stay_inside_switch_scope(t); Outside_jumps_cannot_enter_the_switch(t); }
    public void Cross_case_jumps_stay_inside_switch_scope(TestContext t)
    {
        foreach(var withDefault in new[]{false,true})
        {
            var input=S.Parameter(typeof(int));var target=S.Label(typeof(int));
            var body=S.Switch(typeof(int),input,withDefault?S.Block(S.Label(target,S.Constant(2))):S.Constant(0),null,
                S.SwitchCase(S.Block(S.Goto(target,S.Constant(29)),S.Constant(1)),S.Constant(0)),
                S.SwitchCase(withDefault?(System.Linq.Expressions.Expression)S.Constant(2):S.Block(S.Label(target,S.Constant(2))),S.Constant(1)));
            var tree=S.Lambda<Func<int,int>>(body,input);
            foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo})
            {
                var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
                foreach(var n in new[]{0,1,2})t.AreEqual(tree.Compile()(n),fast(n));
            }
        }
    }
    public void Outside_jumps_cannot_enter_the_switch(TestContext t)
    {
        var input=S.Parameter(typeof(int));var target=S.Label(typeof(int));
        var body=S.Switch(input,S.Constant(0),S.SwitchCase(S.Block(S.Label(target,S.Constant(2))),S.Constant(1)));
        var tree=S.Lambda<Func<int,int>>(S.Block(S.Goto(target,S.Constant(7)),body),input);
        t.Throws<InvalidOperationException>(()=>tree.Compile());t.Throws<InvalidOperationException>(()=>tree.FromSysExpression().CompileFast(true));
    }
}
