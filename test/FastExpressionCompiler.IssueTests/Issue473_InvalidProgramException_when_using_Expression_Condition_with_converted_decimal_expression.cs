using System;

#if LIGHT_EXPRESSION
using ExpressionType = System.Linq.Expressions.ExpressionType;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue473_InvalidProgramException_when_using_Expression_Condition_with_converted_decimal_expression : ITestX
{
    public void Run(TestRun t)
    {
        Original_case(t);
    }

    public void Original_case(TestContext t)
    {
        var left = Convert(Constant(0d), typeof(decimal));
        var right = Constant(3m);

        var cond = Condition(Equal(left, Default(left.Type)),
            right,
            Multiply(left, right));

        var expr = Lambda<Func<decimal>>(cond);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual(3, fs());

        var ff = expr.CompileFast(false, CompilerFlags.DisableInterpreter);
        ff.PrintIL();
        t.AreEqual(3, ff());

        var ffi = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual(3, ffi());
    }
}