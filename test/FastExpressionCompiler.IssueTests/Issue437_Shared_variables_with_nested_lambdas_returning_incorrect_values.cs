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
        // Nested_lambda_with_shared_variable(); // todo: @fixme
        Nested_lambda_with_shared_variable_Workaround();
        return 1;
    }

    public static void DoSome() { }

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
                Invoke(aa,
                    Lambda<Action>(Assign(myVar, Constant(3)))
                ),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        var sr = fs();
        Assert.AreEqual(3, sr);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Assert.AreEqual(5, sr);

        Assert.AreEqual(sr, fr);
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
                Invoke(aa,
                    Lambda<Action>(Assign(Field(myVar, valueField), Constant(3)))
                ),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        var sr = fs();
        Assert.AreEqual(3, sr.Value);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Assert.AreEqual(3, sr.Value);

        Assert.AreEqual(sr.Value, fr.Value);
    }
}