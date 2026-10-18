using System;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct TypeIsRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Value_nullable_and_reference_operands(t);
        Constant_and_local_value_operands(t);
        Void_operands_preserve_side_effects(t);
        Ignored_type_tests_preserve_side_effects(t);
    }

    public void Value_nullable_and_reference_operands(TestContext t)
    {
        Type[] sources = { typeof(int), typeof(uint), typeof(double), typeof(DateTime), typeof(Kind),
            typeof(Tagged), typeof(int?), typeof(DateTime?), typeof(Kind?), typeof(Tagged?), typeof(object), typeof(string) };
        object[][] values = {
            new object[] { -1, 0, 42 }, new object[] { 0u, uint.MaxValue }, new object[] { 0d, double.NaN },
            new object[] { DateTime.MinValue }, new object[] { Kind.One }, new object[] { new Tagged { Text = "value" } },
            new object[] { null, 42 }, new object[] { null, DateTime.MinValue }, new object[] { null, Kind.One },
            new object[] { null, new Tagged { Text = "value" } }, new object[] { null, 42, "text", new Tagged() },
            new object[] { null, "text" }
        };
        Type[] targets = { typeof(void), typeof(object), typeof(ValueType), typeof(Enum), typeof(int), typeof(uint),
            typeof(double), typeof(DateTime), typeof(Kind), typeof(Tagged), typeof(int?), typeof(Kind?), typeof(Tagged?),
            typeof(IComparable), typeof(ITagged), typeof(string), typeof(int).MakePointerType() };
        for (var i = 0; i < sources.Length; ++i)
        foreach (var target in targets)
        {
            var fastParameter = Expr.Parameter(sources[i]);
            var fast = Expr.Lambda(Expr.TypeIs(fastParameter, target), fastParameter).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in values[i])
                t.AreEqual((object)target.IsInstanceOfType(value), fast.DynamicInvoke(new[] { value }));
        }
    }

    public void Constant_and_local_value_operands(TestContext t)
    {
        Type[] sources = { typeof(int), typeof(Kind), typeof(Tagged), typeof(int?), typeof(Tagged?) };
        object[] values = { 42, Kind.One, new Tagged { Text = "value" }, 42, null };
        Type[] targets = { typeof(object), typeof(ValueType), typeof(Enum), typeof(int?), typeof(ITagged), typeof(void) };
        for (var i = 0; i < sources.Length; ++i)
        foreach (var target in targets)
        {
            var constant = Expr.Constant(values[i], sources[i]);
            var local = Expr.Variable(sources[i]);
            var expected = target.IsInstanceOfType(values[i]);
            var direct = Expr.Lambda<Func<bool>>(Expr.TypeIs(constant, target)).CompileFast(true);
            var block = Expr.Lambda<Func<bool>>(Expr.Block(new[] { local }, Expr.Assign(local, constant),
                Expr.TypeIs(local, target))).CompileFast(true);
            if (!t.IsNotNull(direct) || !t.IsNotNull(block)) return;
            t.AreEqual(expected, direct());
            t.AreEqual(expected, block());
        }
    }

    public void Void_operands_preserve_side_effects(TestContext t)
    {
        var action = Expr.Parameter(typeof(Action));
        foreach (var target in new[] { typeof(void), typeof(object), typeof(int), typeof(int?) })
        {
            var expression = Expr.Lambda<Func<Action, bool>>(Expr.TypeIs(Expr.Invoke(action), target), action);
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var calls = 0;
            t.AreEqual(target == typeof(void), fast(() => ++calls));
            t.AreEqual(1, calls);
        }
    }

    public void Ignored_type_tests_preserve_side_effects(TestContext t)
    {
        var getValue = Expr.Parameter(typeof(Func<int>));
        var expression = Expr.Lambda<Action<Func<int>>>(
            Expr.Block(Expr.TypeIs(Expr.Invoke(getValue), typeof(object)), Expr.Empty()), getValue);
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        fast(() => ++calls);
        t.AreEqual(1, calls);
    }

    public interface ITagged { }
    public struct Tagged : ITagged { public string Text; }
    public enum Kind { One = 1 }
}
