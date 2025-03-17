#if LIGHT_EXPRESSION
using System;
using System.Reflection.Emit;
using NUnit.Framework;

using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;

[TestFixture]
public class Issue365_Working_with_ref_return_values : ITest
{
    public int Run()
    {
        Test_access_ref_returning_method_assigned_var_then_property();
        Test_access_ref_returning_method_then_property();
        return 2;
    }

    [Test]
    public void Test_access_ref_returning_method_assigned_var_then_property()
    {
        var getParamValueByRefMethod = typeof(ParamProcessor).GetMethod(nameof(ParamProcessor.GetParamValueByRef));
        var valueProperty = typeof(ParamValue).GetProperty(nameof(ParamValue.Value));
        var varByRef = Variable(typeof(ParamValue).MakeByRefType(), "varByRef");

        var pp = Parameter(typeof(ParamProcessor), "pp");
        var e = Lambda<Action<ParamProcessor>>(
            Block(new[] { varByRef },
                Assign(varByRef, Call(pp, getParamValueByRefMethod)),
                Assign(MakeMemberAccess(varByRef, valueProperty), Constant(8))),
            pp);

        e.PrintCSharp();
        var @cs = (Action<ParamProcessor>)((ParamProcessor pp) =>
        {
            // ParamValue varByRef__discard_init_by_ref = default; ref var varByRef = ref varByRef__discard_init_by_ref;
            ref var varByRef = ref pp.GetParamValueByRef();
            varByRef.Value = 8;
        });
        @cs.PrintIL();

        var paramValue = new ParamValue() { Value = 5 };
        var paramProcessor = new ParamProcessor(paramValue);
        @cs(paramProcessor);
        Asserts.AreEqual(8, paramProcessor.ParamValue.Value);

        // var fs = e.CompileSys(); // todo: does not convert ref returning method calls, cause unable to find the property on the T& type

        var ff = e.CompileFast(true);
        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call, // ParamValue& GetParamValueByRef()
            OpCodes.Stloc_0,
            OpCodes.Ldloc_0,
            OpCodes.Ldc_I4_8,
            OpCodes.Call, // Void set_Value(Int32)
            OpCodes.Ret
        );

        paramValue = new ParamValue() { Value = 5 };
        paramProcessor = new ParamProcessor(paramValue);
        ff(paramProcessor);
        Asserts.AreEqual(8, paramProcessor.ParamValue.Value);
    }

    [Test]
    public void Test_access_ref_returning_method_then_property()
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
        Asserts.AreEqual(7, paramProcessor.ParamValue.Value);

        // var fs = e.CompileSys(); // todo: does not conver ref returning method calls, cause unable cannot find the property on the T& type

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
        Asserts.AreEqual(7, paramProcessor.ParamValue.Value);
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
#endif