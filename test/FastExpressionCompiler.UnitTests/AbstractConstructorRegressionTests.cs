using System;
using System.Reflection;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct AbstractConstructorRegressionTests : ITestX
{
    public void Run(TestRun t) { Abstract_constructor_is_rejected_before_delegate_creation(t); Concrete_constructor_and_default_value_type_remain_valid(t); Nested_abstract_constructor_is_rejected(t); Nested_concrete_and_default_constructors_remain_valid(t); }
    public void Abstract_constructor_is_rejected_before_delegate_creation(TestContext t)
    {
        var noArgs = E.Lambda<Func<Abstract>>(E.New(typeof(Abstract).GetConstructor(Type.EmptyTypes)));
        var withArg = E.Lambda<Func<Abstract>>(E.New(typeof(Abstract).GetConstructor(new[] { typeof(int) }), E.Constant(4)));
        t.Throws<InvalidOperationException>(() => noArgs.CompileFast(true));
        t.Throws<InvalidOperationException>(() => withArg.CompileFast(true));
    }
    public void Concrete_constructor_and_default_value_type_remain_valid(TestContext t)
    {
        var concrete = E.Lambda<Func<Concrete>>(E.New(typeof(Concrete).GetConstructor(Type.EmptyTypes))).CompileFast(true);
        if (t.IsNotNull(concrete)) t.AreEqual(7, concrete().Value);
        var withArg = E.Lambda<Func<Concrete>>(E.New(typeof(Concrete).GetConstructor(new[] { typeof(int) }), E.Constant(4))).CompileFast(true);
        if (t.IsNotNull(withArg)) t.AreEqual(4, withArg().Value);
        var zero = E.Lambda<Func<Plain>>(E.New(typeof(Plain))).CompileFast(true);
        if (t.IsNotNull(zero)) t.AreEqual(0, zero().Value);
    }
    public void Nested_abstract_constructor_is_rejected(TestContext t)
    {
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        {
            var inner = E.Lambda<Func<Abstract>>(E.New(typeof(Abstract).GetConstructor(Type.EmptyTypes)));
            var outer = E.Lambda<Func<Func<Abstract>>>(inner);
            t.Throws<InvalidOperationException>(() => outer.CompileFast(true, flags));
        }
    }
    public void Nested_concrete_and_default_constructors_remain_valid(TestContext t)
    {
        var concrete = E.Lambda<Func<Func<Concrete>>>(E.Lambda<Func<Concrete>>(
            E.New(typeof(Concrete).GetConstructor(Type.EmptyTypes)))).CompileFast(true);
        if (t.IsNotNull(concrete)) t.AreEqual(7, concrete()().Value);
        var plain = E.Lambda<Func<Func<Plain>>>(E.Lambda<Func<Plain>>(E.New(typeof(Plain)))).CompileFast(true);
        if (t.IsNotNull(plain)) t.AreEqual(0, plain()().Value);
    }
    public abstract class Abstract { public Abstract() { } public Abstract(int value) { } }
    public sealed class Concrete : Abstract { public int Value; public Concrete() { Value = 7; } public Concrete(int value) { Value = value; } }
    public struct Plain { public int Value; }
}
