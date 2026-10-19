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
        SmallMap16_promotes_and_resizes(t, false);
        SmallMap16_promotes_and_resizes(t, true);
        SmallMap8_retains_initial_hash_capacity(t);
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

    SmallList<int, Stack4<int>, NoArrayPool<int>> _smallList;

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
            hashes.GetSurePresentRef(n) = default(IntEq).GetHashCode(n);
            entries.GetSurePresentRef(n) = new SmallMap.Entry<int>(n);
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

    public void SmallMap16_promotes_and_resizes(TestContext t, bool sureAbsent)
    {
        SmallMap16<int, int, IntEq> map = default;
        map.Map.AddOrGetValueRef(0, out _) = 0;
        ref var firstInlineValue = ref map.Map.TryGetValueRef(0, out _);
        for (var i = 1; i < 80; ++i)
        {
            // Collisions also exercise the displaced-entry hash/index mask.
            var key = i * 32;
            if (sureAbsent)
                map.Map.AddSureAbsentDefaultAndGetValueRef(key) = i;
            else
            {
                ref var value = ref map.Map.AddOrGetValueRef(key, out var found);
                t.IsFalse(found);
                value = i;
            }
            if (i == 16)
                t.AreEqual(32, map.Map.PackedHashesAndIndexes.Length);
        }
        t.AreEqual(80, map.Map.Count);
        firstInlineValue = -7;
        for (var i = 0; i < 80; ++i)
        {
            var value = map.Map.TryGetValueRef(i * 32, out var found);
            t.IsTrue(found);
            t.AreEqual(i == 0 ? -7 : i, value);
        }
        map.Map.TryGetValueRef(-1, out var missing);
        t.IsFalse(missing);
        ref var existing = ref map.Map.AddOrGetValueRef(32, out var alreadyPresent);
        t.IsTrue(alreadyPresent);
        t.AreEqual(1, existing);
        t.AreEqual(80, map.Map.Count);
    }

    public void SmallMap8_retains_initial_hash_capacity(TestContext t)
    {
        SmallMap8<int, int, IntEq> map = default;
        for (var i = 0; i < 9; ++i)
            map.Map.AddOrGetValueRef(i, out _) = i;
        t.AreEqual(16, map.Map.PackedHashesAndIndexes.Length);
        t.AreEqual(9, map.Map.Count);
    }
}