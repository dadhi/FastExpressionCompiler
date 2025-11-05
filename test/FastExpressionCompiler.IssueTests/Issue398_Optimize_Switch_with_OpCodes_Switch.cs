using System;
using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ILDecoder;
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using FastExpressionCompiler.ILDecoder;
using FastExpressionCompiler.ImTools;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue398_Optimize_Switch_with_OpCodes_Switch : ITestX
{
    public void Run(TestRun t)
    {
        Test_switch_for_minimal_number_of_cases_enabling_OpCodesSwitch_and_no_default_case(t);
        Test_switch_for_all_integer_cases_starting_from_0(t);
        Test_switch_for_the_bytes_two_ranges(t);
        Test_switch_for_the_bytes(t);
        Test_switch_for_the_enums(t);
        Test_switch_for_integer_cases_starting_from_Not_0(t);
        Test_switch_for_nullable_integer_types(t);
    }

    public void Test_switch_for_minimal_number_of_cases_enabling_OpCodesSwitch_and_no_default_case(TestContext t)
    {
        var p = Parameter(typeof(int));

        // OpCodes.Switch will always be generated even for 2 cases. I think it maybe Jit to decide to use jump table or not.
        var expr = Lambda<Func<int, int>>(
            Block(typeof(int),
                Switch(
                    p,
                    SwitchCase(
                        Assign(p, Constant(42)),
                        Constant(0)),
                    SwitchCase(
                        Assign(p, Constant(31)),
                        Constant(3),
                        Constant(1)),
                    SwitchCase(
                        Assign(p, Constant(2)),
                        Constant(2)),
                    SwitchCase(
                        Assign(p, Constant(67)),
                        Constant(6),
                        Constant(7))
                    ),
                    p
                ),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL(format: ILFormat.AssertOpCodes);

        fs.AssertOpCodes(
           OpCodes.Ldarg_1, //        at IL_0000
           OpCodes.Stloc_0, //        at IL_0001
           OpCodes.Ldloc_0, //        at IL_0002
           OpCodes.Switch, // [IL_0045, IL_0054, IL_0063, IL_0054, IL_0075, IL_0075, IL_0071, IL_0071] at IL_0003
           OpCodes.Br, // IL_0075     at IL_0040
           OpCodes.Ldc_I4_S, // 42    at IL_0045
           OpCodes.Starg_S, // V_1    at IL_0047
           OpCodes.Br, // IL_0075     at IL_0049
           OpCodes.Ldc_I4_S, // 31    at IL_0054
           OpCodes.Starg_S, // V_1    at IL_0056
           OpCodes.Br, // IL_0075     at IL_0058
           OpCodes.Ldc_I4_2, //       at IL_0063
           OpCodes.Starg_S, // V_1    at IL_0064
           OpCodes.Br, // IL_0075     at IL_0066
           OpCodes.Ldc_I4_S, // 67    at IL_0071
           OpCodes.Starg_S, // V_1    at IL_0073
           OpCodes.Ldarg_1, //        at IL_0075
           OpCodes.Ret  //            at IL_0076
        );

        t.IsNotNull(fs);
        t.AreEqual(5, fs(5));

        var ff = expr.CompileFast();
        ff.PrintIL(format: ILFormat.AssertOpCodes | ILFormat.SkipNop);


        t.IsNotNull(ff);
        t.AreEqual(-1, ff(-1));
        t.AreEqual(31, ff(1));
        t.AreEqual(31, ff(3));
        t.AreEqual(4, ff(4));
        t.AreEqual(5, ff(5));
        t.AreEqual(67, ff(6));
        t.AreEqual(67, ff(7));
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

                // 2 cases and a single body, reordered too
                SwitchCase(
                    Constant(3),
                    Constant(4), Constant(3)),

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
        fs.PrintIL(format: ILFormat.AssertOpCodes);
        fs.AssertOpCodes(
            OpCodes.Ldarg_1, //        at IL_0000
            OpCodes.Stloc_0, //        at IL_0001
            OpCodes.Ldloc_0, //        at IL_0002
            OpCodes.Switch, // (IL_0041, IL_0047, IL_0071, IL_0053, IL_0053, IL_0065, IL_0059) at IL_0003
            OpCodes.Br, // IL_0071     at IL_0036
            OpCodes.Ldc_I4_0, //       at IL_0041
            OpCodes.Br, // IL_0072     at IL_0042
            OpCodes.Ldc_I4_1, //       at IL_0047
            OpCodes.Br, // IL_0072     at IL_0048
            OpCodes.Ldc_I4_3, //       at IL_0053
            OpCodes.Br, // IL_0072     at IL_0054
            OpCodes.Ldc_I4_6, //       at IL_0059
            OpCodes.Br, // IL_0072     at IL_0060
            OpCodes.Ldc_I4_5, //       at IL_0065
            OpCodes.Br, // IL_0072     at IL_0066
            OpCodes.Ldc_I4_M1, //      at IL_0071
            OpCodes.Ret  //            at IL_0072
        );

        t.IsNotNull(fs);
        t.AreEqual(5, fs(5));

        var ff = expr.CompileFast();
        ff.PrintIL(format: ILFormat.AssertOpCodes);


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
        fs.PrintIL(format: ILFormat.AssertOpCodes);
        fs.AssertOpCodes(
            OpCodes.Ldarg_1, //        at IL_0000
            OpCodes.Stloc_0, //        at IL_0001
            OpCodes.Ldloc_0, //        at IL_0002
            OpCodes.Ldc_I4_3, //       at IL_0003
            OpCodes.Sub, //            at IL_0004
            OpCodes.Switch, // (IL_0056, IL_0062, IL_0068, IL_0074) at IL_0005
            OpCodes.Ldloc_0, //        at IL_0026
            OpCodes.Ldc_I4_S, // 15    at IL_0027
            OpCodes.Sub, //            at IL_0029
            OpCodes.Switch, // (IL_0080, IL_0087, IL_0094, IL_0101) at IL_0030
            OpCodes.Br, // IL_0108     at IL_0051
            OpCodes.Ldc_I4_3, //       at IL_0056
            OpCodes.Br, // IL_0109     at IL_0057
            OpCodes.Ldc_I4_4, //       at IL_0062
            OpCodes.Br, // IL_0109     at IL_0063
            OpCodes.Ldc_I4_5, //       at IL_0068
            OpCodes.Br, // IL_0109     at IL_0069
            OpCodes.Ldc_I4_6, //       at IL_0074
            OpCodes.Br, // IL_0109     at IL_0075
            OpCodes.Ldc_I4_S, // 15    at IL_0080
            OpCodes.Br, // IL_0109     at IL_0082
            OpCodes.Ldc_I4_S, // 16    at IL_0087
            OpCodes.Br, // IL_0109     at IL_0089
            OpCodes.Ldc_I4_S, // 17    at IL_0094
            OpCodes.Br, // IL_0109     at IL_0096
            OpCodes.Ldc_I4_S, // 18    at IL_0101
            OpCodes.Br, // IL_0109     at IL_0103
            OpCodes.Ldc_I4_M1, //      at IL_0108
            OpCodes.Ret  //            at IL_0109
        );

        t.IsNotNull(fs);
        t.AreEqual(16, fs(16));

        var ff = expr.CompileFast();
        ff.PrintIL();

        t.IsNotNull(ff);
        t.AreEqual(16, ff(16));
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