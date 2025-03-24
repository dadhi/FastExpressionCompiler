using System;
using System.Linq.Expressions;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif


public class Issue437_Shared_variables_with_nested_lambdas_returning_incorrect_values : ITest
{
    public int Run()
    {
        More_simplified_test_no_inlining_for_SystemCompile_with_Execute_no_assigning();

        More_simplified_test_no_inlining_for_SystemCompile_with_Execute();
        More_simplified_test_no_inlining();
        Simplified_test_no_inlining();
        Simplified_test();

        Nested_lambda_with_shared_variable_Workaround_with_struct();
        Nested_lambda_with_shared_variable();
        Nested_lambda_with_shared_variable_Workaround();

        return 8;
    }

    public class TestClass
    {
        public static void ExecuteAction(Action action) => action();
        public static int ExecuteFunc(Func<int> func) => func();
    }


    public void More_simplified_test_no_inlining_for_SystemCompile_with_Execute_no_assigning()
    {
        var execute = typeof(TestClass).GetMethod(nameof(TestClass.ExecuteFunc));

        var myVar = Variable(typeof(int), "myVar");
        var expr = Lambda<Func<int>>(
            Block(
                new[] { myVar },
                Assign(myVar, Constant(5)),
                Call(execute, Lambda<Func<int>>(Add(myVar, Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();
        // outputs:
        var @cs = (Func<int>)(() => //int
        {
            int myVar = default;
            myVar = 5;
            TestClass.ExecuteFunc((Func<int>)(() => //int
                myVar + 3));
            return myVar;
        });
        Asserts.AreEqual(5, @cs());

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(5, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(5, fr);
    }


    public void More_simplified_test_no_inlining_for_SystemCompile_with_Execute()
    {
        var execute = typeof(TestClass).GetMethod(nameof(TestClass.ExecuteAction));

        var myVar = Variable(typeof(int), "myVar");
        var expr = Lambda<Func<int>>(
            Block(
                new[] { myVar },
                Call(execute, Lambda<Action>(Assign(myVar, Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(3, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(3, fr);
    }


    public void More_simplified_test_no_inlining()
    {
        var myVar = Variable(typeof(int), "myVar");
        var expr = Lambda<Func<int>>(
            Block(
                new[] { myVar },
                Invoke(Lambda<Action>(Assign(myVar, Constant(3)))),
                myVar
            )
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(3, sr);

        var ff = expr.CompileFast(false, CompilerFlags.NoInvocationLambdaInlining);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(3, fr);
    }


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
        Asserts.AreEqual(3, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(3, fr);
    }


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
        Asserts.AreEqual(3, sr);

        var ff = expr.CompileFast(false, CompilerFlags.NoInvocationLambdaInlining);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(3, fr);
    }


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
        Asserts.AreEqual(3, sr);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Asserts.AreEqual(3, fr);
    }

    public class Box<T>
    {
        public T Value;
    }


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
        Asserts.AreEqual(3, sr.Value);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Asserts.AreEqual(3, fr.Value);
    }

    public class Val<T>
    {
        public T Value;
    }


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
        Asserts.AreEqual(3, sr.Value);

        var ff = expr.CompileFast(false, CompilerFlags.ThrowOnNotSupportedExpression);
        var fr = ff();
        Asserts.AreEqual(3, fr.Value);
    }
}