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
public class Issue437_Shared_variables_with_nested_lambdas_returning_incorrect_values : ITest
{
    public int Run()
    {
        Simplified_test();
        Simplified_test_no_inlining();

        // Nested_lambda_with_shared_variable_Workaround_with_struct();
        Nested_lambda_with_shared_variable();
        Nested_lambda_with_shared_variable_Workaround();
        return 2;
    }

    [Test]
    public void Simplified_test()
    {
        var myVar = Variable(typeof(int), "myVar");
        var expr = Lambda<Func<int>>(
            Block(
                new[] { myVar },
                Assign(myVar, Constant(5)),
                Invoke(Lambda<Action>(Assign(myVar, Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(3, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        if (ff.TryGetDebugClosureNestedLambda(0, out var nested))
            nested.PrintIL("nested");

        var fr = ff();
        Assert.AreEqual(3, fr);
    }

    [Test]
    public void Simplified_test_no_inlining()
    {
        var myVar = Variable(typeof(int), "myVar");
        var expr = Lambda<Func<int>>(
            Block(
                new[] { myVar },
                Assign(myVar, Constant(5)),
                Invoke(Lambda<Action>(Assign(myVar, Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(3, sr);

        var ff = expr.CompileFast(false, CompilerFlags.NoInvocationLambdaInlining);
        ff.PrintIL();

        // if (ff.TryGetDebugClosureNestedLambda(0, out var nested))
        //     nested.PrintIL("nested");

        var fr = ff();
        Assert.AreEqual(3, fr);
    }

    [Test]
    public void Nested_lambda_with_shared_variable()
    {
        System.Linq.Expressions.Expression<Action<Action>> invokeParamLambda = lambda => lambda();
        var aa = invokeParamLambda.FromSysExpression();
        aa.PrintCSharp();

        var myVar = Variable(typeof(int), "myVar");
        var expr = Lambda<Func<int>>(
            Block(
                new[] { myVar },
                Assign(myVar, Constant(5)),
                Invoke(aa, Lambda<Action>(Assign(myVar, Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        var sr = fs();
        Assert.AreEqual(3, sr);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Assert.AreEqual(5, fr);
    }

    public class Box<T>
    {
        public T Value;
    }

    [Test]
    public void Nested_lambda_with_shared_variable_Workaround()
    {
        System.Linq.Expressions.Expression<Action<Action>> invokeParamLambda = lambda => lambda();
        var aa = invokeParamLambda.FromSysExpression();
        aa.PrintCSharp();

        var valueField = typeof(Box<int>).GetField("Value");
        var myVar = Variable(typeof(Box<int>), "myVar");
        var expr = Lambda<Func<Box<int>>>(
            Block(
                new[] { myVar },
                Assign(myVar, MemberInit(New(typeof(Box<int>)), Bind(valueField, Constant(5)))),
                Invoke(aa, Lambda<Action>(Assign(Field(myVar, valueField), Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        var sr = fs();
        Assert.AreEqual(3, sr.Value);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Assert.AreEqual(3, fr.Value);
    }

    public class Val<T>
    {
        public T Value;
    }

    [Test]
    public void Nested_lambda_with_shared_variable_Workaround_with_struct()
    {
        System.Linq.Expressions.Expression<Action<Action>> invokeParamLambda = lambda => lambda();
        var aa = invokeParamLambda.FromSysExpression();
        aa.PrintCSharp();

        var valueField = typeof(Val<int>).GetField("Value");
        var myVar = Variable(typeof(Val<int>), "myVar");
        var expr = Lambda<Func<Val<int>>>(
            Block(
                new[] { myVar },
                Assign(myVar, MemberInit(New(typeof(Val<int>)), Bind(valueField, Constant(5)))),
                Invoke(aa, Lambda<Action>(Assign(Field(myVar, valueField), Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        var sr = fs();
        Assert.AreEqual(3, sr.Value);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Assert.AreEqual(3, fr.Value);
    }
}