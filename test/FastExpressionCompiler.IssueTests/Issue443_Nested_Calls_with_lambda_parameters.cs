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
public class Issue443_Nested_Calls_with_lambda_parameters : ITest
{
    public int Run()
    {
        // Original_case();
        // Case_with_Invoke_NoInlining();
        Case_with_Invoke();
        return 3;
    }

    public class TestClass
    {
        public static int ExecuteDelegate(Func<int> action) => action();
    }

    [Test]
    public void Original_case()
    {
        var executeDelegate = typeof(TestClass).GetMethod(nameof(TestClass.ExecuteDelegate));
        var local = Variable(typeof(int), "local");

        var innerLambda =
            Lambda<Func<int>>(
                Block(
                    new ParameterExpression[] { local },
                    Assign(local, Constant(42)),
                    // Call does not work
                    Call(executeDelegate, Lambda<Func<int>>(local))
                )
            );

        var expr = Lambda<Func<int>>(
            Call(executeDelegate, innerLambda)
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(42, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(42, fr);
    }

    [Test]
    public void Case_with_Invoke()
    {
        var executeDelegate = typeof(TestClass).GetMethod(nameof(TestClass.ExecuteDelegate));
        var local = Variable(typeof(int), "local");

        var innerLambda =
            Lambda<Func<int>>(
                Block(
                    new ParameterExpression[] { local },
                    Assign(local, Constant(42)),

                    // Invoke works
                    Invoke(Lambda<Func<int>>(local))
                )
            );

        var expr = Lambda<Func<int>>(
            Call(executeDelegate, innerLambda)
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(42, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(42, fr);
    }

    [Test]
    public void Case_with_Invoke_NoInlining()
    {
        var executeDelegate = typeof(TestClass).GetMethod(nameof(TestClass.ExecuteDelegate));
        var local = Variable(typeof(int), "local");

        var innerLambda =
            Lambda<Func<int>>(
                Block(
                    new ParameterExpression[] { local },
                    Assign(local, Constant(42)),

                    // Invoke works
                    Invoke(Lambda<Func<int>>(local))
                )
            );

        var expr = Lambda<Func<int>>(
            Call(executeDelegate, innerLambda)
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(42, sr);

        var ff = expr.CompileFast(false, CompilerFlags.NoInvocationLambdaInlining);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(42, fr);
    }
}