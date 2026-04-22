using System;
using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue498_InvalidProgramException_when_using_loop : ITestX
{
    public void Run(TestRun t)
    {
        Original_test(t);
    }

    public void Original_test(TestContext t)
    {
        // Creating a label that represents the return value
        var result = Variable(typeof(int), "result");
        var x = Variable(typeof(int), "x");

        var endMain = Label(typeof(int), "endMain");
        var endSub1 = Label(typeof(void), "endSub1");
        var continue1 = Label(typeof(void), "continue1");

        var loopExpression = Loop(
            Block(
                Loop(
                    Block(
                        IfThen(GreaterThanOrEqual(x, Constant(10)),
                            Break(endMain, result)),
                        AddAssign(result, Constant(1)),
                        AddAssign(x, Constant(1))
                        ),
                    endSub1,
                    continue1
                    )
                ),
            endMain
        );

        var body = Block([result, x], loopExpression);
        var expr = Lambda<Func<int>>(body);
        expr.PrintCSharp();

        var _ = (Func<int>)(() => //int
        {
            int result = default;
            int x = default;
            while (true)
            {
                while (true)
                {
                continue1:;
                    if (x >= 10)
                    {
                        return result;
                    }
                    result += 1;
                    x += 1;
                }
            endSub1:;
            }
        endMain:;
        });

        var fs = expr.CompileSys();
        fs.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        var calResult = fs();
        t.AreEqual(10, calResult);

        // Act: CompileFast should throw NotSupportedExpressionException or return null
        var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        t.IsNotNull(ff);
        ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);

        ff.AssertOpCodes(
           OpCodes.Ldloc_1, //        at IL_0000
           OpCodes.Ldc_I4_S, // 10    at IL_0001
           OpCodes.Clt, //            at IL_0003
           OpCodes.Ldc_I4_0, //       at IL_0005
           OpCodes.Ceq, //            at IL_0006
           OpCodes.Brfalse, // IL_0019at IL_0008
           OpCodes.Ldloc_0, //        at IL_0013
           OpCodes.Br, // IL_0037     at IL_0014
           OpCodes.Ldloc_0, //        at IL_0019
           OpCodes.Ldc_I4_1, //       at IL_0020
           OpCodes.Add, //            at IL_0021
           OpCodes.Stloc_0, //        at IL_0022
           OpCodes.Ldloc_1, //        at IL_0023
           OpCodes.Ldc_I4_1, //       at IL_0024
           OpCodes.Add, //            at IL_0025
           OpCodes.Stloc_1, //        at IL_0026
           OpCodes.Br_S, // IL_0000   at IL_0027 (short backward branch: fits in sbyte)
           OpCodes.Br_S, // IL_0000   at IL_002A (short backward branch: fits in sbyte)
           OpCodes.Ret  //            at IL_002C
        );

        calResult = ff();
        t.AreEqual(10, calResult);
    }
}