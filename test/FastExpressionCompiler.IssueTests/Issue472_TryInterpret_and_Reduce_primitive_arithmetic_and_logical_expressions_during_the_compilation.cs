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

public struct Issue472_TryInterpret_and_Reduce_primitive_arithmetic_and_logical_expressions_during_the_compilation : ITestX
{
    public void Run(TestRun t)
    {
        Logical_expression_started_with_not_Without_Interpreter_due_param_use(t);
        Logical_expression_started_with_not(t);
    }

    public void Logical_expression_started_with_not(TestContext t)
    {
        var p = Parameter(typeof(bool), "p");
        var expr = Lambda<Func<bool, bool>>(
            OrElse(
                Not(AndAlso(Constant(true), Constant(false))), p),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.IsTrue(fs(true));
        t.IsTrue(fs(false));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.IsTrue(ff(true));
        t.IsTrue(ff(false));
    }

    public void Logical_expression_started_with_not_Without_Interpreter_due_param_use(TestContext t)
    {
        var p = Parameter(typeof(bool), "p");
        var expr = Lambda<Func<bool, bool>>(
            Not(AndAlso(Constant(true), p)),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.IsFalse(fs(true));
        t.IsTrue(fs(false));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.IsFalse(ff(true));
        t.IsTrue(ff(false));
    }
}