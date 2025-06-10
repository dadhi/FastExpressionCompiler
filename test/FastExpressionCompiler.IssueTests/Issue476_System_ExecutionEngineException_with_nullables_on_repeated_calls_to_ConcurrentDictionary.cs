using System;


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

public struct Issue476_System_ExecutionEngineException_with_nullables_on_repeated_calls_to_ConcurrentDictionary : ITestX
{
    public void Run(TestRun t)
    {
        TestSmallMap_Lookup_SIMD(t);
        TestSmallList(t);
        Original_case(t);
    }

    public class Record
    {
        public DateTimeOffset? Timestamp { get; set; }
    }

    public void Original_case(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<Record, bool>> sExpr = record => record.Timestamp != null;
        var expr = sExpr.FromSysExpression();

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var currTime = DateTimeOffset.UtcNow;
        var notNull = new Record() { Timestamp = currTime };
        var aNull = new Record() { Timestamp = null };

        t.IsTrue(fs(notNull));
        t.IsFalse(fs(aNull));

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        t.IsTrue(ff(notNull));
        t.IsFalse(ff(aNull));
    }

    SmallList<int, Stack4<int>> _smallList;

    public void TestSmallList(TestContext t)
    {
        for (var i = 0; i < 8; ++i)
            _smallList.Add(i);

        var doubleSum = 0;
        foreach (var n in _smallList)
            doubleSum += n + n;

        t.AreEqual(56, doubleSum);
    }

    public void TestSmallMap_Lookup_SIMD(TestContext t)
    {
        Stack8<int> hashes = default;
        Stack8<SmallMap.Entry<int>> entries = default;

        for (var n = 0; n < 8; ++n)
        {
            hashes.GetSurePresentItemRef(n) = default(IntEq).GetHashCode(n);
            entries.GetSurePresentItemRef(n) = new SmallMap.Entry<int>(n);
        }

        var sum = 0;
        for (var i = 12; i >= -4; --i)
        {
            ref var e = ref entries.TryGetEntryRef(
                ref hashes, 8, i, out var found,
                default(IntEq), default(Size8), default(Use<SmallMap.Entry<int>>));
            if (found)
                sum += e.Key;
        }

        t.AreEqual(28, sum);
    }
}