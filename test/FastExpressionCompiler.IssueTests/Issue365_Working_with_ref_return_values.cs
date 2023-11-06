using System;
using System.Reflection.Emit;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue365_Working_with_ref_return_values : ITest
{
    public int Run()
    {
        Test1();
        return 1;
    }

    [Test]
    public void Test1()
    {
        var getParamValueByRefMethod = typeof(ParamProcessor).GetMethod(nameof(ParamProcessor.GetParamValueByRef));
        var valueProperty = typeof(ParamValue).GetProperty(nameof(ParamValue.Value));

        var pp = Parameter(typeof(ParamProcessor), "pp");
        var e = Lambda<Action<ParamProcessor>>(
            Assign(
                MakeMemberAccess(Call(pp, getParamValueByRefMethod), valueProperty), 
                Constant(7)),
            pp);

        e.PrintCSharp();
        var @cs = (Action<ParamProcessor>)((ParamProcessor pp) =>
        {
            pp.GetParamValueByRef().Value = 7;
        });

        var paramValue = new ParamValue() { Value = 5 };
        var paramProcessor = new ParamProcessor(paramValue);
        @cs(paramProcessor);
        Assert.AreEqual(7, paramProcessor.ParamValue.Value);

        // var fs = e.CompileSys(); // todo: @wip does not conver ref returning method calls, cause unable cannot find the property on the T& type
        var ff = e.CompileFast(true);
        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call, // ParamValue& GetParamValueByRef()
            OpCodes.Ldc_I4_7,
            OpCodes.Call, // Void set_Value(Int32)
            OpCodes.Ret
        );

        paramValue = new ParamValue() { Value = 5 };
        paramProcessor = new ParamProcessor(paramValue);
        ff(paramProcessor);

        Assert.AreEqual(7, paramProcessor.ParamValue.Value);
    }

    public struct ParamValue
    {
        public int Value { get; set; }
    }

    public class ParamProcessor
    {
        public ParamValue ParamValue;

        public ParamProcessor(ParamValue paramValue) => ParamValue = paramValue;

        public ref ParamValue GetParamValueByRef() => ref ParamValue;
    }
}
