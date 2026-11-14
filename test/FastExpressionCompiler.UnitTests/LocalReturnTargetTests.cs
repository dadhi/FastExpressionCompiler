using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct LocalReturnTargetTests : ITestX
{
    public void Run(TestRun t) { Ignored_blocks_keep_their_continuations(t); Inlined_jumps_use_their_actual_targets(t); }
    public void Ignored_blocks_keep_their_continuations(TestContext t)
    {
        var target=S.Label(typeof(int));
        var local=S.Block(S.Return(target,S.Constant(1)),S.Label(target,S.Constant(0)));
        var tree=S.Lambda<Func<int>>(S.Block(local,S.Constant(7)));
        var fast=tree.FromSysExpression().CompileFast(true);
        if(t.IsNotNull(fast))t.AreEqual(tree.Compile()(),fast());
    }
    public void Inlined_jumps_use_their_actual_targets(TestContext t)
    {
        foreach(var returnKind in new[]{false,true})
        foreach(var argument in new[]{false,true})
        {
            var target=S.Label(typeof(int));var value=S.Parameter(typeof(int));
            var jump=returnKind?S.Return(target,S.Constant(1)):S.Goto(target,S.Constant(1));
            var body=S.Block(jump,S.Label(target,S.Constant(2)),S.Constant(3));
            var invoked=argument?S.Invoke(S.Lambda<Func<int,int>>(body,value),S.Constant(0))
                :S.Invoke(S.Lambda<Func<int>>(body));
            var tree=S.Lambda<Func<int>>(S.Add(S.Constant(10),invoked));
            foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining})
            {
                var fast=tree.FromSysExpression().CompileFast(true,flags);
                if(t.IsNotNull(fast))t.AreEqual(tree.Compile()(),fast());
            }
        }
    }
}
