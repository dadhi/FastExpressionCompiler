using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue458_Support_TryFault : ITest, ITestX
{
    public int Run()
    {
        Original_case1();
        Original_case2();
        return 2;
    }

    public void Run(TestRun tr)
    {
        Original_case1(tr);
    }

    public void Original_case1(TestContext tx = default)
    {
        var expr = Lambda<Func<bool>>(
            TryFault(
                Constant(true),
                Constant(false)
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.IsTrue(sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.IsTrue(fr);
    }

    public void Original_case2(TestContext tx = default)
    {
        var x = Parameter(typeof(int), "x");

        var expr = Lambda<Func<bool>>(
            Block(
                [x],
                TryCatch(
                    TryFault( // fault is like a try finally, but for exceptions
                        Block(
                            Assign(x, Constant(1)),
                            Throw(New(typeof(Exception)), typeof(int))
                        ),
                        Assign(x, Constant(2))
                    ),
                    // catch so we can verify fault ran
                    Catch(typeof(Exception), Constant(-1))
                ),
                x
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.IsFalse(sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.IsFalse(fr);
    }
}