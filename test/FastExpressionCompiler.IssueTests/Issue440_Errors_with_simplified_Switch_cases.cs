using System;
using System.Linq.Expressions;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif


public class Issue440_Errors_with_simplified_Switch_cases : ITest
{
    public int Run()
    {
        Switch_with_single_case_without_default();
#if !NETFRAMEWORK
        Switch_with_no_cases_but_default();
        // Switch without cases is not supported in .NET 472
        Switch_with_no_cases();
        return 3;
#endif
        return 1;
    }


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
        Asserts.AreEqual(2, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(2, fr);
    }


    public void Switch_with_no_cases()
    {
        var block = Block(
            Switch(
                Constant(1),
                Tools.Empty<SwitchCase>()
            ),
            Constant(2)
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp(); // todo: @fixme for the empty switch - do not output the switch at all

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(2, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(2, fr);
    }


    public void Switch_with_no_cases_but_default()
    {
        var block = Block(
            Switch(
                Constant(1),
                Constant(42), // default case
                Tools.Empty<SwitchCase>()
            )
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(42, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(42, fr);
    }
}