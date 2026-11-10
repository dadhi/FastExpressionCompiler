using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct PropertyGetterReceiverRegressionTests : ITestX
{
    public void Run(TestRun t) { Interface_getters_use_struct_receiver_addresses(t); Discarded_getters_pop_results_and_keep_effects(t); Null_reference_getters_throw(t); }
    public interface IValue { int Value { get; } }
    public struct Cell : IValue { public int Number; public int Value { get { return ++Number; } } }
    public sealed class Owner { public int Calls; public int Value { get { ++Calls; return 17; } } public int Constant => 17; }
    public void Interface_getters_use_struct_receiver_addresses(TestContext t)
    {
        var value = S.Variable(typeof(Cell));
        var property = S.Property(value, typeof(IValue).GetProperty(nameof(IValue.Value)));
        var tree = S.Lambda<Func<int>>(S.Block(new[] { value }, S.Assign(value, S.Default(typeof(Cell))),
            S.Add(S.Multiply(property, S.Constant(10)), S.Field(value, nameof(Cell.Number)))));
        var expected = tree.Compile()();
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        { var fast = tree.FromSysExpression().CompileFast(true, flags); if (t.IsNotNull(fast)) t.AreEqual(expected, fast()); }
    }
    public void Discarded_getters_pop_results_and_keep_effects(TestContext t)
    {
        var owner = S.Parameter(typeof(Owner));
        var tree = S.Lambda<Func<Owner, int>>(S.Block(S.Property(owner, nameof(Owner.Value)), S.Field(owner, nameof(Owner.Calls))), owner);
        var fast = tree.FromSysExpression().CompileFast(true); if (!t.IsNotNull(fast)) return;
        t.AreEqual(tree.Compile()(new Owner()), fast(new Owner()));
    }
    public void Null_reference_getters_throw(TestContext t)
    {
        var owner = S.Parameter(typeof(Owner));
        var tree = S.Lambda<Func<Owner, int>>(S.Property(owner, nameof(Owner.Constant)), owner);
        t.Throws<NullReferenceException>(() => tree.Compile()(null));
        var fast = tree.FromSysExpression().CompileFast(true); if (t.IsNotNull(fast)) t.Throws<NullReferenceException>(() => fast(null));
    }

}
