using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct TerminalCollectionTests : ITestX
{
    public void Run(TestRun t) { Bound_constants_keep_reference_identity(t); Undefined_parameters_are_still_rejected(t); }
    public void Bound_constants_keep_reference_identity(TestContext t)
    {
        var value=new object();var boxed=(object)7;
        foreach(var item in new[]{value,boxed,new string(new[]{'t','e','x','t'})})
        {
            var tree=S.Lambda<Func<object>>(S.Constant(item,typeof(object)));var fast=tree.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.IsTrue(ReferenceEquals(item,fast()));
        }
        var input=S.Parameter(typeof(int));var nested=S.Lambda<Func<int,Func<int>>>(S.Lambda<Func<int>>(input),input);var child=nested.FromSysExpression().CompileFast(true);if(t.IsNotNull(child))t.AreEqual(7,child(7)());
    }
    public void Undefined_parameters_are_still_rejected(TestContext t)
    {
        var tree=S.Lambda<Func<int>>(S.Parameter(typeof(int)));t.AreEqual(null,tree.FromSysExpression().CompileFast(true));
        t.Throws<NotSupportedExpressionException>(()=>tree.FromSysExpression().CompileFast(true,CompilerFlags.ThrowOnNotSupportedExpression));
        var typed=S.Lambda<Func<int>>(S.Default(typeof(int)));var fast=typed.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.AreEqual(0,fast());
    }
}
