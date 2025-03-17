using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue441_Fails_to_pass_Constant_as_call_parameter_by_reference : ITest
{
    public int Run()
    {
        Original_case();
        Case_with_string();
        Case_with_nullable();
        return 3;
    }

    public class TestClass
    {
        public static int MethodThatTakesARefInt(ref int i) => i + 1;
        public static int MethodThatTakesARefString(ref string s) => s.Length;
        public static int MethodThatTakesARefNullable(ref int? s) => s ?? 2;
    }

    [Test]
    public void Original_case()
    {
        var callRefMethod = typeof(TestClass).GetMethod(nameof(TestClass.MethodThatTakesARefInt));

        var block = Block(
            typeof(int),
            Call(callRefMethod, Constant(42))
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(43, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(43, fr);
    }

    [Test]
    public void Case_with_string()
    {
        var callRefMethod = typeof(TestClass).GetMethod(nameof(TestClass.MethodThatTakesARefString));

        var block = Block(
            typeof(int),
            Call(callRefMethod, Constant("42"))
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

    [Test]
    public void Case_with_nullable()
    {
        var callRefMethod = typeof(TestClass).GetMethod(nameof(TestClass.MethodThatTakesARefNullable));

        var block = Block(
            typeof(int),
            Call(callRefMethod, Constant(null, typeof(int?)))
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
}