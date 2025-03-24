using System;
using System.Reflection.Emit;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif


public class Issue426_Directly_passing_a_method_result_to_another_method_by_ref_fails_with_InvalidProgramException : ITest
{
    public int Run()
    {
        Two_ref_value_params_case();
        Original_case();
        Class_ref_param_case();
        return 3;
    }

    public static class Numbers
    {
        public static int GetInt() => 40;
        public static int AddTwo(ref int value) => value + 2;
        public static int AddTwoTwo(ref int value, ref int value2) => value + value2;
        public static string AckMessage(ref Message msg)
        {
            msg = new Message { Data = msg.Data + "Ack" };
            return msg.Data;
        }
        public static Message GetMessage() => new Message { Data = "Duck" };
    }

    public class Message
    {
        public string Data;
    }


    public void Original_case()
    {
        var getMethod = typeof(Numbers).GetMethod(nameof(Numbers.GetInt));
        var addMethod = typeof(Numbers).GetMethod(nameof(Numbers.AddTwo));

        var expr = Lambda<Func<int>>(Call(addMethod, Call(getMethod)));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Call,
            OpCodes.Stloc_0,
            OpCodes.Ldloca_S,
            OpCodes.Call,
            OpCodes.Ret
        );

        Asserts.AreEqual(42, fs());
        Asserts.AreEqual(42, ff());
    }


    public void Class_ref_param_case()
    {
        var getMethod = typeof(Numbers).GetMethod(nameof(Numbers.GetMessage));
        var addMethod = typeof(Numbers).GetMethod(nameof(Numbers.AckMessage));

        var expr = Lambda<Func<string>>(Call(addMethod, Call(getMethod)));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Call,
            OpCodes.Stloc_0,
            OpCodes.Ldloca_S, //0
            OpCodes.Call,
            OpCodes.Ret
        );

        Asserts.AreEqual("DuckAck", fs());
        Asserts.AreEqual("DuckAck", ff());
    }


    public void Two_ref_value_params_case()
    {
        var getMethod = typeof(Numbers).GetMethod(nameof(Numbers.GetInt));
        var addMethod = typeof(Numbers).GetMethod(nameof(Numbers.AddTwoTwo));

        var expr = Lambda<Func<int>>(Call(addMethod, Call(getMethod), Call(getMethod)));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Call,
            OpCodes.Stloc_0,
            OpCodes.Ldloca_S, //0
            OpCodes.Call,
            OpCodes.Stloc_1,
            OpCodes.Ldloca_S, //1
            OpCodes.Call,
            OpCodes.Ret
        );

        Asserts.AreEqual(80, fs());
        Asserts.AreEqual(80, ff());
    }
}