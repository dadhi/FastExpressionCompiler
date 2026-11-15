using System;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#elif !FLAT_EXPRESSION
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#else
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.FlatExpression.UnitTests;
#endif

public struct IntAddCompileTests : ITestX
{
    public void Run(TestRun t) => Can_add_two_ints(t);

#if FLAT_EXPRESSION
    ExprTree Expr;
    public ExprTree ExprTree => Expr;
    public void ResetExpr() => Expr = default;
#else
    public void ResetExpr() { }
#endif

    public void Can_add_two_ints(TestContext t)
    {
        ResetExpr();
        var a = Expr.Parameter(typeof(int), "a");
        var b = Expr.Parameter(typeof(int), "b");
        var expression = Expr.Lambda<Func<int, int, int>>(Expr.Add(a, b), a, b);
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(4, fast(1, 3));
    }
}
