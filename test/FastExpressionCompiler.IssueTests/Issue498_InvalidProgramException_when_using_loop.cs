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
        Quicksort_partition_with_nested_loops(t);
        Comparison_function_with_goto_labels(t);
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
           OpCodes.Br, // IL_0000     at IL_0027
           OpCodes.Br, // IL_0000     at IL_0032
           OpCodes.Ret  //            at IL_0037
        );

        calResult = ff();
        t.AreEqual(10, calResult);
    }

    // Reproduces the sorting use-case from https://github.com/dadhi/FastExpressionCompiler/issues/499
    // using the same nested-loop pattern as SortCompiler.Compile but minimised to a plain int[].
    // The outer loop drives a quicksort-style partition; two inner loops advance the i/j cursors.
    public void Quicksort_partition_with_nested_loops(TestContext t)
    {
        var arr = Parameter(typeof(int[]), "arr");
        var low = Parameter(typeof(int), "low");
        var high = Parameter(typeof(int), "high");

        var i = Variable(typeof(int), "i");
        var j = Variable(typeof(int), "j");
        var pivot = Variable(typeof(int), "pivot");
        var temp = Variable(typeof(int), "temp");
        var compareResult = Variable(typeof(int), "compareResult");

        var endMain = Label(typeof(void), "endMain");
        var endSub1 = Label(typeof(void), "endSub1");
        var endSub2 = Label(typeof(void), "endSub2");
        var continue1 = Label(typeof(void), "continue1");
        var continue2 = Label(typeof(void), "continue2");

        var compareToMethod = typeof(int).GetMethod("CompareTo", [typeof(int)]);

        // Inner loop 1: while arr[i] < pivot { i++ }
        // Matches the makeCondition(rowNumbers[i], pivot, Break(endSub1), Block(i++, Continue(continue1))) pattern.
        var innerLoop1 = Loop(
            Block(
                Assign(compareResult, Call(ArrayAccess(arr, i), compareToMethod, pivot)),
                IfThen(Equal(compareResult, Constant(-1)),
                    Block(PostIncrementAssign(i), Continue(continue1))),
                Break(endSub1)
            ),
            endSub1, continue1);

        // Inner loop 2: while arr[j] > pivot { j-- }
        // Matches the makeCondition(pivot, rowNumbers[j], Break(endSub2), Block(j--, Continue(continue2))) pattern.
        var innerLoop2 = Loop(
            Block(
                Assign(compareResult, Call(pivot, compareToMethod, ArrayAccess(arr, j))),
                IfThen(Equal(compareResult, Constant(-1)),
                    Block(PostDecrementAssign(j), Continue(continue2))),
                Break(endSub2)
            ),
            endSub2, continue2);

        var body = Block(
            [i, j, pivot, temp, compareResult],
            Assign(i, low),
            Assign(j, high),
            Assign(pivot, ArrayAccess(arr, Divide(Add(i, j), Constant(2)))),
            Loop(
                Block(
                    IfThen(GreaterThan(i, j), Break(endMain)),
                    innerLoop1,
                    innerLoop2,
                    IfThen(LessThanOrEqual(i, j),
                        Block(
                            Assign(temp, ArrayAccess(arr, i)),
                            Assign(ArrayAccess(arr, i), ArrayAccess(arr, j)),
                            Assign(ArrayAccess(arr, j), temp),
                            PostIncrementAssign(i),
                            PostDecrementAssign(j)))
                ),
                endMain));

        var expr = Lambda<Action<int[], int, int>>(body, arr, low, high);
        expr.PrintCSharp();

        int[] data1 = [3, 1, 4, 1, 5];
        int[] data2 = [3, 1, 4, 1, 5];

        var fs = expr.CompileSys();
        fs.PrintIL();
        fs(data1, 0, data1.Length - 1);

        var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        t.IsNotNull(ff);
        ff.PrintIL();
        ff(data2, 0, data2.Length - 1);

        t.AreEqual(data1, data2);
    }

    // Reproduces the comparison-function use-case from https://github.com/dadhi/FastExpressionCompiler/issues/499
    // using the same Return(label, value) / goto pattern as SortCompiler.CompileComparisonFunction
    // but minimised to a plain int comparison (DESC order, like the original failing test).
    public void Comparison_function_with_goto_labels(TestContext t)
    {
        var left = Parameter(typeof(int), "left");
        var right = Parameter(typeof(int), "right");
        var compareResult = Variable(typeof(int), "compareResult");

        var endMain = Label(typeof(int), "endMain");

        var compareToMethod = typeof(int).GetMethod("CompareTo", [typeof(int)]);

        // Mirrors makeCondition(leftIndex, rightIndex, Return(endMain,1), Return(endMain,-1))
        // for INT DESC without null-handling overhead.
        // allNotNil block:
        //   compareResult = left.CompareTo(right)
        //   compareResult = -compareResult           (DESC: negate)
        //   if compareResult == -1: return -1        (continueExp)
        //   (fall through to breakExp)
        // breakExp (fallthrough): return 1
        // Label: endMain default -1
        var body = Block(
            [compareResult],
            Assign(compareResult, Call(left, compareToMethod, right)),
            Assign(compareResult, Negate(compareResult)),
            IfThen(Equal(compareResult, Constant(-1)), Return(endMain, Constant(-1))),
            Return(endMain, Constant(1)),
            Label(endMain, Constant(-1)));

        var expr = Lambda<Func<int, int, int>>(body, left, right);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual(-1, fs(3, 1)); // DESC: 3 > 1 means left should come first → result -1
        t.AreEqual(1, fs(1, 3));  // DESC: 1 < 3 means left should come after  → result  1

        var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        t.IsNotNull(ff);
        ff.PrintIL();
        t.AreEqual(-1, ff(3, 1));
        t.AreEqual(1, ff(1, 3));
    }
}