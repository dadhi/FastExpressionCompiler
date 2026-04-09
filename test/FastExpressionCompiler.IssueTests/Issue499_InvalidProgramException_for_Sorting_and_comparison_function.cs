using System;
using System.Collections.Generic;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue499_InvalidProgramException_for_Sorting_and_comparison_function : ITestX
{
    public void Run(TestRun t)
    {
        Quicksort_partition_with_nested_loops(t);
        Comparison_function_with_goto_labels(t);
        ArrayInList_ArrayAcceesError(t);
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

        var compareToMethod = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });

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
            new[] { i, j, pivot, temp, compareResult },
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

        int[] data1 = new[] { 3, 1, 4, 1, 5 };
        int[] data2 = new[] { 3, 1, 4, 1, 5 };

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

        var compareToMethod = typeof(int).GetMethod("CompareTo", new[] { typeof(int) });

        // Mirrors makeCondition(leftIndex, rightIndex, breakExp: Return(endMain, 1), continueExp: Return(endMain, -1))
        // for INT DESC without null-handling overhead.
        //
        // The SortCompiler convention is: -1 = "continueExp" (advance cursor, left value is smaller in sort order),
        //                                  1 = "breakExp"    (stop advancing, left value is larger in sort order).
        // For DESC order the natural comparison result is negated so that:
        //   left=3, right=1 → CompareTo=1, negated=-1 → return -1  (3 sorts before 1 in DESC)
        //   left=1, right=3 → CompareTo=-1, negated=1 → return  1  (1 sorts after  3 in DESC)
        //
        // allNotNil block:
        //   compareResult = left.CompareTo(right)
        //   compareResult = -compareResult           (DESC: negate)
        //   if compareResult == -1: return -1        (continueExp: left comes first)
        //   (fall through to breakExp)
        // breakExp (fallthrough): return 1
        // Label: endMain default -1
        var body = Block(
            new[] { compareResult },
            Assign(compareResult, Call(left, compareToMethod, right)),
            Assign(compareResult, Negate(compareResult)),
            IfThen(Equal(compareResult, Constant(-1)), Return(endMain, Constant(-1))),
            Return(endMain, Constant(1)),
            Label(endMain, Constant(-1)));

        var expr = Lambda<Func<int, int, int>>(body, left, right);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual(-1, fs(3, 1)); // 3 > 1 in DESC → left(3) comes first → -1
        t.AreEqual(1, fs(1, 3));  // 1 < 3 in DESC → left(1) comes after  → +1

        var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        t.IsNotNull(ff);
        ff.PrintIL();
        t.AreEqual(-1, ff(3, 1));
        t.AreEqual(1, ff(1, 3));
    }

    /// <summary>
    /// ff raise System.InvalidProgramException: Common Language Runtime detected an invalid program.
    /// </summary>
    /// <param name="t"></param>
    public void ArrayInList_ArrayAcceesError(TestContext t)
    {
        List<Expression> exps = new List<Expression>();
        var dataArrayList = Parameter(typeof(List<int?[]>), "dataArrayList");
        List<ParameterExpression> vars = new List<ParameterExpression>();
        var left_ListIndex = Parameter(typeof(int), "left_ListIndex");
        var left_ArrayIndex = Parameter(typeof(int), "left_ArrayIndex");

        vars.AddRange(new ParameterExpression[] { left_ListIndex, left_ArrayIndex });
        var leftVars = new ParameterExpression[1];
        leftVars[0] = Parameter(typeof(int?), $"left_{0}");
        vars.Add(leftVars[0]);
        exps.Add(Assign(left_ListIndex, Constant(0)));
        exps.Add(Assign(left_ArrayIndex, Constant(0)));
        exps.Add(Assign(leftVars[0], ArrayAccess(Expression.Property(dataArrayList, "Item", left_ListIndex), left_ArrayIndex)));

        BlockExpression block = Block(
            vars.ToArray(), exps
        );
        var expr = Lambda<Action<List<int?[]>>>(block, dataArrayList);
        expr.PrintCSharp();

        List<int?[]> data1 = new List<int?[]> { new int?[] { 1 } };
        List<int?[]> data2 = new List<int?[]> { new int?[] { 1 } };


        var fs = expr.CompileSys();
        fs.PrintIL();
        fs(data1);

        var ff = expr.CompileFast(ifFastFailedReturnNull: true);
        t.IsNotNull(ff);
        ff.PrintIL();
        ff(data2);
    }
}
