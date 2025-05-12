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

public struct Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET : ITestX
{
    public void Run(TestRun t)
    {
        Original_expression(t);
        Original_expression_with_closure(t);
    }

    // Exposing for the benchmarking
    public static Expression<Func<bool>> CreateExpression(
#if LIGHT_EXPRESSION
        bool addClosure = false
#endif
    )
    {
        var e = new Expression[11]; // the unique expressions
        var expr = Lambda<Func<bool>>(
        e[0] = MakeBinary(ExpressionType.Equal,

            e[1] = MakeBinary(ExpressionType.Equal,
                e[2] = MakeBinary(ExpressionType.Add,
                    e[3] = Constant(1),
                    e[4] = Constant(2)),
                e[5] = MakeBinary(ExpressionType.Add,
                    e[6] = Constant(5),
                    e[7] = Constant(-2))),

            e[8] = MakeBinary(ExpressionType.Equal,
                e[9] = Constant(42),
#if LIGHT_EXPRESSION
                e[10] = !addClosure ? Constant(42) : ConstantRef(42, out _)
#else
                e[10] = Constant(42)
#endif
            )), new ParameterExpression[0]);
        return expr;
    }

    public void Original_expression(TestContext t)
    {
        var expr = CreateExpression();

        expr.PrintCSharp();
        // outputs:
        var @cs = (Func<bool>)(() => //bool
            ((1 + 2) == (5 + -2)) == (42 == 42));

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.IsTrue(fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.IsTrue(ff());

        var ffe = expr.CompileFast(false, CompilerFlags.DisableInterpreter);
        ffe.PrintIL();
        t.IsTrue(ffe());
    }

    public void Original_expression_with_closure(TestContext t)
    {
#if LIGHT_EXPRESSION
        var expr = CreateExpression(true);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.IsTrue(fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.IsTrue(ff());
#endif
    }
}