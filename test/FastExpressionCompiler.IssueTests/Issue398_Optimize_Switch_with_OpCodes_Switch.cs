using System;
using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using FastExpressionCompiler.ImTools;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue398_Optimize_Switch_with_OpCodes_Switch : ITestX
{
    public void Run(TestRun t)
    {
        Test_switch_for_all_integer_types_with_cases_starting_from_0(t);
        Test_switch_for_all_integer_types_with_cases_starting_from_Not_0(t);
    }

    public void Test_switch_for_all_integer_types_with_cases_starting_from_0(TestContext t)
    {
        var p = Parameter(typeof(int));

        var expr = Lambda<Func<int, int>>(
            Switch(
                p,
                Constant(-1),
                SwitchCase(
                    Constant(0),
                    Constant(0)),
                SwitchCase(
                    Constant(1),
                    Constant(1)),
                // Adding the hole in the table to see how it handled by the generated IL
                // SwitchCase(
                //     Constant(2),
                //     Constant(2)),
                SwitchCase(
                    Constant(3),
                    Constant(3)),
                SwitchCase(
                    Constant(4),
                    Constant(4)),
                SwitchCase(
                    Constant(5),
                    Constant(5)),
                SwitchCase(
                    Constant(6),
                    Constant(6))),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        t.IsNotNull(fs);
        t.AreEqual(5, fs(5));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        // todo: @wip should be this if fs changed to ff
        fs.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Stloc_0,
            OpCodes.Ldloc_0,
            OpCodes.Switch, // (IL_0041, IL_0047, IL_0077, IL_0053, IL_0059, IL_0065, IL_0071),
            OpCodes.Br,
            OpCodes.Ldc_I4_0,
            OpCodes.Br,
            OpCodes.Ldc_I4_1,
            OpCodes.Br,
            OpCodes.Ldc_I4_3,
            OpCodes.Br,
            OpCodes.Ldc_I4_4,
            OpCodes.Br,
            OpCodes.Ldc_I4_5,
            OpCodes.Br,
            OpCodes.Ldc_I4_6,
            OpCodes.Br,
            OpCodes.Ldc_I4_M1,
            OpCodes.Ret
        );

        t.IsNotNull(ff);
        t.AreEqual(5, ff(5));
    }

    public void Test_switch_for_all_integer_types_with_cases_starting_from_Not_0(TestContext t)
    {
        var p = Parameter(typeof(int));

        var expr = Lambda<Func<int, int>>(
            Switch(
                p,
                Constant(-1),
                // Specifically starting from 1 instead of 0 to see how OpCodes.Switch handles it
                // SwitchCase(
                //     Constant(0),
                //     Constant(0)),
                SwitchCase(
                    Constant(1),
                    Constant(1)),
                // Adding the hole in the table to see how it handled by the generated IL
                // SwitchCase(
                //     Constant(2),
                //     Constant(2)),
                SwitchCase(
                    Constant(3),
                    Constant(3)),
                SwitchCase(
                    Constant(4),
                    Constant(4)),
                SwitchCase(
                    Constant(5),
                    Constant(5)),
                SwitchCase(
                    Constant(6),
                    Constant(6))),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        t.IsNotNull(fs);
        t.AreEqual(5, fs(5));

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        t.IsNotNull(ff);
        t.AreEqual(5, ff(5));
    }
}