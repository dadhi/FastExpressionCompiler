using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue442_TryCatch_and_the_Goto_outside_problems : ITest
{
    public int Run()
    {
        // Original_case();
        return 1;
    }

    [Test]
    public void Original_case()
    {
        var label = Label("label");
        var variable = Variable(typeof(int), "variable");

        var block = Block(
            new[] { variable },
            TryCatch(
                Block(
                    Assign(variable, Constant(5)),
                    Goto(label)
                ),
                Catch(
                    typeof(Exception),
                    Block(
                        typeof(void),
                        Assign(variable, Constant(10))
                    )
                )
            ),
            Label(label),
            variable
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(5, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(5, fr);
    }
}