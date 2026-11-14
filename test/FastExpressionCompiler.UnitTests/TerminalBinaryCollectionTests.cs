using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct TerminalBinaryCollectionTests : ITestX
{
    public void Run(TestRun t) { Reference_constants_keep_box_identity(t); Undefined_parameters_still_fail(t); Nonterminal_operands_keep_normal_collection(t); }
    public void Reference_constants_keep_box_identity(TestContext t)
    {
        var input=S.Parameter(typeof(object));var box=(object)7;
        var tree=S.Lambda<Func<object,bool>>(S.ReferenceEqual(input,S.Constant(box,typeof(object))),input);
        var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;t.IsTrue(fast(box));t.IsTrue(!fast((object)7));
    }
    public void Undefined_parameters_still_fail(TestContext t)
    {
        var tree=S.Lambda<Func<int>>(S.Add(S.Parameter(typeof(int)),S.Constant(7)));t.AreEqual(null,tree.FromSysExpression().CompileFast(true));
    }
    public void Nonterminal_operands_keep_normal_collection(TestContext t)
    {
        var input=S.Parameter(typeof(int));var tree=S.Lambda<Func<int,int>>(S.Add(input,S.TryFinally(S.Constant(3),S.Empty())),input);
        var fast=tree.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.AreEqual(tree.Compile()(4),fast(4));
    }
}
