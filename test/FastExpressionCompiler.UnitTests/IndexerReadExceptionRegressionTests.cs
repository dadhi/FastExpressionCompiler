using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct IndexerReadExceptionRegressionTests : ITestX
{
    public void Run(TestRun t) => Receiver_and_index_effects_match_system(t);
    public sealed class Box
    {
        public List<int> Trace;
        public int this[int index] { get { Trace.Add(3); return index + 10; } }
    }
    public void Receiver_and_index_effects_match_system(TestContext t)
    {
        var factory = S.Parameter(typeof(Func<Box>));
        var index = S.Parameter(typeof(Func<int>));
        var cleanup = S.Parameter(typeof(Action));
        var access = S.MakeIndex(S.Invoke(factory), typeof(Box).GetProperty("Item"),
            new[] { S.TryFinally(S.Invoke(index), S.Invoke(cleanup)) });
        var tree = S.Lambda<Func<Func<Box>, Func<int>, Action, int>>(access, factory, index, cleanup);
        var system = tree.Compile();
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            foreach (var fail in new[] { false, true })
            foreach (var nullReceiver in new[] { false, true })
            {
                var trace = new List<int>(); var box = new Box { Trace = trace };
                Func<Box> make = () => { trace.Add(0); return nullReceiver ? null : box; };
                Func<int> read = () => { trace.Add(1); if (fail) throw new InvalidOperationException(); return 2; };
                Action final = () => trace.Add(2);
                var expected = Result(() => system(make, read, final)); var effects = string.Join(",", trace);
                trace.Clear(); t.AreEqual(expected, Result(() => fast(make, read, final)));
                t.AreEqual(effects, string.Join(",", trace));
            }
        }
    }
    private static object Result(Func<int> read) { try { return read(); } catch (Exception ex) { return ex.GetType(); } }

}
