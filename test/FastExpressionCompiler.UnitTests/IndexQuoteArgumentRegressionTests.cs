using System;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct IndexQuoteArgumentRegressionTests : ITestX
{
    public void Run(TestRun t) { Quoted_lambda_indices_preserve_expression_identity(t); Multiple_indices_preserve_quoted_lambda(t); }
    public void Quoted_lambda_indices_preserve_expression_identity(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int>> key = () => 2;
        var dictionary = new Dictionary<System.Linq.Expressions.Expression<Func<int>>, int> { [key] = 9 };
#if LIGHT_EXPRESSION
        // Light conversion creates a different lambda object; pass the existing System expression as a constant.
        var index = E.Constant(key, typeof(System.Linq.Expressions.Expression<Func<int>>));
#else
        var index = key;
#endif
        var lambda = E.Lambda<Func<int>>(E.Property(E.Constant(dictionary), dictionary.GetType().GetProperty("Item"), index));
        var fast = lambda.CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(9, fast());
    }
    public void Multiple_indices_preserve_quoted_lambda(TestContext t)
    {
#if LIGHT_EXPRESSION
        var argument = E.Quote(E.Lambda<Action>(E.Empty()));
#else
        var argument = E.Lambda<Action>(E.Empty());
#endif
        var target = new Indexed();
        var lambda = E.Lambda<Func<bool>>(E.Property(E.Constant(target), typeof(Indexed).GetProperty("Item"), E.Constant(3), argument));
        var fast = lambda.CompileFast(true);
        if (t.IsNotNull(fast)) { t.AreEqual(true, fast()); t.AreEqual(3, target.Seen); }
    }
    public class Indexed { public int Seen; public bool this[int index, System.Linq.Expressions.Expression<Action> action] { get { Seen = index; return action != null && action.Body.NodeType == System.Linq.Expressions.ExpressionType.Default; } } }
}
