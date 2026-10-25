using System;
using SysExpr = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
using CE = FastExpressionCompiler.LightExpression.ConstantExpression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
using CE = System.Linq.Expressions.ConstantExpression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct SharedPreCreatedClosureTests : ITestX
{
    public void Run(TestRun t)
    {
        Nested_constants_share_one_target(t);
        Reused_lambdas_compile_once(t);
        Nested_parameters_and_custom_byref_signatures(t);
        Captures_fail_without_poisoning_later_compilation(t);
        Nested_exception_regions_keep_constant_access(t);
        Parallel_compilation_has_independent_targets(t);
    }
    public sealed class Box { public int Value = 42; public void Increment() => ++Value; }
    private static Delegate Compile(System.Linq.Expressions.LambdaExpression tree, params object[] values)
    {
        var constants = new CE[values.Length];
        for (var i = 0; i < values.Length; ++i) constants[i] = E.Constant(values[i]);
        return tree.FromSysExpression().TryCompileWithPreCreatedClosure<Delegate>(constants);
    }
    public void Nested_constants_share_one_target(TestContext t)
    {
        var box = new Box();
        var read = SysExpr.Lambda<Func<int>>(SysExpr.Field(SysExpr.Constant(box), nameof(Box.Value)));
        var middle = SysExpr.Lambda<Func<Func<int>>>(read);
        var tree = SysExpr.Lambda<Func<Func<Func<int>>>>(middle);
        var compiled = (Func<Func<Func<int>>>)Compile(tree, box);
        if (!t.IsNotNull(compiled)) return;
        var actualMiddle = compiled(); var actual = actualMiddle();
        t.AreEqual(tree.Compile()()()(), actual());
        t.IsTrue(ReferenceEquals(compiled.Target, actualMiddle.Target));
        t.IsTrue(ReferenceEquals(compiled.Target, actual.Target));
        var constants = ((ExpressionCompiler.ArrayClosure)compiled.Target).ConstantsAndNestedLambdas;
        t.AreEqual(1, constants.Length);
        t.IsTrue(ReferenceEquals(box, constants[0]));
        box.Value = 17; t.AreEqual(17, actual());
    }
    public delegate int RefUpdater(ref int value);
    public void Nested_parameters_and_custom_byref_signatures(TestContext t)
    {
        var box = new Box();var value = SysExpr.Parameter(typeof(int).MakeByRefType());
        var child = SysExpr.Lambda<RefUpdater>(SysExpr.AddAssign(value,
            SysExpr.Field(SysExpr.Constant(box), nameof(Box.Value))), value);
        var tree = SysExpr.Lambda<Func<RefUpdater>>(child);
        var factory = (Func<RefUpdater>)Compile(tree, box);
        if (!t.IsNotNull(factory)) return;
        var actual = factory();var expected = tree.Compile()();
        var a = 3;var e = 3;
        t.AreEqual(expected(ref e), actual(ref a)); t.AreEqual(e, a);
        t.AreEqual(expected(ref e), actual(ref a)); t.AreEqual(e, a);
    }
    public void Reused_lambdas_compile_once(TestContext t)
    {
        var box = new Box();
        var read = SysExpr.Field(SysExpr.Constant(box), nameof(Box.Value));
        var first = SysExpr.Lambda<Func<int>>(read);
        var second = SysExpr.Lambda<Func<int>>(SysExpr.Add(read, SysExpr.Constant(1)));
        var tree = SysExpr.Lambda<Func<Func<int>[]>>(SysExpr.NewArrayInit(typeof(Func<int>), first, first, second));
        var compiled = (Func<Func<int>[]>)Compile(tree, box);
        if (!t.IsNotNull(compiled)) return;
        var children = compiled();
        t.IsTrue(ReferenceEquals(children[0], children[1]));
        t.IsTrue(!ReferenceEquals(children[0], children[2]));
        t.IsTrue(ReferenceEquals(children[0].Target, children[2].Target));
        t.AreEqual(42, children[0]()); t.AreEqual(43, children[2]());
        t.IsTrue(ReferenceEquals(children[0], compiled()[0]));
    }
    public void Captures_fail_without_poisoning_later_compilation(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(int));
        var capturesParameter = SysExpr.Lambda<Func<int, Func<int>>>(SysExpr.Lambda<Func<int>>(value), value);
        var local = SysExpr.Variable(typeof(int));
        var capturesLocal = SysExpr.Lambda<Func<int, Func<int>>>(SysExpr.Block(new[] { local }, SysExpr.Assign(local, value),
            SysExpr.Lambda<Func<int>>(local)), value);
        var box = new Box();
        var valid = SysExpr.Lambda<Func<int, Func<int>>>(SysExpr.Lambda<Func<int>>(
            SysExpr.Field(SysExpr.Constant(box), nameof(Box.Value))), value);
        for (var i = 0; i < 8; ++i)
        {
            t.IsTrue(Compile(capturesParameter) == null);
            t.IsTrue(Compile(capturesLocal) == null);
            var compiled = (Func<int, Func<int>>)Compile(valid, box);
            if (!t.IsNotNull(compiled)) return;
            t.AreEqual(42, compiled(3)());
        }
    }
    public void Nested_exception_regions_keep_constant_access(TestContext t)
    {
        var box = new Box();var constant = SysExpr.Constant(box);
        var child = SysExpr.Lambda<Func<int>>(SysExpr.TryFinally(SysExpr.Field(constant, nameof(Box.Value)),
            SysExpr.Call(constant, typeof(Box).GetMethod(nameof(Box.Increment)))));
        var tree = SysExpr.Lambda<Func<Func<int>>>(child);
        var compiled = (Func<Func<int>>)Compile(tree, box);
        if (!t.IsNotNull(compiled)) return;
        t.AreEqual(42, compiled()()); t.AreEqual(43, box.Value);
        t.AreEqual(43, compiled()()); t.AreEqual(44, box.Value);
    }
    public void Parallel_compilation_has_independent_targets(TestContext t)
    {
        var box = new Box();
        var tree = SysExpr.Lambda<Func<Func<int>>>(SysExpr.Lambda<Func<int>>(SysExpr.Field(SysExpr.Constant(box), nameof(Box.Value))));
        var delegates = new Func<Func<int>>[16];
        System.Threading.Tasks.Parallel.For(0, delegates.Length, i => delegates[i] = (Func<Func<int>>)Compile(tree, box));
        for (var i = 0; i < delegates.Length; ++i)
        {
            if (!t.IsNotNull(delegates[i])) return;
            t.AreEqual(42, delegates[i]()());
            if (i != 0) t.IsTrue(!ReferenceEquals(delegates[0].Target, delegates[i].Target));
        }
    }
}
