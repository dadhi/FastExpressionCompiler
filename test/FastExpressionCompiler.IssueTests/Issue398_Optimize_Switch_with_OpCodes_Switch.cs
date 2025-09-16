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
        Test_switch_for_the_bytes_two_ranges(t);
        Test_switch_for_the_bytes(t);
        Test_switch_for_the_enums(t);
        Test_switch_for_all_integer_cases_starting_from_0(t);
        Test_switch_for_integer_cases_starting_from_Not_0(t);
        Test_switch_for_nullable_integer_types(t);
    }

    public void Test_switch_for_all_integer_cases_starting_from_0(TestContext t)
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

                // 2 cases and a single body, reordered two
                SwitchCase(
                    Constant(3),
                    Constant(4),
                    Constant(3)),

                // Reordered cases
                SwitchCase(
                    Constant(6),
                    Constant(6)),
                SwitchCase(
                    Constant(5),
                    Constant(5))),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        t.IsNotNull(fs);
        t.AreEqual(5, fs(5));

        var ff = expr.CompileFast();
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
            OpCodes.Ldc_I4_6,
            OpCodes.Br,
            OpCodes.Ldc_I4_5,
            OpCodes.Br,
            OpCodes.Ldc_I4_M1,
            OpCodes.Ret
        );

        t.IsNotNull(ff);
        t.AreEqual(5, ff(5));
    }

    public void Test_switch_for_integer_cases_starting_from_Not_0(TestContext t)
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

        var ff = expr.CompileFast();
        ff.PrintIL();

        t.IsNotNull(ff);
        t.AreEqual(5, ff(5));
    }

    enum IntEnum : int
    {
        Zero = 0,
        One,
        Two,
        Three,
        Four,
        Five,
        Six
    }

    public void Test_switch_for_the_enums(TestContext t)
    {
        var p = Parameter(typeof(IntEnum));

        var expr = Lambda<Func<IntEnum, int>>(
            Switch(
                p,
                Constant(-1),
                // Specifically starting from 1 instead of 0 to see how OpCodes.Switch handles it
                // SwitchCase(
                //     Constant(0),
                //     Constant(0)),
                SwitchCase(
                    Constant(1),
                    Constant(IntEnum.One)),
                // Adding the hole in the table to see how it handled by the generated IL
                // SwitchCase(
                //     Constant(2),
                //     Constant(2)),
                SwitchCase(
                    Constant(3),
                    Constant(IntEnum.Three)),
                SwitchCase(
                    Constant(4),
                    Constant(IntEnum.Four)),
                SwitchCase(
                    Constant(5),
                    Constant(IntEnum.Five)),
                SwitchCase(
                    Constant(6),
                    Constant(IntEnum.Six))),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        t.IsNotNull(fs);
        t.AreEqual(5, fs(IntEnum.Five));

        var ff = expr.CompileFast();
        ff.PrintIL();

        t.IsNotNull(ff);
        t.AreEqual(5, ff(IntEnum.Five));
    }

    public void Test_switch_for_the_bytes(TestContext t)
    {
        var p = Parameter(typeof(sbyte));

        var expr = Lambda<Func<sbyte, int>>(
            Switch(
                p,
                Constant(-1),
                // The -3 case is handled separately before the switch table, but -2 is included into the switch table
                SwitchCase(
                    Constant(-3),
                    Constant((sbyte)-3, typeof(sbyte))),
                SwitchCase(
                    Constant(3),
                    Constant((sbyte)3, typeof(sbyte))),
                SwitchCase(
                    Constant(4),
                    Constant((sbyte)4, typeof(sbyte))),
                SwitchCase(
                    Constant(5),
                    Constant((sbyte)5, typeof(sbyte))),
                SwitchCase(
                    Constant(6),
                    Constant((sbyte)6, typeof(sbyte))),
                SwitchCase(
                    Constant(12),
                    Constant((sbyte)12, typeof(sbyte)))
                ),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        t.IsNotNull(fs);
        t.AreEqual(-3, fs(-3));

        var ff = expr.CompileFast();
        ff.PrintIL();

        t.IsNotNull(ff);
        t.AreEqual(-3, ff(-3));
    }

    public void Test_switch_for_the_bytes_two_ranges(TestContext t)
    {
        var p = Parameter(typeof(sbyte));

        var expr = Lambda<Func<sbyte, int>>(
            Switch(
                p,
                Constant(-1),
                SwitchCase(
                    Constant(3),
                    Constant((sbyte)3, typeof(sbyte))),
                SwitchCase(
                    Constant(4),
                    Constant((sbyte)4, typeof(sbyte))),
                SwitchCase(
                    Constant(5),
                    Constant((sbyte)5, typeof(sbyte))),
                SwitchCase(
                    Constant(6),
                    Constant((sbyte)6, typeof(sbyte))),
                SwitchCase(
                    Constant(15), // boundary case to split single range into 2 ranges (seems like the diff should be >= range1 + range2)
                    Constant((sbyte)15, typeof(sbyte))),
                SwitchCase(
                    Constant(16),
                    Constant((sbyte)16, typeof(sbyte))),
                SwitchCase(
                    Constant(17),
                    Constant((sbyte)17, typeof(sbyte))),
                SwitchCase(
                    Constant(18),
                    Constant((sbyte)18, typeof(sbyte)))
                ),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        t.IsNotNull(fs);
        t.AreEqual(13, fs(13));

        var ff = expr.CompileFast();
        ff.PrintIL();

        t.IsNotNull(ff);
        t.AreEqual(13, ff(13));
    }

    public void Test_switch_for_nullable_integer_types(TestContext t)
    {
        var p = Parameter(typeof(int?));

        var expr = Lambda<Func<int?, int>>(
            Switch(
                p,
                Constant(-1),
                SwitchCase(
                    Constant(0),
                    Constant(null, typeof(int?))),
                SwitchCase(
                    Constant(0),
                    Constant(0, typeof(int?))),
                SwitchCase(
                    Constant(1),
                    Constant(1, typeof(int?))),
                SwitchCase(
                    Constant(2),
                    Constant(2, typeof(int?))),
                SwitchCase(
                    Constant(3),
                    Constant(3, typeof(int?))),
                SwitchCase(
                    Constant(4),
                    Constant(4, typeof(int?))),
                SwitchCase(
                    Constant(5),
                    Constant(5, typeof(int?)))),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        t.IsNotNull(fs);
        t.AreEqual(0, fs(null));

        var ff = expr.CompileFast();
        ff.PrintIL();

        t.IsNotNull(ff);
        t.AreEqual(0, ff(null));
    }
}