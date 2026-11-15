using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct SpillMetadataBoundsTests : ITestX
{
    public sealed class Cell { public int this[int a,int b,int c,int d]=>a+b+c+d; }
    public void Run(TestRun t) { Array_bounds_cover_inline_and_overflow_metadata(t); All_index_operands_are_restored(t); }
    public void Array_bounds_cover_inline_and_overflow_metadata(TestContext t)
    {
        foreach(var rank in new[]{1,4,8,12})
        {
            var bounds=new System.Linq.Expressions.Expression[rank];for(var i=0;i<rank;++i)bounds[i]=S.TryFinally(S.Constant(1),S.Empty());
            var tree=S.Lambda<Func<Array>>(S.Convert(S.NewArrayBounds(typeof(int),bounds),typeof(Array)));
            var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;var result=fast();t.AreEqual(rank,result.Rank);t.AreEqual(1L,result.LongLength);
            t.AreEqual(tree.Compile()().LongLength,result.LongLength);
        }
    }
    public void All_index_operands_are_restored(TestContext t)
    {
        var cell=S.Parameter(typeof(Cell));var args=new System.Linq.Expressions.Expression[4];for(var i=0;i<4;++i)args[i]=S.TryFinally(S.Constant(i+1),S.Empty());
        var tree=S.Lambda<Func<Cell,int>>(S.MakeIndex(cell,typeof(Cell).GetProperty("Item"),args),cell);var fast=tree.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.AreEqual(tree.Compile()(new Cell()),fast(new Cell()));
    }
}
