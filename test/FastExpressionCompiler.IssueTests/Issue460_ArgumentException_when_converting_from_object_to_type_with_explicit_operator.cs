using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue460_ArgumentException_when_converting_from_object_to_type_with_explicit_operator : ITest
{
    public int Run()
    {
        Original_case1();
        return 1;
    }

    private sealed class TestClass()
    {
        public TestClass2 ClassProp { get; set; }
    }

    private sealed class TestClass2()
    {
        public static explicit operator TestClass2(int value)
            => new();
    }

    public void Original_case1()
    {
        var setPropMethod = typeof(TestClass).GetProperty(nameof(TestClass.ClassProp)).SetMethod;
        var valueParamType = setPropMethod.GetParameters()[0].ParameterType;

        var instanceParam = Parameter(typeof(object), "instance");
        var valueParam = Parameter(typeof(object), "parameter");

        var call = Call(
            Convert(instanceParam, typeof(TestClass)),
            setPropMethod,
            Convert(valueParam, valueParamType));

        var expr = Lambda<Action<object, object>>(call, instanceParam, valueParam);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        fs(new TestClass(), new TestClass2());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        fs(new TestClass(), new TestClass2());
    }
}
