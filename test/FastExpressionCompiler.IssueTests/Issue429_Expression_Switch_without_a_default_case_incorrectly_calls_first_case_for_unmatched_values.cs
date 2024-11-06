using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue429_Expression_Switch_without_a_default_case_incorrectly_calls_first_case_for_unmatched_values : ITest
{
    public int Run()
    {
        Original_case();
        return 1;
    }

    public static void AddCase(string value, int n) => Debug.WriteLine($"{value}-{n}");

    [Test]
    public void Original_case()
    {
        var number = Parameter(typeof(int), "n");
        var caseMethod = GetType().GetMethod(nameof(AddCase));

        var expr = Lambda<Action<int>>(
            Switch(
                number,
                new SwitchCase[]
                {
                    SwitchCase(Call(null, caseMethod, Constant("Case"), Constant(1)), Constant(1)),
                    SwitchCase(Call(null, caseMethod, Constant("Case"), Constant(2)), Constant(2))
                }
            ),
            number);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        fs(3);

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();
        ff(3);
    }
}