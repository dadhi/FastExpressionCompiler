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
    public void Run(TestRun t) { Compound_indices_evaluate_once_in_order(t); Struct_receivers_preserve_values_and_addresses(t); }
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

    public struct ValueBox
    {
        public Box Inner;
        public int this[int x, int y] { get => Inner[x, y]; set => Inner[x, y] = value; }
        public static ValueBox Copy(ValueBox value) => value;
    }
    public delegate int RefOperation(ref ValueBox value, Action cleanup);
    public void Struct_receivers_preserve_values_and_addresses(TestContext t)
    {
        var value = S.Parameter(typeof(ValueBox));
        var byRef = S.Parameter(typeof(ValueBox).MakeByRefType());
        var cleanup = S.Parameter(typeof(Action));
        foreach (var spill in new[] { false, true })
        {
            var index = spill ? S.TryFinally(S.Constant(0), S.Invoke(cleanup)) : (System.Linq.Expressions.Expression)S.Constant(0);
            foreach (var useCopy in new[] { false, true })
            {
                var receiver = useCopy ? S.Call(typeof(ValueBox).GetMethod(nameof(ValueBox.Copy)), value) : (System.Linq.Expressions.Expression)value;
                var access = S.MakeIndex(receiver, typeof(ValueBox).GetProperty("Item"), new[] { index, S.Constant(1) });
                var tree = S.Lambda<Func<ValueBox, Action, int>>(S.AddAssign(access, S.Constant(3)), value, cleanup);
                var system = tree.Compile();
                var fast = tree.FromSysExpression().CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                var trace = new List<int>(); var box = new Box { Trace = trace };
                var argument = new ValueBox { Inner = box }; Action final = () => trace.Add(3);
                var expected = system(argument, final); var effects = string.Join(",", trace); var stored = box.Value;
                box.Value = 4; trace.Clear();
                t.AreEqual(expected, fast(argument, final)); t.AreEqual(stored, box.Value); t.AreEqual(effects, string.Join(",", trace));
            }
            var refAccess = S.MakeIndex(byRef, typeof(ValueBox).GetProperty("Item"), new[] { index, S.Constant(1) });
            var refTree = S.Lambda<RefOperation>(S.AddAssign(refAccess, S.Constant(3)), byRef, cleanup);
            var refSystem = refTree.Compile();
            var refFast = refTree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(refFast)) return;
            var refTrace = new List<int>(); var refBox = new Box { Trace = refTrace };
            var refArgument = new ValueBox { Inner = refBox }; Action refFinal = () => refTrace.Add(3);
            var refExpected = refSystem(ref refArgument, refFinal); var refEffects = string.Join(",", refTrace); var refStored = refBox.Value;
            refBox.Value = 4; refTrace.Clear();
            t.AreEqual(refExpected, refFast(ref refArgument, refFinal)); t.AreEqual(refStored, refBox.Value); t.AreEqual(refEffects, string.Join(",", refTrace));
        }
    }

}
