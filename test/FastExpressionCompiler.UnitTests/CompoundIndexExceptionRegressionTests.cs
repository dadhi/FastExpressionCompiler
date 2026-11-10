using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CompoundIndexExceptionRegressionTests : ITestX
{
    public void Run(TestRun t) => Compound_indices_evaluate_once_in_order(t);
    public sealed class Box
    {
        public int Value = 4;
        public List<int> Trace;
        public int this[int x, int y] { get { Trace.Add(4); return Value; } set { Trace.Add(5); Value = value; } }
    }
    public void Compound_indices_evaluate_once_in_order(TestContext t)
    {
        var factory = S.Parameter(typeof(Func<Box>));
        var first = S.Parameter(typeof(Func<int>)); var second = S.Parameter(typeof(Func<int>));
        var cleanup = S.Parameter(typeof(Action));
        foreach (var site in new[] { 0, 1 })
        foreach (var operation in new[] { System.Linq.Expressions.ExpressionType.AddAssign,
            System.Linq.Expressions.ExpressionType.PreIncrementAssign, System.Linq.Expressions.ExpressionType.PostDecrementAssign })
        {
            System.Linq.Expressions.Expression[] indices = { S.Invoke(first), S.Invoke(second) };
            indices[site] = S.TryFinally(indices[site], S.Invoke(cleanup));
            var access = S.MakeIndex(S.Invoke(factory), typeof(Box).GetProperty("Item"), indices);
            var body = operation == System.Linq.Expressions.ExpressionType.AddAssign
                ? S.AddAssign(access, S.Constant(3)) : (System.Linq.Expressions.Expression)S.MakeUnary(operation, access, null);
            var tree = S.Lambda<Func<Func<Box>, Func<int>, Func<int>, Action, int>>(body, factory, first, second, cleanup);
            var system = tree.Compile();
            foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
            {
                var fast = tree.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
                foreach (var fail in new[] { false, true })
                {
                    var trace = new List<int>(); var box = new Box { Trace = trace };
                    Func<Box> make = () => { trace.Add(0); return box; };
                    Func<int> x = () => { trace.Add(1); return 0; };
                    Func<int> y = () => { trace.Add(2); if (fail) throw new InvalidOperationException(); return 1; };
                    Action final = () => trace.Add(3);
                    var expected = Result(() => system(make, x, y, final)); var value = box.Value; var effects = string.Join(",", trace);
                    trace.Clear(); box.Value = 4;
                    t.AreEqual(expected, Result(() => fast(make, x, y, final))); t.AreEqual(value, box.Value);
                    t.AreEqual(effects, string.Join(",", trace));
                }
            }
        }
    }
    private static object Result(Func<int> read) { try { return read(); } catch (Exception ex) { return ex.GetType(); } }

}
