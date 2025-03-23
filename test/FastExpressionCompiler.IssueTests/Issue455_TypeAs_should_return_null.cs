using System;
using NUnit.Framework;
using System.Collections.Generic;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue444_TEMP : ITest
{
    public int Run()
    {
        Original_case();
        return 1;
    }

    [Test]
    public void Original_case()
    {
        var x1 = 12345L as int?;

        var x = Parameter(typeof(int?), "x");

        var expr = Lambda<Func<int?>>(
            Block(
                [x],
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