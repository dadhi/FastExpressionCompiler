using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct IndexJumpOperandTests : ITestX
{
    public sealed class Cell { public int Value=7;public int this[int i] {get=>Value+i;set=>Value=value;} }
    public void Run(TestRun t) { Index_jumps_discard_receivers(t); Index_assignment_jumps_do_not_write(t); }
    public void Index_jumps_discard_receivers(TestContext t)
    {
        var leave=S.Parameter(typeof(bool));var target=S.Label(typeof(int));
        var index=S.Condition(leave,S.Goto(target,S.Constant(19),typeof(int)),S.Constant(0));
        foreach(var arrayKind in new[]{0,1,2,3})
        {
            System.Linq.Expressions.Expression access;
            if(arrayKind==0)access=S.ArrayIndex(S.Constant(new[]{7}),index);
            else if(arrayKind==1)access=S.ArrayAccess(S.Constant(new[]{7}),index);
            else if(arrayKind==2)access=S.ArrayAccess(S.Constant(new[,]{{7}}),index,S.Constant(0));
            else access=S.MakeIndex(S.Constant(new Cell()),typeof(Cell).GetProperty("Item"),new[]{index});
            var tree=S.Lambda<Func<bool,int>>(S.Block(access,S.Label(target,S.Constant(8))),leave);
            foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo})
            {
                var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
                foreach(var input in new[]{false,true})t.AreEqual(tree.Compile()(input),fast(input));
            }
        }
    }
    public void Index_assignment_jumps_do_not_write(TestContext t)
    {
        var leave=S.Parameter(typeof(bool));var cell=S.Parameter(typeof(Cell));var target=S.Label(typeof(int));
        var index=S.Condition(leave,S.Goto(target,S.Constant(19),typeof(int)),S.Constant(0));
        var tree=S.Lambda<Func<Cell,bool,int>>(S.Block(S.Assign(S.MakeIndex(cell,typeof(Cell).GetProperty("Item"),new[]{index}),S.Constant(11)),S.Label(target,S.Constant(8))),cell,leave);
        var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;
        foreach(var input in new[]{false,true})
        {var a=new Cell();var b=new Cell();t.AreEqual(tree.Compile()(a,input),fast(b,input));t.AreEqual(a.Value,b.Value);}
    }
}
