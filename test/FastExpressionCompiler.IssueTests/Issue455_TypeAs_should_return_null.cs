using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

public class Issue455_TypeAs_should_return_null : ITest
{
    public int Run()
    {
        Original_case();
        return 1;
    }

    public void Original_case()
    {
        var x = Parameter(typeof(int?), "x");

        var expr = Lambda<Func<int?>>(
            Block(
                new[] { x },
                Assign(x, TypeAs(Constant(12345L), typeof(int?)))
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(null, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(null, fr);
    }
}