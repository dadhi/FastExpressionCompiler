using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ByrefLambdaInliningTests : ITestX
{
    public delegate int Update(ref int first, ref int second);
    public void Run(TestRun t) { Ref_parameters_preserve_aliases_and_writes(t); }
    public void Ref_parameters_preserve_aliases_and_writes(TestContext t)
    {
        var first = E.Parameter(typeof(int).MakeByRefType()); var second = E.Parameter(typeof(int).MakeByRefType());
        var inner = E.Lambda<Update>(E.Block(E.Assign(first, E.Constant(40)), E.AddAssign(second, E.Constant(2)), E.Add(first, second)), first, second);
        var value = E.Variable(typeof(int));
        var tree = E.Lambda<Func<int>>(E.Block(new[] { value }, E.Assign(value, E.Constant(1)), E.Invoke(inner, value, value), value));
        t.AreEqual(42, ((Func<int>)tree.CompileSys())());
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        { var fast = tree.CompileFast(true, flags); if (t.IsNotNull(fast)) t.AreEqual(42, fast()); }
    }

}
