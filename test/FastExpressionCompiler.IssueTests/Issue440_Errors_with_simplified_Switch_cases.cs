using System;
using System.Linq.Expressions;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue440_Errors_with_simplified_Switch_cases : ITest
{
    public int Run()
    {
        Switch_with_no_cases();
        Switch_with_single_case_without_default();
        return 1;
    }

    [Test]
    public void Switch_with_single_case_without_default()
    {
        var label = Label("after_switch");
        var block = Block(
            Switch(
                Constant(1),
                SwitchCase(Goto(label), Constant(1)) // This gets emitted as a conditional with a null False block
            ),
            Label(label),
            Constant(2)
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(2, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(2, fr);
    }

    [Test]
    public void Switch_with_no_cases()
    {
        var block = Block(
            Switch(
                Constant(1),
                Tools.Empty<SwitchCase>()
            ), // todo: @wip test with no cases but default body
            Constant(2)
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp(); // todo: @fixme for the empty switch

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(2, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(2, fr);
    }
}