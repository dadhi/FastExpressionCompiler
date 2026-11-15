using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ScalarArithmeticFrameTests : ITestX
{
    public void Run(TestRun t) { Scalar_and_lifted_paths_preserve_results(t); Earlier_finalizers_remain_observable(t); }
    public void Scalar_and_lifted_paths_preserve_results(TestContext t)
    {
        var a=S.Parameter(typeof(int));var b=S.Parameter(typeof(int));
        foreach(var operation in new[]{System.Linq.Expressions.ExpressionType.Add,System.Linq.Expressions.ExpressionType.Subtract,System.Linq.Expressions.ExpressionType.Multiply,System.Linq.Expressions.ExpressionType.Divide})
        {
            var tree=S.Lambda<Func<int,int,int>>(S.MakeBinary(operation,a,b),a,b);var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;
            foreach(var x in new[]{-7,0,7})t.AreEqual(tree.Compile()(x,3),fast(x,3));
        }
        var n=S.Parameter(typeof(int?));var lifted=S.Lambda<Func<int?,int?>>(S.Add(n,S.Constant((int?)3,typeof(int?))),n);var f=lifted.FromSysExpression().CompileFast(true);if(!t.IsNotNull(f))return;
        foreach(var x in new int?[]{null,7})t.AreEqual(lifted.Compile()(x),f(x));
    }
    public void Earlier_finalizers_remain_observable(TestContext t)
    {
        var n=S.Parameter(typeof(int));var tree=S.Lambda<Func<int,int>>(S.Add(S.TryFinally(n,S.Assign(n,S.Constant(7))),n),n);
        var fast=tree.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.AreEqual(tree.Compile()(3),fast(3));
    }
}
