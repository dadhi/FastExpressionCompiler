using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct IndexerExceptionSpillRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Try_indices_preserve_reference_indexer_assignment_and_read(t);
        Struct_indexer_receivers_remain_references_across_try_indices(t);
        Try_indices_preserve_array_assignment_and_read(t);
    }

    public sealed class Box
    {
        public readonly int[] Values = new int[4];
        public int Gets, Sets;
        public int this[int index]
        {
            get { ++Gets; return Values[index]; }
            set { ++Sets; Values[index] = value; }
        }
    }

    public void Try_indices_preserve_reference_indexer_assignment_and_read(TestContext t)
    {
        var factory = SysExpr.Parameter(typeof(Func<Box>));
        var indexFactory = SysExpr.Parameter(typeof(Func<int>));
        var finallyAction = SysExpr.Parameter(typeof(Action));
        var index = SysExpr.MakeIndex(SysExpr.Invoke(factory), typeof(Box).GetProperty("Item"), new[] {
            SysExpr.TryFinally(SysExpr.Invoke(indexFactory), SysExpr.Invoke(finallyAction)) });
        var expression = SysExpr.Lambda<Func<Func<Box>, Func<int>, Action, bool>>(
            SysExpr.Block(SysExpr.Assign(index, SysExpr.Constant(123)), SysExpr.Equal(index, SysExpr.Constant(123))),
            factory, indexFactory, finallyAction);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var box = new Box();
        var receiverCalls = 0;
        var indexCalls = 0;
        var finallyCalls = 0;
        t.IsTrue(fast(() => { ++receiverCalls; return box; }, () => { ++indexCalls; return 1; }, () => ++finallyCalls));
        t.AreEqual(2, receiverCalls);
        t.AreEqual(2, indexCalls);
        t.AreEqual(2, finallyCalls);
        t.AreEqual(1, box.Sets);
        t.AreEqual(1, box.Gets);
        t.AreEqual(123, box.Values[1]);
        var expectedBox = new Box();
        t.IsTrue(expression.Compile()(() => expectedBox, () => 1, () => { }));
        t.AreEqual(expectedBox.Values[1], box.Values[1]);
    }

    public struct ValueBox
    {
        public int Value;
        public int this[int index]
        {
            get { Value += index; return Value; }
            set { Value = value - index; }
        }
    }
    public delegate int RefIndexed(ref ValueBox box);

    public void Struct_indexer_receivers_remain_references_across_try_indices(TestContext t)
    {
        var box = SysExpr.Parameter(typeof(ValueBox).MakeByRefType());
        var index = SysExpr.MakeIndex(box, typeof(ValueBox).GetProperty("Item"), new[] {
            SysExpr.TryFinally(SysExpr.Constant(1), SysExpr.Empty()) });
        var expression = SysExpr.Lambda<RefIndexed>(SysExpr.Block(SysExpr.Assign(index, SysExpr.Constant(123)), index), box);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var actual = new ValueBox();
        var expected = new ValueBox();
        t.AreEqual(123, fast(ref actual));
        t.AreEqual(123, expression.Compile()(ref expected));
        t.AreEqual(expected.Value, actual.Value);
        t.AreEqual(123, actual.Value);
    }

    public void Try_indices_preserve_array_assignment_and_read(TestContext t)
    {
        var array = SysExpr.Parameter(typeof(int[]));
        var index = SysExpr.ArrayAccess(array, SysExpr.TryFinally(SysExpr.Constant(1), SysExpr.Empty()));
        var expression = SysExpr.Lambda<Func<int[], int>>(SysExpr.Block(SysExpr.Assign(index, SysExpr.Constant(123)), index), array);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var actual = new int[2];
        var expected = new int[2];
        t.AreEqual(123, fast(actual));
        t.AreEqual(expression.Compile()(expected), actual[1]);
        t.AreEqual(expected[1], actual[1]);
        t.Throws<IndexOutOfRangeException>(() => fast(new int[1]));
        t.Throws<NullReferenceException>(() => fast(null));
    }
}
