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
        Original_case_2();
        Original_case_1();
        return 2;
    }

    [Test]
    public void Original_case_1()
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
        Asserts.AreEqual(5, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(5, fr);
    }

    [Test]
    public void Original_case_2()
    {
        // FEC throws `System.ArgumentException: Bad label content in ILGenerator.`
        var label = Label("label");
        var variable = Parameter(typeof(int));
        var exceptionParam = Parameter(typeof(Exception), "ex");

        var block = Block(
            new[] { variable },
            TryCatch(
                Block(
                    Goto(label),
                    Throw(Constant(new Exception("Exception"))),
                    Label(label),
                    Assign(variable, Constant(2))
                ),
                Catch(exceptionParam, Assign(variable, Constant(50)))
            ),
            variable
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(2, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(2, fr);
    }
}