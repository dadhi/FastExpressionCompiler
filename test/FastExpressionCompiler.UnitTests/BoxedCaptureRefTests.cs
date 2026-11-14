using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct BoxedCaptureRefTests : ITestX
{
    public struct Counter { public int Value; }
    public static int Increment(ref Counter counter)=>++counter.Value;
    public void Run(TestRun t)=>Captured_value_arguments_use_the_existing_box_address(t);
    public void Captured_value_arguments_use_the_existing_box_address(TestContext t)
    {
        var counter=S.Parameter(typeof(Counter));
        var increment=typeof(BoxedCaptureRefTests).GetMethod(nameof(Increment));
        var inner=S.Lambda<Func<int>>(S.Call(increment,counter));
        var tree=S.Lambda<Func<Counter,Func<int>>>(inner,counter);
        foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining})
        {
            var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
            foreach(var outer in new[]{tree.Compile(),fast})
            {
                var first=outer(new Counter{Value=5});var second=outer(new Counter{Value=10});
                t.AreEqual(6,first());t.AreEqual(7,first());t.AreEqual(11,second());t.AreEqual(8,first());
            }
        }
    }
}
